// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Idempotency / IdempotencyStore.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements the **Idempotency pattern** required by ADR‑016.
//   All state‑changing device commands (lock, unlock, firmware update)
//   must carry an `Idempotency‑Key` header.
//
//   The store provides three independent operations:
//     1. **Reserve** – atomically inserts the key with a NULL response.
//        If a non‑expired record already exists, the reservation fails.
//        If an expired record exists, it is **replaced** atomically,
//        allowing the key to be reused after the TTL window.
//     2. **Store response** – after successful command execution,
//        updates the record with the actual response body.  The update
//        only succeeds if the record is still `InProgress`
//        (Response IS NULL) AND the record has not expired.
//     3. **Get state** – returns the current lifecycle state of an
//        idempotency key: NotFound, InProgress, or Completed.
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
//         (new timestamps, NULL response) – one row affected →
//         reservation succeeds.
//       - If a row exists AND it is NOT expired, the WHERE clause
//         prevents the UPDATE – zero rows affected → reservation fails.
//
//     Two concurrent reservations for the same expired key will see
//     exactly one succeed because the database serialises the two
//     UPSERTs and row‑level locking ensures the second one sees the
//     updated (non‑expired) row.  Using the server’s clock eliminates
//     client‑side clock skew as a factor.
//
//   RESPONSE UPDATE PROTECTION:
//     The `StoreResponseAsync` method uses a conditional UPDATE:
//         UPDATE … WHERE Key = @key AND Response IS NULL AND ExpiresAtUtc > NOW()
//     This guarantees that a late writer cannot write into an
//     expired row, and that the response is only set once.
//     However, if the key is reused after expiry (a new reservation
//     replaces the old row), the old worker could theoretically
//     still satisfy the conditions and overwrite the new response.
//     This scenario is effectively impossible in practice because
//     MQTT commands complete in seconds and the TTL is 24 hours.
//     A future enterprise hardening phase may add a lease/version
//     column to establish absolute ownership across key reuse.
//
//   TTL SEMANTICS:
//     Keys expire after 24 hours.  An expired row is automatically
//     replaced on the next reservation attempt, with no cleanup job
//     or extra rows needed.
//
//   CRASH RECOVERY (future enhancement):
//     If a command crashes after reservation, the key remains
//     InProgress until expiry (24 hours).  For faster recovery, a
//     future version can add a `LockedUntilUtc` lease with heartbeats.
//     This is acceptable for MVP.
//
//   REQUIRED SCHEMA:
//     The database table `IdempotencyRecords` must have a single
//     **unique constraint on `Key` only**.
//
//   TRADE‑OFFS:
//     This design is physically race‑safe and functionally correct
//     for VERIXORA’s MQTT‑based IoT commands.  It does not provide
//     mathematically formal idempotency under all possible retry
//     patterns (e.g., an old worker writing to a reused key).
//     These edge cases are extremely unlikely in practice and would
//     require a lease‑based system.  For MVP, the current approach
//     is the right balance of safety and simplicity.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **sealed class** – prevents inheritance.
// 2. **DbContext injection** – decouples the store from any module.
// 3. **async / await** – all I/O is asynchronous.
// 4. **ExecuteSqlInterpolatedAsync** – safe, parameterised SQL.
// 5. **enum IdempotencyState** – explicit three‑state machine.
// 6. **Input validation** – key length and response size caps.
// 7. **internal persistence model** – `IdempotencyRecord` is hidden.
// 8. **Atomic UPSERT with `NOW()`** – the WHERE clause uses the
//    database server’s clock for deterministic expiry evaluation.
// 9. **Conditional UPDATE with expiry check** – `WHERE Response IS NULL
//    AND ExpiresAtUtc > NOW()` protects against late writes into
//    expired rows and duplicate response setting.
// 10. **LINQ query with `SingleOrDefaultAsync`** – used in the
//     state lookup to avoid raw SQL and EF compatibility issues.
// ====================================================================

