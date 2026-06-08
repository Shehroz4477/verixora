// ====================================================================
// VERIXORA – Identity.Application / Commands / VerifyEmail / VerifyEmailCommand.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements the "Verify Email" use case.  The command carries
//   the user's ID; the handler marks the user's email as verified
//   and persists the change.
//
//   FLOW:
//     1. Find user by ID (repository).
//     2. Call user.VerifyEmail() (domain method — idempotent).
//     3. Persist the change (IUnitOfWork).
//     4. Return success.
//
//   WHY THIS IS A COMMAND (not a query):
//     - It changes the system state (EmailVerified flag).
//     - It should be idempotent — calling it multiple times is safe.
//
//   DEPENDENCIES:
//     - IUserRepository – find user + persist changes
//     - IUnitOfWork      – transaction boundary
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **record** (C# 9+) for command – immutable data.
// 2. **ICommand** (non‑generic) – returns a plain Result, no data.
// 3. **ICommandHandler<TCommand>** – handler contract for plain results.
// 4. **Result** – functional return type (success or failure).
// 5. **Constructor injection** – all dependencies explicit.
// 6. **async / await** – non‑blocking I/O.
// 7. **CancellationToken** – propagated to all async calls.
// ====================================================================

using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using SharedKernel.Application.Abstractions;
using SharedKernel.Domain.Base;
using SharedKernel.Domain.Results;

namespace Identity.Application.Commands.VerifyEmail;

/// <summary>
/// Command to verify a user's email address.
/// </summary>
public sealed record VerifyEmailCommand(Ulid UserId) : ICommand;

/// <summary>
/// Handles the email verification use case.
/// </summary>
public sealed class VerifyEmailHandler
    : ICommandHandler<VerifyEmailCommand>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initialises the handler.
    /// </summary>
    public VerifyEmailHandler(
        IUserRepository users,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(
        VerifyEmailCommand command,
        CancellationToken ct)
    {
        // ============================================================
        // Step 1: Find the user
        // ============================================================
        var user = await _users.GetByIdAsync(command.UserId, ct);

        if (user is null)
            return Result.Failure("User not found.");

        // ============================================================
        // Step 2: Verify the email (idempotent domain method)
        // ============================================================
        // If already verified, this is a no‑op — no error is thrown.
        user.VerifyEmail();

        // ============================================================
        // Step 3: Persist the change
        // ============================================================
        await _unitOfWork.SaveChangesAsync(ct);

        // ============================================================
        // Step 4: Return success
        // ============================================================
        return Result.Success();
    }
}

////Dry‑run — how the verify email flow works:
//// ====================================================================
//// SCENARIO 1: First verification
//// ====================================================================
//// User clicks the verification link in their email.
//// The link contains the user's ID.

//var command = new VerifyEmailCommand(user.Id);

//    // Handler:
//    var user = await _users.GetByIdAsync(user.Id, ct);
//// → User found.  EmailVerified = false.

//user.VerifyEmail();
//// → EmailVerified = true.
//// → UserEmailVerified domain event raised.

//await _unitOfWork.SaveChangesAsync(ct);
//// → SQL: UPDATE Users SET EmailVerified = TRUE WHERE Id = @id
//// → Outbox: INSERT UserEmailVerified event.

//return Result.Success();

//// ====================================================================
//// SCENARIO 2: Already verified (idempotent)
//// ====================================================================
//// User clicks the link again.

//var command = new VerifyEmailCommand(user.Id);

//var user = await _users.GetByIdAsync(user.Id, ct);
//// → User found.  EmailVerified = true.

//user.VerifyEmail();
//// → The method checks: if (EmailVerified) return;
//// → No change, no event raised.

//await _unitOfWork.SaveChangesAsync(ct);
//// → No SQL update (nothing changed).

//return Result.Success();
//// → Still returns success.  The client sees the same response.

//// ====================================================================
//// SCENARIO 3: User not found
//// ====================================================================
//var command = new VerifyEmailCommand(nonExistentUlid);

//var user = await _users.GetByIdAsync(nonExistentUlid, ct);
//// → null

//return Result.Failure("User not found.");
//// → The client sees a 404 or 400 error.
