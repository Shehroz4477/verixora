// ====================================================================
// VERIXORA – Identity.Application / Interfaces / IUserRepository.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Defines the contract for persisting and retrieving User
//   aggregates.  This interface lives in the Application layer
//   and is implemented in the Infrastructure layer (EF Core),
//   following the Dependency Inversion Principle.
//
//   WHY A REPOSITORY INTERFACE:
//     - Decouples command/query handlers from EF Core (or any
//       specific ORM).  The handler only knows about this
//       interface, not about DbContext or SQL.
//     - Enables unit testing: mock implementations can be
//       injected to test handlers in complete isolation.
//     - Allows swapping persistence technology (e.g., moving
//       from EF Core to Dapper, or from PostgreSQL to CosmosDB)
//       without changing any application logic.
//
//   WHY ONLY AGGREGATE ROOTS:
//     - Repositories should only persist aggregate roots.
//     - Child entities (Session, TrustedDevice, RefreshToken)
//       are persisted automatically as part of the User
//       aggregate via EF Core cascade rules.
//     - This enforces the aggregate consistency boundary:
//       you cannot accidentally save a child entity without
//       its parent aggregate.
//
//   WHY NO UpdateAsync():
//     - EF Core's change tracker automatically detects changes
//       to tracked entities.  The typical flow is:
//         1. Load the User via GetByIdAsync().
//         2. Call domain methods (VerifyEmail, AddSession, etc.).
//         3. Call IUnitOfWork.SaveChangesAsync().
//       No explicit "update" call is needed.  This keeps the
//       repository minimal and consistent with IHomeRepository.
//
//   METHODS:
//     - GetByIdAsync       – fetch a user by their ULID
//     - GetByEmailAsync    – fetch a user by email (for login,
//                            password reset, duplicate check)
//     - ExistsByEmailAsync – fast boolean existence check
//                            (more efficient than loading the
//                            full aggregate)
//     - AddAsync           – track a new user for insertion
//
//   CONSISTENCY:
//     Both IUserRepository and IHomeRepository follow the same
//     minimal pattern: GetById, GetBy*, Add, and an optional
//     existence check — no UpdateAsync().
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **interface** keyword:
//    - Defines a contract without any implementation.  Any class
//      that implements this interface MUST provide all four methods.
//    - The compiler enforces this at build time.
//    - This enables polymorphism: different implementations can
//      be swapped without changing the code that depends on them.
//
// 2. **public** access modifier:
//    - The interface and all its members are accessible from any
//      other project in the solution that references this assembly.
//
// 3. **Task<T>** return types:
//    - `Task<User?>` means the method runs asynchronously and
//      returns either a User object or null.
//    - `Task<bool>` means the method runs asynchronously and
//      returns true or false.
//    - `Task` (no generic) means the method runs asynchronously
//      and returns no value.
//    - All methods are async because they perform I/O (database
//      queries).  Async methods free the current thread to handle
//      other requests while waiting for the database.
//
// 4. **Ulid** parameter type:
//    - Using our custom ULID type instead of `Guid` or `string`
//      provides strong typing.  The compiler prevents you from
//      accidentally passing the wrong kind of identifier.
//    - ULIDs are time‑sortable and better for database indexes
//      than random GUIDs.
//
// 5. **CancellationToken** parameter (`ct`):
//    - A struct that signals when an operation should be cancelled
//      (e.g., the HTTP request timed out, the server is shutting
//      down, or the caller is no longer interested in the result).
//    - Every async I/O method should accept one and pass it to
//      the underlying database call.
//    - The default value `default` means "no cancellation
//      requested" — convenient for unit tests.
//
// 6. **User?** (nullable reference type):
//    - The `?` after `User` means this method can return null.
//    - The compiler will warn if the caller tries to use the
//      result without first checking for null.
//    - This is a C# 8+ feature that helps prevent
//      NullReferenceException at compile time.
//
// 7. **namespace** declaration:
//    - `Identity.Application.Interfaces` clearly places this
//      interface in the Application layer, under the Interfaces
//      folder.  This follows the convention established across
//      the entire VERIXORA codebase.
// ====================================================================

using Identity.Domain.Entities;
using SharedKernel.Domain.Base;

namespace Identity.Application.Interfaces;