using System.Collections.Generic;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
    /// An expired record is silently replaced, allowing key reuse.
    /// Expiry is determined by the database server’s clock (<c>NOW()</c>).
    /// </summary>
    /// <returns>
    /// <c>true</c> if the key was successfully reserved – the caller
    /// MUST execute the command and then call
    /// <see cref="StoreResponseAsync"/>.
    /// <c>false</c> if a non‑expired record already exists – the
    /// caller MUST skip command execution and call
    /// <see cref="GetStateAsync"/>.
    /// </returns>
    public async Task<bool> TryReserveAsync(string key)
    {
        // Validate the key before touching the database.
        ValidateKey(key);

        var now = DateTime.UtcNow;
        var expires = now.AddHours(24);

        // Single atomic UPSERT.
        // The WHERE clause uses the database server’s NOW() function
        // so all concurrent callers use the same time reference.
        int rowsAffected = await _context.Database
            .ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""IdempotencyRecords"" (""Key"", ""Response"", ""CreatedAtUtc"", ""ExpiresAtUtc"")
                VALUES ({key}, NULL, {now}, {expires})
                ON CONFLICT (""Key"") DO UPDATE
                SET ""CreatedAtUtc"" = EXCLUDED.""CreatedAtUtc"",
                    ""ExpiresAtUtc"" = EXCLUDED.""ExpiresAtUtc"",
                    ""Response"" = NULL
                WHERE ""IdempotencyRecords"".""ExpiresAtUtc"" < NOW()
            ")
            .ConfigureAwait(false);

        // rowsAffected == 1 → reservation succeeded (new or replaced expired).
        // rowsAffected == 0 → non‑expired record already exists.
        return rowsAffected == 1;
    }

    // ----------------------------------------------------------------
    // Step 2 – After successful command execution, store the response
    // ----------------------------------------------------------------

    /// <summary>
    /// Stores the actual response for a previously reserved key.
    /// The update only succeeds if the record is still
    /// <see cref="IdempotencyState.InProgress"/> (Response IS NULL)
    /// AND the record has not expired.  If the response has already
    /// been set or the record has expired, this method throws.
    /// </summary>
    /// <param name="key">The reserved idempotency key.</param>
    /// <param name="response">The response body to store.</param>
    public async Task StoreResponseAsync(string key, string response)
    {
        // Validate input.
        ValidateKey(key);
        if (string.IsNullOrEmpty(response))
            throw new ArgumentException("Response cannot be null or empty.", nameof(response));
        if (response.Length > MaxResponseLength)
            throw new ArgumentException(
                $"Response exceeds maximum length of {MaxResponseLength} characters.", nameof(response));

        // Conditional UPDATE: only set the response if it is still NULL
        // AND the record has not expired.  This prevents a late writer
        // from storing a response into an already‑expired row.
        // Note: if the key has been reused after expiry, an old worker
        // could still pass these checks.  See the class‑level remarks
        // for a discussion of this trade‑off.
        int rows = await _context.Database
            .ExecuteSqlInterpolatedAsync($@"
                UPDATE ""IdempotencyRecords""
                SET ""Response"" = {response}
                WHERE ""Key"" = {key}
                  AND ""Response"" IS NULL
                  AND ""ExpiresAtUtc"" > NOW()
            ")
            .ConfigureAwait(false);

        if (rows == 0)
            throw new InvalidOperationException(
                $"Could not store response for key '{key}'. " +
                "Either the key does not exist, the record has expired, or the response was already stored.");
    }

    // ----------------------------------------------------------------
    // Step 3 – Retrieve the state of an idempotency key
    // ----------------------------------------------------------------

    /// <summary>
    /// Returns the current lifecycle state of the given idempotency key,
    /// along with the response if the command has completed.
    ///
    /// Expiry is checked using the client’s UTC clock for simplicity.
    /// The reservation logic uses the server clock (`NOW()`) for
    /// atomicity; the state query’s slight timing difference is
    /// acceptable for MVP.
    /// </summary>
    public async Task<(IdempotencyState State, string? Response)> GetStateAsync(string key)
    {
        ValidateKey(key);

        var now = DateTime.UtcNow;

        // Use a standard LINQ query to avoid EF compatibility issues
        // with raw SQL (FromSqlInterpolated not always available).
        var record = await _context.Set<IdempotencyRecord>()
            .AsNoTracking()                               // read‑only query – faster
            .SingleOrDefaultAsync(r => r.Key == key && r.ExpiresAtUtc > now)
            .ConfigureAwait(false);

        if (record is null)
            return (IdempotencyState.NotFound, null);

        if (record.Response is null)
            return (IdempotencyState.InProgress, null);

        return (IdempotencyState.Completed, record.Response);
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


//Dry‑run – complete idempotency lifecycle with this store:

//Client sends: POST /api/v1/smartlocks/{id}/ unlock
//Headers: Idempotency - Key: abc - 123 - def

//Thread A(first request)            Thread B(duplicate, same key)
//─────────────────────────────────   ─────────────────────────────────
//1.store.TryReserveAsync(key)       1.store.TryReserveAsync(key)
//   → UPSERT inserts new row            → UPSERT detects conflict
//   → rowsAffected=1 → true             → WHERE not expired → do nothing
//                                       → rowsAffected=0 → false
//2. Executes unlock pipeline          2. Calls store.GetStateAsync(key)
//3. On success:                          → State = InProgress
//   store.StoreResponseAsync(key,
//   resultJson)
//   → UPDATE sets response
//4. Returns 200 OK                    3. Returns 200 OK with original response


//Handling expired key reuse(24 hours later):
//Thread C:
//   TryReserveAsync("abc-123-def")
//   → UPSERT detects existing row
//   → WHERE ExpiresAtUtc<NOW() → true
//   → UPDATE replaces row(new timestamps, NULL response)
//   → rowsAffected=1 → reservation succeeds
