// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Idempotency / IdempotencyStore.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements the **Idempotency pattern** required by ADR‑016.
//   All state‑changing device commands must carry an
//   `Idempotency‑Key` header.
//
//   The store provides four operations:
//     1. **Reserve** – atomically inserts the key with a NULL response
//        and an initial status of `InProgress`.  If a non‑expired
//        record already exists, the reservation fails.  If an expired
//        record exists, it is **replaced** atomically, allowing the
//        key to be reused after the TTL window.  The reservation
//        returns a unique `OwnerExecutionId` that the caller must
//        use for all subsequent updates.
//     2. **Store response** – after successful command execution,
//        updates the record with the response body and sets the
//        status to `Completed`.  The update only succeeds if the
//        caller still owns the reservation (matching
//        `OwnerExecutionId`) and the record is still `InProgress`.
//     3. **Mark failed** – permanently marks the command as `Failed`,
//        preventing the record from staying `InProgress` forever.
//     4. **Get state** – returns the current lifecycle state of an
//        idempotency key: NotFound, InProgress, Completed, or Failed.
//
//   STRICT IDEMPOTENCY RULE:
//     Once a key has been reserved, the command associated with that
//     key must NEVER be executed again.  The state InProgress means
//     the command is running or its outcome is unknown; the caller
//     MUST NOT re‑execute.
//
//   CONCURRENCY GUARANTEE:
//     The unique constraint on the `Key` column is the sole
//     synchronisation primitive.  Reservation uses a single atomic
//     UPSERT that:
//       - INSERTS a new row if no row exists.
//       - If a row exists AND it is expired (judged by the database
//         server’s own clock, `NOW()`), the UPDATE clause replaces it
//         (new timestamps, NULL response, new OwnerExecutionId) – one
//         row affected → reservation succeeds.
//       - If a row exists AND it is NOT expired, the WHERE clause
//         prevents the UPDATE – zero rows affected → reservation fails.
//
//     Two concurrent reservations for the same expired key will see
//     exactly one succeed because the database serialises the two
//     UPSERTs and row‑level locking ensures the second one sees the
//     updated (non‑expired) row.  Using the server’s clock eliminates
//     client‑side clock skew as a factor.
//
//   STATE TRANSITION GUARDS:
//     StoreResponseAsync and MarkFailedAsync require the record to
//     be in the `InProgress` state.  Once a record is Completed or
//     Failed, no further changes are allowed.  This prevents late
//     or duplicate writers from corrupting the final result.
//
//   KNOWN LIMITATION – Ownership Expiry:
//     If a worker crashes after reservation, the record stays
//     InProgress until the TTL expires (24 hours).  There is no
//     heartbeat or lease mechanism in MVP.  A future version can
//     add a `LockedUntilUtc` column for fine‑grained lease recovery.
//
//   REQUIRED SCHEMA:
//     The table `IdempotencyRecords` must have:
//       - A **unique constraint on `Key` only**.
//       - A `Status` column (InProgress, Completed, Failed).
//       - An `OwnerExecutionId` column (nullable, for ownership).
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **sealed class** – prevents inheritance.
// 2. **DbContext injection** – decouples the store from any module.
// 3. **async / await** – all I/O is asynchronous.
// 4. **ExecuteSqlInterpolatedAsync** – safe, parameterised SQL.
// 5. **enum IdempotencyState / ReserveResult** – explicit state
//    machines and result types.
// 6. **Input validation** – key length and response size caps.
// 7. **internal persistence model** – `IdempotencyRecord` is hidden.
// 8. **Atomic UPSERT with `NOW()`** – the WHERE clause uses the
//    database server’s clock for deterministic expiry evaluation.
// 9. **Conditional UPDATE with state guard** – `WHERE Status =
//    'InProgress'` ensures that state transitions are only made
//    from the correct initial state.
// ====================================================================

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OpenTelemetry.Trace;

namespace BuildingBlocks.Infrastructure.Idempotency;

/// <summary>
/// Atomic idempotency reservation and response storage.
/// </summary>
public sealed class IdempotencyStore
{
    // Limits to prevent abuse / storage bloat.
    private const int MaxKeyLength = 256;
    private const int MaxResponseLength = 100_000;   // 100 KB

    private readonly DbContext _context;

    /// <summary>
    /// Initialises the store with a DbContext that exposes a
    /// <see cref="DbSet{TEntity}"/> of <see cref="IdempotencyRecord"/>.
    /// </summary>
    public IdempotencyStore(DbContext context)
    {
        _context = context;
    }

