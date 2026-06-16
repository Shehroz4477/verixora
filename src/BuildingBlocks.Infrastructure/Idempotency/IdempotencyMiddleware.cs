// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Idempotency / IdempotencyMiddleware.cs
// ====================================================================
//
// ASP.NET Core middleware that enforces idempotency for requests
// carrying an `Idempotency‑Key` header.  It guarantees that a
// state‑changing command is executed at most once per key, even
// across multiple API instances.
//
// The middleware:
//   1. Extracts the key from the request header.
//   2. Calls TryReserveAsync to obtain an execution token.
//   3. If reservation succeeds → executes the pipeline and captures
//      the successful response (status code, content type, body up to
//      100 KB).  On failure, marks the key as failed.
//   4. If reservation fails → checks the current state:
//        - Completed → replays the stored response verbatim.
//        - Failed → replays the stored error response.
//        - InProgress → returns HTTP 409 Conflict.
//
// Ownership is enforced by the execution token: only the token
// holder may store or fail the key, preventing stale writes.
//
//   KNOWN LIMITATION – Response Capture:
//     Response bodies are read into a string using UTF‑8 encoding.
//     Binary payloads (e.g., firmware blobs) may be truncated or
//     corrupted.  A future version should use a byte‑oriented buffer
//     and store the raw bytes in the database.
//
//   NO NOTFOUND RETRY:
//     The middleware no longer retries on a NotFound state after a
//     failed reservation, as that could cause a double reservation
//     race.  It treats it as a conflict and returns HTTP 500.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **Middleware** – a class with `InvokeAsync` that can short‑circuit
//    the pipeline.
// 2. **RequestDelegate** – represents the next middleware.
// 3. **HttpContext** – access to request/response.
// 4. **Stream replacement** – we replace `Response.Body` with a
//    `MemoryStream` to capture the output, then restore it.
// 5. **System.Text.Json** – used to serialise / deserialise the
//    stored response object.
// 6. **ILogger** – structured logging.
// 7. **Async / await** – all I/O is non‑blocking.
// 8. **ReserveResult** / **IdempotencyState** enums – clean control flow.
// ====================================================================

using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using static System.Net.Mime.MediaTypeNames;

namespace BuildingBlocks.Infrastructure.Idempotency;