/// <summary>
/// Contract for persisting and retrieving <see cref="User"/> aggregates.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The user's ULID.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    /// The user if found; otherwise <c>null</c>.
    /// </returns>
    Task<User?> GetByIdAsync(Ulid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by their normalised email address (lowercase).
    /// Used during login and password reset flows.
    /// </summary>
    /// <param name="email">The email address (already lowercased).</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    /// The user if found; otherwise <c>null</c>.
    /// </returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a user with the given email already exists.
    /// This is more efficient than <see cref="GetByEmailAsync"/>
    /// because it performs an existence check (SELECT EXISTS…)
    /// without loading the full entity graph.
    /// </summary>
    /// <param name="email">The email address (already lowercased).</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    /// <c>true</c> if a user with this email exists; otherwise
    /// <c>false</c>.
    /// </returns>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Tracks a new user for insertion.  The actual database INSERT
    /// happens later when <c>SaveChangesAsync</c>
    /// is called.  This is the standard EF Core Unit of Work pattern.
    /// </summary>
    /// <param name="user">The user aggregate to insert.</param>
    /// <param name="ct">A cancellation token.</param>
    Task AddAsync(User user, CancellationToken ct = default);
}


//////Dry‑run — how the repository is used in the handler:
//// ====================================================================
//// SCENARIO 1: Registration — Check for duplicate email
//// ====================================================================
//// The handler calls:
//bool exists = await _users.ExistsByEmailAsync("alice@example.com", ct);

//    // SQL generated by EF Core:
//    //   SELECT EXISTS (
//    //     SELECT 1 FROM "Users" WHERE "Email" = 'alice@example.com'
//    //   )
//    //
//    // Result: false  →  No existing user.  Registration proceeds.
//    // Result: true   →  Handler returns Result.Failure("Email already exists.")


//    // ====================================================================
//    // SCENARIO 2: Registration — Insert a new user
//    // ====================================================================
//    // The handler creates the user and calls:
//    await _users.AddAsync(user, ct);

//    // EF Core: DbContext.Users.Add(user)
//    // The User entity is now tracked in the "Added" state.
//    // No SQL is executed yet.  All child entities (the initial
//    // HomeMembership created by Home.Create) are also tracked.

//    // Later, the handler calls:
//    await _unitOfWork.SaveChangesAsync(ct);

//    // EF Core generates and executes:
//    //   INSERT INTO "Users" ("Id", "Email", "PasswordHash", ...)
//    //   VALUES (@id, @email, @passwordHash, ...)
//    //
//    // The user is now persisted in the database.


//    // ====================================================================
//    // SCENARIO 3: Login — Find user by email
//    // ====================================================================
//    // The login handler calls:
//    var user = await _users.GetByEmailAsync("alice@example.com", ct);

//    // SQL generated by EF Core:
//    //   SELECT "u"."Id", "u"."Email", "u"."PasswordHash", ...
//    //   FROM "Users" AS "u"
//    //   WHERE "u"."Email" = 'alice@example.com'
//    //   LIMIT 1
//    //
//    // Result: User entity with all child collections loaded
//    //         (Sessions, TrustedDevices, RefreshTokens).
//    //
//    // If the user is not found, the result is null.
//    // The handler must check: if (user is null) return Result.Failure(...);


//    // ====================================================================
//    // SCENARIO 4: Session validation — Find user by ID
//    // ====================================================================
//    // The session validation handler calls:
//    var user = await _users.GetByIdAsync(someUlid, ct);

//    // SQL generated by EF Core:
//    //   SELECT "u"."Id", "u"."Email", ...
//    //   FROM "Users" AS "u"
//    //   WHERE "u"."Id" = @id
//    //   LIMIT 1
//    //
//    // Result: User entity, or null if the ID doesn't match any user.


//    // ====================================================================
//    // SCENARIO 5: Update — Verify email (no UpdateAsync needed)
//    // ====================================================================
//    // The handler calls:
//    var user = await _users.GetByIdAsync(userId, ct);
//// SQL: SELECT ... FROM "Users" WHERE "Id" = @id

//user.VerifyEmail();
//// Domain method changes EmailVerified from false to true.
//// This only modifies the in‑memory entity; no SQL yet.

//await _unitOfWork.SaveChangesAsync(ct);
//// EF Core detects the change and generates:
////   UPDATE "Users" SET "EmailVerified" = TRUE WHERE "Id" = @id
////
//// No UpdateAsync() call was needed.  The change tracker
//// handled everything automatically.
