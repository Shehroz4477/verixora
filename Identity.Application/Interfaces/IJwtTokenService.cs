// ====================================================================
// VERIXORA – Identity.Application / Interfaces / IJwtTokenService.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Defines the contract for generating and validating JWT access
//   tokens.  This interface lives in the Application layer and is
//   implemented in the Infrastructure layer, following the
//   Dependency Inversion Principle.
//
//   WHY A SEPARATE ABSTRACTION:
//     - JWT generation involves cryptographic signing, which is an
//       infrastructure concern (keys, algorithms, expiry).
//     - Command handlers should not depend on JWT libraries directly.
//     - Enables unit testing of handlers without real token generation.
//     - Allows swapping the token format or signing algorithm without
//       touching application logic.
//
//   TOKEN LIFETIME (per VERIXORA Master Spec):
//     - Access token:  15 minutes (JWT)
//     - Refresh token: 30 days (opaque, stored in database)
//
//   METHODS:
//     - GenerateAccessTokenAsync – creates a signed JWT for a session
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **interface** keyword:
//    - Defines a contract without implementation.  Any class that
//      implements this interface MUST provide the declared method.
//
// 2. **public** access modifier:
//    - The interface is accessible from any referencing project.
//
// 3. **Task<string>** return type:
//    - `GenerateAccessTokenAsync` returns the signed JWT as a string
//      asynchronously.  The operation is async because it may involve
//      I/O (reading signing keys from a secrets manager).
//
// 4. **Ulid** parameters:
//    - Strong typing for the user and session identifiers.
//    - The JWT payload will include these as claims (`sub` and
//      `sid` respectively).
//
// 5. **CancellationToken** parameter:
//    - Allows the caller to cancel the token generation if the
//      request times out or the server shuts down.
//
// 6. **namespace** declaration:
//    - `Identity.Application.Interfaces` places this in the
//      Application layer's abstraction space.
// ====================================================================

using Identity.Domain.Entities;
using SharedKernel.Domain.Base;

namespace Identity.Application.Interfaces;

/// <summary>
/// Contract for generating JWT access tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT access token for the given user and
    /// session.  The token includes claims for UserId (sub) and
    /// SessionId (sid), and expires after 15 minutes.
    /// </summary>
    /// <param name="userId">The user's ULID.</param>
    /// <param name="sessionId">The session's ULID.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    /// A signed JWT string (e.g., "eyJhbGciOiJIUzI1NiIs...").
    /// </returns>
    Task<string> GenerateAccessTokenAsync(
        Ulid userId,
        Ulid sessionId,
        CancellationToken ct = default);
}


////Dry‑run — how the token service is used during login:
//// ====================================================================
//// SCENARIO: Successful login
//// ====================================================================
//// After the handler validates the user's password and creates a
//// session, it generates the JWT:

//var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(
//    user.Id,       // sub claim
//    session.Id,    // sid claim
//    ct);

//// Return value (example):
////   "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.
////    eyJzdWIiOiIwMUhYWVpBIiwic2lkIjoiMDFIWFlaQiIsImV4cCI6MTcxNzc3MjgwMH0.
////    signature"
////
//// The token contains:
////   - sub (subject): the user's ULID
////   - sid (session ID): the session's ULID
////   - exp (expiration): 15 minutes from now
////   - iat (issued at): current time
////
//// The handler returns this to the client, which includes it in
//// the Authorization header for subsequent requests:
////   Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