    // ----------------------------------------------------------------
    // Step 1 – Reserve the idempotency key (execution gate)
    // ----------------------------------------------------------------

    /// <summary>
    /// Tries to atomically reserve the given idempotency key.
    /// If successful, returns an execution ID that must be used for
    /// <see cref="StoreResponseAsync"/> or <see cref="MarkFailedAsync"/>.
    /// An expired record is silently replaced, allowing key reuse.
    /// </summary>
    /// <returns>
    /// A <see cref="ReserveResult"/> indicating whether the key was
    /// acquired, and the execution ID if acquired.
    /// </returns>
    public async Task<(ReserveResult Result, string? ExecutionId)> TryReserveAsync(string key)
    {
        // Validate the key before touching the database.
        ValidateKey(key);

        var now = DateTime.UtcNow;
        var expires = now.AddHours(24);
        var executionId = Guid.NewGuid().ToString("N");   // 32‑char hex string

        // Single atomic UPSERT.
        // The WHERE clause uses the database server’s NOW() function
        // so all concurrent callers use the same time reference.
        int rowsAffected = await _context.Database
            .ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""IdempotencyRecords""
                (""Key"", ""Response"", ""OwnerExecutionId"", ""Status"", ""CreatedAtUtc"", ""ExpiresAtUtc"")
                VALUES ({key}, NULL, {executionId}, 'InProgress', {now}, {expires})
                ON CONFLICT (""Key"") DO UPDATE
                SET ""CreatedAtUtc"" = EXCLUDED.""CreatedAtUtc"",
                    ""ExpiresAtUtc"" = EXCLUDED.""ExpiresAtUtc"",
                    ""Response"" = NULL,
                    ""OwnerExecutionId"" = EXCLUDED.""OwnerExecutionId"",
                    ""Status"" = 'InProgress'
                WHERE ""IdempotencyRecords"".""ExpiresAtUtc"" < NOW()
            ")
            .ConfigureAwait(false);

        // rowsAffected == 1 → reservation succeeded (new or replaced expired).
        // rowsAffected == 0 → non‑expired record already exists.
        if (rowsAffected == 1)
            return (ReserveResult.Acquired, executionId);

        return (ReserveResult.AlreadyExists, null);
    }

    // ----------------------------------------------------------------
    // Step 2 – After successful command execution, store the response
    // ----------------------------------------------------------------

