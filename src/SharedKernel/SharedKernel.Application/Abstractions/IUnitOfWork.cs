// ====================================================================
// VERIXORA – SharedKernel.Application / Abstractions / IUnitOfWork.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Defines the contract for a transaction boundary across one or
//   more aggregate repositories.  It guarantees that all changes
//   within a single business operation are persisted atomically:
//   either everything succeeds, or everything is rolled back.
//
//   WHY A SEPARATE ABSTRACTION:
//     - Decouples command handlers from EF Core's DbContext.
//     - Allows the Application layer to commit changes without
//       knowing about the underlying ORM or database.
//     - Enables unit testing by mocking the Unit of Work.
//     - Provides a single point where domain events are dequeued
//       from aggregates and written to the outbox table (this
//       happens inside the EF Core implementation via
//       BaseDbContext.SaveChangesAsync).
//
//   TYPICAL FLOW IN A COMMAND HANDLER:
//     1. Load aggregates via repositories (GetByIdAsync, etc.).
//     2. Call domain methods (VerifyEmail, AddMember, etc.).
//     3. Track new aggregates via repositories (AddAsync).
//     4. Call SaveChangesAsync() ONCE to persist everything.
//
//     The handler never calls SaveChangesAsync() on individual
//     repositories — only on the Unit of Work.  This guarantees
//     that all changes are committed in a single database
//     transaction.
//
//   IMPLEMENTATION NOTE:
//     The concrete implementation in BuildingBlocks.Infrastructure
//     (or a module's Infrastructure layer) wraps EF Core's
//     DbContext.SaveChangesAsync().  It also handles:
//       - Domain event dispatch (dequeuing from aggregates and
//         writing to the OutboxMessages table).
//       - Transient fault retry (via EnableRetryOnFailure).
//       - Idempotent outbox insertion (same transaction as
//         aggregate changes).
//
//   WHY NOT IN SHAREDKERNEL.DOMAIN:
//     - The Domain layer is persistence‑ignorant.  It does not
//       know about transactions, databases, or repositories.
//     - The Unit of Work is an application‑level concern that
//       coordinates infrastructure (repositories + database).
//     - Placing it in SharedKernel.Application makes it available
//       to all modules without coupling them to EF Core.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **interface** keyword:
//    - Defines a contract without implementation.  Any class that
//      implements this interface MUST provide SaveChangesAsync.
//
// 2. **public** access modifier:
//    - The interface is accessible from any project that references
//      this assembly.
//
// 3. **Task<int>** return type:
//    - `SaveChangesAsync` returns the number of state entries
//      written to the database.  This matches EF Core's
//      SaveChangesAsync return value and can be used to verify
//      that changes were actually persisted.
//    - The operation is asynchronous because it performs I/O.
//
// 4. **CancellationToken** parameter:
//    - Allows the caller to cancel the database commit if the
//      request times out or the server shuts down.
//    - The default value `default` means "no cancellation
//      requested" — convenient for unit tests.
//
// 5. **namespace** declaration:
//    - `SharedKernel.Application.Abstractions` places this
//      alongside the CQRS abstractions (ICommand, IQuery, etc.).
// ====================================================================

namespace SharedKernel.Application.Abstractions;

/// <summary>
/// Represents a transaction boundary that commits all pending
/// changes across multiple repositories atomically.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes within a single database
    /// transaction.  This method also dequeues domain events
    /// from all tracked aggregate roots and writes them to the
    /// outbox table as part of the same transaction.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    /// The number of state entries written to the database.
    /// </returns>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}


////Dry‑run — how the Unit of Work is used in a command handler:
//// ====================================================================
//// SCENARIO: Registering a new user
//// ====================================================================
//// The handler orchestrates multiple repositories:

//public async Task<Result<RegisterUserResponse>> Handle(
//    RegisterUserCommand command, CancellationToken ct)
//    {
//        // 1. Check business rules (read, no transaction needed).
//        if (await _users.ExistsByEmailAsync(command.Email, ct))
//            return Result<RegisterUserResponse>.Failure("Email already exists.");

//        // 2. Create aggregates (in‑memory only — no SQL yet).
//        var user = User.Register(command.Email, passwordHash, now);
//        var home = Home.Create("My Home", user.Id, now);

//        // 3. Track aggregates for insertion (no SQL yet).
//        await _users.AddAsync(user, ct);     // DbContext.Users.Add(user)
//        await _homes.AddAsync(home, ct);     // DbContext.Homes.Add(home)

//        // 4. Commit everything atomically (SINGLE transaction).
//        int rowsAffected = await _unitOfWork.SaveChangesAsync(ct);

//        // What happens inside SaveChangesAsync():
//        //   BEGIN TRANSACTION
//        //   INSERT INTO Users (...) VALUES (...)
//        //   INSERT INTO Homes (...) VALUES (...)
//        //   INSERT INTO HomeMemberships (...) VALUES (...)
//        //   -- Domain events are dequeued from User and Home aggregates:
//        //   INSERT INTO OutboxMessages (...) VALUES (...)  -- UserRegistered
//        //   INSERT INTO OutboxMessages (...) VALUES (...)  -- HomeCreated
//        //   COMMIT TRANSACTION
//        //
//        // rowsAffected = 4 (User + Home + Membership + 2 outbox messages)

//        // 5. Return success.
//        return Result<RegisterUserResponse>.Success(
//            new RegisterUserResponse(user.Id, user.Email));
//    }


//// ====================================================================
//// WHAT IF SOMETHING FAILS?
//// ====================================================================
//// If the database is unreachable during SaveChangesAsync:
////   → A DbUpdateException (or similar) is thrown.
////   → The entire transaction is rolled back automatically.
////   → No partial data is left in the database.
////   → The handler can catch the exception and return a failure result,
////     or let it propagate to the global exception handler.

//// If the handler catches a business error BEFORE SaveChangesAsync:
////   → No database calls were made yet (repositories only tracked entities).
////   → The handler simply returns Result.Failure(...).
////   → The tracked entities are discarded when the DbContext is disposed.
////   → No cleanup is needed.
