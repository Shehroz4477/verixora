// ====================================================================
// VERIXORA – Identity.Application / Commands / Login / LoginCommand.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements the "Login" use case.  The command carries the
//   email and password; the handler authenticates the user, creates
//   a session, issues a refresh token, and returns the JWT access
//   token and refresh token.
//
//   FLOW:
//     1. Validate shape (FluentValidation pipeline).
//     2. Find user by email (repository).
//     3. Verify password (IPasswordHasher).
//     4. Check email is verified (business rule).
//     5. Create a new session (User.AddSession).
//     6. Issue a refresh token (User.IssueRefreshToken).
//     7. Persist changes (IUnitOfWork).
//     8. Generate JWT access token (IJwtTokenService).
//     9. Return tokens and session info.
//
//   DEPENDENCIES:
//     - IUserRepository  – find user by email + persist changes
//     - IPasswordHasher  – verify password
//     - IClock           – deterministic time source
//     - IUnitOfWork      – transaction boundary
//     - IJwtTokenService – generate JWT access token
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **record** (C# 9+) for command and response – immutable data.
// 2. **ICommand<TResponse>** – generic marker tying command to response.
// 3. **ICommandHandler<TCommand, TResponse>** – handler contract.
// 4. **Result<T>** – functional return type (success with value or failure).
// 5. **Constructor injection** – all dependencies explicit.
// 6. **async / await** – non‑blocking I/O.
// 7. **CancellationToken** – propagated to all async calls.
// ====================================================================

using Identity.Application.Interfaces;
using SharedKernel.Application.Abstractions;
using SharedKernel.Domain.Base;
using SharedKernel.Domain.Results;

namespace Identity.Application.Commands.Login;

/// <summary>
/// Command to authenticate a user and create a session.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password,
    string DeviceFingerprint,
    string IpAddress,
    string UserAgent)
    : ICommand<LoginResponse>;

/// <summary>
/// Handles the login use case.
/// </summary>
public sealed class LoginHandler
    : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;

    /// <summary>
    /// Initialises the handler with required dependencies.
    /// </summary>
    public LoginHandler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IClock clock,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
    }

    /// <inheritdoc />
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand command,
        CancellationToken ct)
    {
        // ============================================================
        // Step 1: Find the user by email
        // ============================================================
        var user = await _users.GetByEmailAsync(
            command.Email.ToLowerInvariant().Trim(), ct);

        if (user is null)
            return Result<LoginResponse>.Failure(
                "Invalid email or password.");

        // ============================================================
        // Step 2: Verify the password
        // ============================================================
        var passwordValid = await _passwordHasher.VerifyAsync(
            command.Password, user.PasswordHash, ct);

        if (!passwordValid)
            return Result<LoginResponse>.Failure(
                "Invalid email or password.");

        // ============================================================
        // Step 3: Check that the email is verified
        // ============================================================
        if (!user.EmailVerified)
            return Result<LoginResponse>.Failure(
                "Email not verified. Please check your inbox.");

        // ============================================================
        // Step 4: Get the current time
        // ============================================================
        var now = _clock.UtcNow;

        // ============================================================
        // Step 5: Create a new session
        // ============================================================
        var session = user.AddSession(
            command.DeviceFingerprint,
            command.IpAddress,
            command.UserAgent,
            now);

        // ============================================================
        // Step 6: Issue a refresh token
        // ============================================================
        var refreshTokenExpiresAt = now.AddDays(30);
        var refreshToken = user.IssueRefreshToken(
            Guid.NewGuid().ToString("N"), // placeholder — real token hash
            refreshTokenExpiresAt);

        // ============================================================
        // Step 7: Persist the changes (new session + refresh token)
        // ============================================================
        await _unitOfWork.SaveChangesAsync(ct);

        // ============================================================
        // Step 8: Generate the JWT access token
        // ============================================================
        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(
            user.Id, session.Id, ct);

        // ============================================================
        // Step 9: Return the tokens and session info
        // ============================================================
        return Result<LoginResponse>.Success(new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.TokenHash, // raw token in production
            ExpiresAt: session.ExpiresAt,
            SessionId: session.Id,
            IsTrusted: session.IsTrusted));
    }
}

/// <summary>
/// Response returned after a successful login.
/// </summary>
/// <param name="AccessToken">The JWT access token (15‑minute expiry).</param>
/// <param name="RefreshToken">The refresh token (30‑day expiry).</param>
/// <param name="ExpiresAt">When the access token expires (UTC).</param>
/// <param name="SessionId">The ULID of the newly created session.</param>
/// <param name="IsTrusted">Whether this device was already trusted.</param>
public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    Ulid SessionId,
    bool IsTrusted);
