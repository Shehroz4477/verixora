// ====================================================================
// VERIXORA – Identity.Application / Commands / RegisterUser / RegisterUserCommand.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements the "Register User" use case using the CQRS +
//   Vertical Slice pattern.  The command carries the input data;
//   the handler orchestrates the domain logic and returns a
//   typed Result.
//
//   WHY CQRS:
//     - Separates the write model (command) from the read model
//       (query).  Each can evolve independently.
//     - The handler is a pure orchestrator; domain entities
//       contain the business rules.
//
//   FLOW:
//     1. The API controller receives POST /api/v1/auth/register.
//     2. MediatR dispatches RegisterUserCommand to this handler.
//     3. FluentValidation pipeline validates the command shape.
//     4. Handler checks business rules (duplicate email).
//     5. Password is hashed using Argon2id (async).
//     6. User aggregate is created (time injected for determinism).
//     7. Personal Home aggregate is created (time injected).
//     8. Both aggregates are persisted atomically via UnitOfWork.
//     9. Result carrying the new UserId and Email is returned.
//
//   DEPENDENCIES:
//     - IUserRepository  – persistence + duplicate check
//     - IHomeRepository  – persistence
//     - IPasswordHasher  – Argon2id hashing
//     - IClock           – deterministic time source
//     - IUnitOfWork      – transaction boundary
//
//   WHY THESE ABSTRACTIONS:
//     - Repositories decouple the handler from EF Core.
//     - IPasswordHasher decouples from the Argon2id library.
//     - IClock makes the handler testable (inject a fixed time).
//     - IUnitOfWork guarantees atomicity across two aggregates.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **record** (C# 9+) for the command and response:
//    - A record is a reference type designed for immutable data.
//    - It provides value‑based equality: two records with the same
//      property values are equal.
//    - Primary constructor parameters (C# 12) become public
//      properties automatically.
//    - `sealed` prevents further inheritance.
//
// 2. **ICommand<TResponse>** (from SharedKernel):
//    - A generic marker interface that ties a command to its
//      expected response type.
//    - Enables the MediatR pipeline to know the return type.
//    - `ICommand<RegisterUserResponse>` means this command
//      returns a `Result<RegisterUserResponse>`.
//
// 3. **ICommandHandler<TCommand, TResponse>** (from SharedKernel):
//    - Contract for handling a specific command.
//    - The generic parameters are:
//        TCommand  – the command type (must implement ICommand<TResponse>)
//        TResponse – the type of data returned on success
//    - The handler must implement:
//        Task<Result<TResponse>> Handle(TCommand, CancellationToken)
//
// 4. **Result<T>** (from SharedKernel):
//    - A functional return type (also called a "discriminated union"
//      or "Either monad").
//    - `Result<T>.Success(value)` creates a success result.
//    - `Result<T>.Failure(error)` creates a failure result.
//    - The caller checks `IsSuccess` before accessing `Value`.
//    - This avoids throwing exceptions for expected failures.
//
// 5. **Constructor injection**:
//    - All dependencies are passed via the constructor and stored
//      in `private readonly` fields.
//    - `readonly` ensures they cannot be reassigned after
//      construction.
//    - This makes the class testable: you can inject mock
//      implementations in unit tests.
//
// 6. **async / await**:
//    - `async` marks a method as asynchronous.
//    - `await` suspends the method until the awaited task completes,
//      without blocking the current thread.
//    - The method returns a `Task<T>` or `Task`.
//    - All I/O (database, password hashing) is async to keep the
//      thread pool free.
//
// 7. **CancellationToken** (`ct`):
//    - A struct that signals when an operation should be cancelled
//      (e.g., HTTP request timeout, server shutdown).
//    - It is propagated to every async call (`HashAsync`,
//      `AddAsync`, `SaveChangesAsync`) so the operation stops
//      immediately when cancellation is requested.
//    - The caller (MediatR / ASP.NET Core) provides it.
//
// 8. **private readonly** fields:
//    - `readonly` means the field can only be assigned in the
//      constructor or field initialiser.  After construction, the
//      reference cannot be changed.
//    - This makes the handler stateless and thread‑safe.
//
// 9. **IClock** abstraction:
//    - Wraps `DateTime.UtcNow` so that the current time can be
//      controlled in tests.
//    - The handler never calls `DateTime.UtcNow` directly.
//    - This makes the handler deterministic: given the same inputs
//      and the same clock, it always produces the same output.
// ====================================================================

using System.Numerics;
using Identity.Application.Commands.RegisterUser;
using System.Xml.Linq;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.Win32;
using SharedKernel.Domain.Base;
using SharedKernel.Domain.Results;
using static System.Net.WebRequestMethods;
using SharedKernel.Application.Abstractions;

namespace Identity.Application.Commands.RegisterUser;

/// <summary>
/// Command to register a new user.
/// </summary>
public sealed record RegisterUserCommand(
    string Email,
    string Password)
    : ICommand<RegisterUserResponse>;

