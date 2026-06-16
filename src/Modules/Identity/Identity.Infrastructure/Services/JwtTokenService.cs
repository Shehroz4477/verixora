// ====================================================================
// VERIXORA – Identity.Infrastructure / Services / JwtTokenService.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements <see cref="IJwtTokenService"/> by generating signed
//   JWT access tokens using HMAC‑SHA256.  The secret key, issuer,
//   and audience are read from <see cref="JwtOptions"/>, which is
//   bound from configuration / secrets manager.
//
//   WHY HMAC‑SHA256:
//     - Simple, fast, and secure for single‑service architectures.
//     - If VERIXORA moves to a microservices model, RS256 (asymmetric)
//       can be introduced later via a new implementation.
//
//   TOKEN PAYLOAD (claims):
//     - sub   (Subject)   – the user's ULID
//     - sid   (Session ID) – the session's ULID
//     - jti   (JWT ID)    – a unique identifier for this token
//     - iat   (Issued At) – when the token was created
//     - exp   (Expiration) – when the token expires
//     - iss   (Issuer)    – the API host name
//     - aud   (Audience)  – the intended client application
//
//   WHY ASYNC:
//     - In the future, the secret key may be fetched from Azure Key
//       Vault or another remote secrets manager, which requires I/O.
//     - The interface is async to support that evolution without
//       breaking changes.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** implementing an interface:
//    - `JwtTokenService : IJwtTokenService` guarantees the
//      `GenerateAccessTokenAsync` method is provided.
//
// 2. **IOptions<T>** (Options pattern):
//    - `IOptions<JwtOptions>` provides validated configuration at
//      runtime.  The options are bound from appsettings.json and
//      secrets manager at startup.
//
// 3. **SymmetricSecurityKey** (Microsoft.IdentityModel.Tokens):
//    - Represents the HMAC‑SHA256 secret key used to sign tokens.
//
// 4. **SecurityTokenDescriptor**:
//    - Describes the token's claims, expiration, signing credentials,
//      issuer, and audience.
//
// 5. **JwtSecurityTokenHandler**:
//    - Creates the signed JWT string from the descriptor.
//
// 6. **Claim** (System.Security.Claims):
//    - A key‑value pair that represents a piece of information about
//      the token's subject.  Standard claims like "sub" and "sid"
//      are used here.
//
// 7. **Task.FromResult**:
//    - Wraps a synchronous result in a completed Task, fulfilling
//      the async contract without actually performing I/O.
//
// 8. **Encoding.UTF8.GetBytes**:
//    - Converts the secret key string into a byte array for the
//      signing algorithm.
//
// 9. **sealed** modifier:
//    - Prevents inheritance.
// ====================================================================

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Generates signed JWT access tokens using HMAC‑SHA256.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    /// <summary>
    /// Initialises the service with JWT configuration.
    /// </summary>
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<string> GenerateAccessTokenAsync(
        Ulid userId,
        Ulid sessionId,
        CancellationToken ct = default)
    {
        // ------------------------------------------------------------
        // 1. Create the signing key
        // ------------------------------------------------------------
        // The secret key from configuration is UTF‑8 encoded to bytes.
        var keyBytes = Encoding.UTF8.GetBytes(_options.SecretKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(
            signingKey, SecurityAlgorithms.HmacSha256);

        // ------------------------------------------------------------
        // 2. Build the claims
        // ------------------------------------------------------------
        var claims = new[]
        {
            // Subject – the user's unique identifier
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            // Session ID – ties this token to a specific session
            new Claim("sid", sessionId.ToString()),
            // JWT ID – a unique identifier for this token
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // ------------------------------------------------------------
        // 3. Set the expiry time
        // ------------------------------------------------------------
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        // ------------------------------------------------------------
        // 4. Create the token descriptor
        // ------------------------------------------------------------
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            IssuedAt = now,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = credentials
        };

        // ------------------------------------------------------------
        // 5. Generate the token string
        // ------------------------------------------------------------
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Return the signed JWT as a completed Task.
        return Task.FromResult(tokenString);
    }
}



//Dry‑run — how the JWT is generated and what it contains:
// =========================================================================
// CONFIGURATION (appsettings.json or Key Vault):
// =========================================================================
// "Jwt": {
//   "SecretKey": "my-super-secret-key-that-is-at-least-32-characters-long!",
//   "Issuer": "verixora-api",
//   "Audience": "verixora-client",
//   "AccessTokenLifetimeMinutes": 15
// }

// =========================================================================
// USAGE IN THE LOGIN HANDLER:
// =========================================================================
//var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(
    //user.Id, session.Id, ct);

// accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.
//                eyJzdWIiOiIwMUhYWVpBIiwic2lkIjoiMDFIWFlaQiIsImp0aSI6ImFiYzEyMy4uLiIsImlhdCI6MTcxNzc3MjgwMCwiZXhwIjoxNzE3NzczNzAwLCJpc3MiOiJ2ZXJpeG9yYS1hcGkiLCJhdWQiOiJ2ZXJpeG9yYS1jbGllbnQifQ.
//                signature"

// Decoded payload (Base64 → JSON):
// {
//   "sub": "01HXYZA...",       // user's ULID
//   "sid": "01HXYZB...",       // session's ULID
//   "jti": "abc123...",        // unique token ID
//   "iat": 1717772800,         // issued at (Unix timestamp)
//   "exp": 1717773700,         // expires at (15 minutes later)
//   "iss": "verixora-api",
//   "aud": "verixora-client"
// }