public sealed class IdempotencyMiddleware
{
    private const string HeaderName = "Idempotency-Key";
    private const string ContentType = "application/json";
    private const int MaxResponseBodyLength = 100_000;   // 100 KB capture limit

    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IdempotencyStore store)
    {
        // If the idempotency header is missing, pass through unchanged.
        if (!context.Request.Headers.TryGetValue(HeaderName, out var values) ||
            string.IsNullOrWhiteSpace(values))
        {
            await _next(context);
            return;
        }

        var key = values.ToString();

        _logger.LogDebug("Idempotency key {Key} received.", key);

        // Step 1: Try to reserve the key.
        var (result, executionId) = await store.TryReserveAsync(key);

        switch (result)
        {
            case ReserveResult.Acquired when executionId is not null:
                // We own the key.  Execute the pipeline and capture the outcome.
                await ExecuteAndCaptureResponseAsync(context, store, key, executionId);
                break;

            case ReserveResult.AlreadyExists:
                // Key already exists – check its current state.
                var (state, responseJson) = await store.GetStateAsync(key);
                switch (state)
                {
                    case IdempotencyState.Completed:
                    case IdempotencyState.Failed:
                        // The command finished (success or failure).  Replay the stored response.
                        await ReplayResponseAsync(context, responseJson!);
                        break;

                    case IdempotencyState.InProgress:
                        // Another request is still executing.
                        context.Response.StatusCode = StatusCodes.Status409Conflict;
                        context.Response.ContentType = ContentType;
                        await context.Response.WriteAsync(
                            "{\"error\":\"A request with this idempotency key is already in progress.\"}");
                        break;

                    default:
                        // Should not happen.  Log and return a generic error.
                        _logger.LogWarning("Unexpected state {State} for key {Key}", state, key);
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("{\"error\":\"Idempotency conflict.\"}");
                        break;
                }
                break;

            default:
                // Unexpected reservation result.
                _logger.LogError("Unexpected reserve result {Result} for key {Key}", result, key);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("{\"error\":\"Idempotency error.\"}");
                break;
        }
    }

    // ----------------------------------------------------------------
    // Private helpers
    // ----------------------------------------------------------------

    /// <summary>
    /// Executes the downstream pipeline, captures the response if
    /// successful, and stores it in the idempotency store.  On
    /// failure, marks the key as permanently failed.
    /// </summary>
    private async Task ExecuteAndCaptureResponseAsync(
        HttpContext context,
        IdempotencyStore store,
        string key,
        string executionId)
    {
        // Save the original response body stream and replace it
        // with a memory stream so we can capture the output.
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            // Execute the rest of the pipeline.
            await _next(context);

            // Only capture successful responses (HTTP 2xx).
            if (context.Response.StatusCode is >= 200 and < 300)
            {
                buffer.Seek(0, SeekOrigin.Begin);
                var bodyText = await ReadStreamAsync(buffer, MaxResponseBodyLength);

                // Build a JSON object containing the original status code,
                // content type, and body, so we can replay it faithfully.
                var stored = new StoredResponse(
                    context.Response.StatusCode,
                    bodyText,
                    context.Response.ContentType ?? ContentType);
                var json = JsonSerializer.Serialize(stored);

                await store.StoreResponseAsync(key, executionId, json);

                // Copy the captured bytes back to the original stream.
                buffer.Seek(0, SeekOrigin.Begin);
                await buffer.CopyToAsync(originalBody);
            }
            else
            {
                // Non‑success response – mark the key as failed so it
                // doesn't stay InProgress forever.
                var errorBody = $"{{\"error\":\"Command returned status {context.Response.StatusCode}\"}}";
                await store.MarkFailedAsync(key, executionId, errorBody);

                // Still write the error response to the client.
                buffer.Seek(0, SeekOrigin.Begin);
                await buffer.CopyToAsync(originalBody);
            }
        }
        catch (Exception ex)
        {
            // If anything goes wrong during execution, mark the key
            // as failed (best effort) and re‑throw the original exception.
            var errorPayload = $"{{\"error\":\"{ex.Message}\"}}";
            try { await store.MarkFailedAsync(key, executionId, errorPayload); } catch { /* best effort */ }
            throw;
        }
        finally
        {
            // Always restore the original response body stream.
            context.Response.Body = originalBody;
        }
    }

    /// <summary>
    /// Reads the response stream as a UTF‑8 string, enforcing a
    /// maximum length to prevent memory exhaustion.
    /// </summary>
    private static async Task<string> ReadStreamAsync(Stream stream, int maxLength)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var buffer = new char[maxLength];
        int count = await reader.ReadBlockAsync(buffer, 0, maxLength);
        return new string(buffer, 0, count);
    }

    /// <summary>
    /// Replays a previously stored response, restoring the original
    /// HTTP status code, content type, and body.
    /// </summary>
    private static async Task ReplayResponseAsync(HttpContext context, string storedJson)
    {
        var stored = JsonSerializer.Deserialize<StoredResponse>(storedJson)
                     ?? new StoredResponse(200, string.Empty, ContentType);

        context.Response.StatusCode = stored.StatusCode;
        context.Response.ContentType = stored.ContentType;
        await context.Response.WriteAsync(stored.Body);
    }
}





//Dry‑run – the complete idempotency flow with the middleware:

//Client sends: POST /api/v1/smartlocks/{id}/ unlock
//Headers: Idempotency - Key: abc - 123 - def

//First request(key is new):
//  1.Middleware: TryReserveAsync → (Acquired, "exec-1")
//  2. Middleware: calls next() → pipeline returns 202 Accepted
//  3. Middleware: captures { status:202, body: "...", contentType: "application/json" }
//     and stores it in the store.
//  4. Client receives HTTP 202 Accepted with original body.

//Duplicate request (same key, same client):
//  1.Middleware: TryReserveAsync → (AlreadyExists, null)
//  2. Middleware: GetStateAsync → (Completed, storedJson)
//  3. Middleware: deserialises storedJson, sets status=202,
//     Content-Type=application/json, writes body.
//  4. Client receives the exact same 202 Accepted response.

//Concurrent request (another thread still executing):
//  1.Middleware: TryReserveAsync → (AlreadyExists, null)
//  2. Middleware: GetStateAsync → (InProgress, null)
//  3. Middleware: returns HTTP 409 Conflict
//  4. Client can retry later or use a different key.