/// <summary>
/// Handles the registration use case.
/// </summary>
public sealed class RegisterUserHandler
    : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    // ----------------------------------------------------------------
    // Dependencies (all injected via constructor)
    // ----------------------------------------------------------------

    private readonly IUserRepository _users;       // persistence + duplicate check
    private readonly IHomeRepository _homes;       // persistence
    private readonly IPasswordHasher _passwordHasher; // Argon2id hashing
    private readonly IClock _clock;                // deterministic time source
    private readonly IUnitOfWork _unitOfWork;       // transaction boundary

    /// <summary>
    /// Initialises the handler with all required dependencies.
    /// </summary>
    public RegisterUserHandler(
        IUserRepository users,
        IHomeRepository homes,
        IPasswordHasher passwordHasher,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _homes = homes;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<RegisterUserResponse>> Handle(
        RegisterUserCommand command,
        CancellationToken ct)
    {
        // ============================================================
        // Step 1: Business rule – duplicate email check
        // ============================================================
        // Shape validation (null/empty, email format, password length)
        // is already done by the FluentValidation pipeline behaviour.
        // Here we only check the business rule.
        if (await _users.ExistsByEmailAsync(command.Email, ct))
            return Result<RegisterUserResponse>.Failure(
                "A user with this email already exists.");

        // ============================================================
        // Step 2: Get the current time from the injected clock
        // ============================================================
        // IClock.UtcNow is used instead of DateTime.UtcNow so that
        // the handler is deterministic and testable.  In production,
        // this returns the real UTC time.  In tests, it returns a
        // fixed value so that assertions are predictable.
        var now = _clock.UtcNow;

        // ============================================================
        // Step 3: Hash the password using Argon2id (async)
        // ============================================================
        // Argon2id is memory‑hard and computationally expensive.
        // We call it asynchronously to avoid blocking the thread.
        // The raw password is never stored; only the hash is persisted.
        var passwordHash = await _passwordHasher.HashAsync(
            command.Password, ct);

        // ============================================================
        // Step 4: Create the User aggregate
        // ============================================================
        // User.Register is the only way to create a new User.
        // It validates the email format, normalises the email to
        // lowercase, and raises a UserRegistered domain event.
        // All time values are injected for determinism.
        var user = User.Register(command.Email, passwordHash, now);

        // ============================================================
        // Step 5: Create a personal Home for the new user
        // ============================================================
        // Every user gets a default personal Home named "My Home".
        // The founding user is automatically added as the Owner.
        // This raises a HomeCreated domain event.
        var home = Home.Create("My Home", user.Id, now);

        // ============================================================
        // Step 6: Persist both aggregates atomically
        // ============================================================
        // IUserRepository.AddAsync and IHomeRepository.AddAsync only
        // track the entities in the DbContext.  The actual database
        // commit happens in SaveChangesAsync, which:
        //   - Persists both aggregates in a single transaction.
        //   - Dequeues domain events into the OutboxMessages table.
        //   - Returns the number of affected rows.
        // If either aggregate fails to save, the entire transaction
        // is rolled back.
        await _users.AddAsync(user, ct);
        await _homes.AddAsync(home, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // ============================================================
        // Step 7: Return success with the new user's details
        // ============================================================
        return Result<RegisterUserResponse>.Success(
            new RegisterUserResponse(user.Id, user.Email));
    }
}

/// <summary>
/// The response returned after a successful registration.
/// </summary>
/// <param name="UserId">The ULID of the newly created user.</param>
/// <param name="Email">The normalised email address.</param>
public sealed record RegisterUserResponse(Ulid UserId, string Email);


//Dry‑run — complete registration flow:
//Client sends: POST /api/v1/auth/register
//Body: { "email": "Alice@Example.com", "password": "s3cr3t!" }

//1.ASP.NET Core deserialises the JSON into a RegisterUserCommand.
//   - Email = "Alice@Example.com"
//   - Password = "s3cr3t!"

//2. FluentValidation pipeline validates:
//   -Email is not null / empty ✓
//   -Email format is valid ✓
//   -Password is not null / empty ✓
//   -Password length >= 8 ✓
//   → All checks pass.

//3. MediatR dispatches the command to RegisterUserHandler.

//4. Handler: Check duplicate email
//   → _users.ExistsByEmailAsync("Alice@Example.com") → false
//   → No existing user.  Proceed.

//5. Handler: Get current time
//   → _clock.UtcNow → 2026-06-07 10:00:00 UTC

//6. Handler: Hash password
//   → _passwordHasher.HashAsync("s3cr3t!")
//   → Returns "$argon2id$v=19$m=65536,t=3,p=4$..." (hashed)

//7. Handler: Create User aggregate
//   → User.Register("Alice@Example.com", "$argon2id$...", 10:00)
//   → User is created:
//       Id = "01HXYZA..."(ULID)
//       Email = "alice@example.com"(lowercased)
//       PasswordHash = "$argon2id$..."
//       EmailVerified = false
//       CreatedAt = 2026 - 06 - 07 10:00 UTC
//   → Domain event UserRegistered is raised.

//8. Handler: Create Home aggregate
//   → Home.Create("My Home", user.Id, 10:00)
//   → Home is created:
//       Id = "01HXYZB..."(ULID)
//       Name = "My Home"
//       Members = [{ UserId: user.Id, Role: Owner }]
//       MaxDevices = 20
//   → Domain event HomeCreated is raised.

//9. Handler: Persist
//   → _users.AddAsync(user) → DbContext tracks the User.
//   → _homes.AddAsync(home) → DbContext tracks the Home.
//   → _unitOfWork.SaveChangesAsync()
//     → SQL INSERT into Users table.
//     → SQL INSERT into Homes table.
//     → SQL INSERT into HomeMemberships table.
//     → Domain events dequeued → INSERT into OutboxMessages table.
//     → Transaction committed.

//10. Handler: Return response
//    → Result<RegisterUserResponse>.Success(
//        new RegisterUserResponse(user.Id, "alice@example.com"))

//11.ASP.NET Core serialises the response:
//    { "userId": "01HXYZA...", "email": "alice@example.com" }
//HTTP 201 Created