    /// <summary>
    /// Stores the actual response for a previously reserved key.
    /// The update only succeeds if the caller still owns the
    /// reservation (matching <paramref name="executionId"/>) and the
    /// record is still in the <see cref="IdempotencyState.InProgress"/>
    /// state.  Once a record is Completed or Failed, no further
    /// changes are allowed.
    /// </summary>
    /// <param name="key">The reserved idempotency key.</param>
    /// <param name="executionId">The execution ID from <see cref="TryReserveAsync"/>.</param>
    /// <param name="response">The response body to store.</param>
    public async Task StoreResponseAsync(string key, string executionId, string response)
    {
        // Validate inputs.
        ValidateKey(key);
        if (string.IsNullOrEmpty(executionId))
            throw new ArgumentNullException(nameof(executionId));
        if (string.IsNullOrEmpty(response))
            throw new ArgumentException("Response cannot be null or empty.", nameof(response));
        if (response.Length > MaxResponseLength)
            throw new ArgumentException(
                $"Response exceeds maximum length of {MaxResponseLength} characters.", nameof(response));

        // Conditional UPDATE: only set the response if the caller
        // still owns the reservation and the record is still InProgress.
        int rows = await _context.Database
            .ExecuteSqlInterpolatedAsync($@"
                UPDATE ""IdempotencyRecords""
                SET ""Response"" = {response},
                    ""Status"" = 'Completed'
                WHERE ""Key"" = {key}
                  AND ""OwnerExecutionId"" = {executionId}
                  AND ""Status"" = 'InProgress'
                  AND ""ExpiresAtUtc"" > NOW()
            ")
            .ConfigureAwait(false);

        if (rows == 0)
            throw new InvalidOperationException(
                "Cannot store response: the key does not exist, the record has expired, " +
                "the response was already stored, or you are not the current owner.");
    }

    // ----------------------------------------------------------------
    // Step 3 – Mark a command as permanently failed
    // ----------------------------------------------------------------

    /// <summary>
    /// Marks the command as permanently failed, preventing it from
    /// staying InProgress forever.  Only the current owner may call
    /// this method, and only if the record is still InProgress.
    /// </summary>
    /// <param name="key">The reserved idempotency key.</param>
    /// <param name="executionId">The execution ID from <see cref="TryReserveAsync"/>.</param>
    /// <param name="errorResponse">The error response body to store.</param>
    public async Task MarkFailedAsync(string key, string executionId, string errorResponse)
    {
        ValidateKey(key);
        if (string.IsNullOrEmpty(executionId))
            throw new ArgumentNullException(nameof(executionId));
        if (string.IsNullOrEmpty(errorResponse))
            throw new ArgumentException("Error response cannot be null or empty.", nameof(errorResponse));
        if (errorResponse.Length > MaxResponseLength)
            throw new ArgumentException(
                $"Error response exceeds maximum length of {MaxResponseLength} characters.", nameof(errorResponse));

        // Same guard as StoreResponseAsync – only InProgress records
        // may transition to Failed.
        int rows = await _context.Database
            .ExecuteSqlInterpolatedAsync($@"
                UPDATE ""IdempotencyRecords""
                SET ""Response"" = {errorResponse},
                    ""Status"" = 'Failed'
                WHERE ""Key"" = {key}
                  AND ""OwnerExecutionId"" = {executionId}
                  AND ""Status"" = 'InProgress'
                  AND ""ExpiresAtUtc"" > NOW()
            ")
            .ConfigureAwait(false);

        if (rows == 0)
            throw new InvalidOperationException(
                "Cannot mark as failed: the key does not exist, the record has expired, " +
                "the response was already stored, or you are not the current owner.");
    }

    // ----------------------------------------------------------------
    // Step 4 – Retrieve the state of an idempotency key
    // ----------------------------------------------------------------

    /// <summary>
    /// Returns the current lifecycle state of the given idempotency key,
    /// along with the response if the command has completed or failed.
    ///
    /// Expiry is evaluated here for query purposes using the
    /// application’s UTC clock.
    /// </summary>
    public async Task<(IdempotencyState State, string? Response)> GetStateAsync(string key)
    {
        ValidateKey(key);

        var now = DateTime.UtcNow;

        // Read the record.  Expired rows are ignored.
        var record = await _context.Set<IdempotencyRecord>()
            .AsNoTracking()                               // read‑only – faster
            .SingleOrDefaultAsync(r => r.Key == key && r.ExpiresAtUtc > now)
            .ConfigureAwait(false);

        if (record is null)
            return (IdempotencyState.NotFound, null);

        // Map the Status string to an IdempotencyState enum value.
        return record.Status switch
        {
            "InProgress" => (IdempotencyState.InProgress, null),
            "Completed" => (IdempotencyState.Completed, record.Response),
            "Failed" => (IdempotencyState.Failed, record.Response),
            _ => (IdempotencyState.NotFound, null)   // defensive fallback
        };
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Idempotency key cannot be null or whitespace.", nameof(key));
        if (key.Length > MaxKeyLength)
            throw new ArgumentException(
                $"Idempotency key exceeds maximum length of {MaxKeyLength} characters.", nameof(key));
    }
}

//Dry‑run – complete lifecycle with the store:

//Client sends: POST /api/v1/smartlocks/{id}/ unlock
//Headers: Idempotency - Key: abc - 123 - def

//First request(key is new):
//  1.TryReserveAsync("abc-123-def")
//     → UPSERT inserts new row, status = InProgress.
//     → Returns(Acquired, "exec-1").
//  2.Command executes and returns 200 OK.
//  3. StoreResponseAsync("abc-123-def", "exec-1", body)
//     → UPDATE sets Response, Status = Completed.
//  4. Client receives 200 OK.

//Duplicate request (same key, same client):
//  1.TryReserveAsync("abc-123-def")
//     → UPSERT finds non‑expired row → rowsAffected = 0.
//     → Returns (AlreadyExists, null).
//  2. GetStateAsync("abc-123-def")
//     → Returns (Completed, storedBody).
//  3. Middleware replays the original response.

//Command failure:
//  1.TryReserveAsync → (Acquired, "exec-2").
//  2.Command throws or returns non‑2xx.
//  3. MarkFailedAsync("abc-123-def", "exec-2", errorPayload)
//     → UPDATE sets Response, Status = Failed.
//  4. Client receives the error response.
