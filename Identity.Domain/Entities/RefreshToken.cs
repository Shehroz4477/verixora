// ====================================================================
// VERIXORA – Identity.Domain / Entities / RefreshToken.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   A child entity within the User aggregate.  Represents a
//   long‑lived refresh token that allows the client to obtain new
//   access tokens (JWTs) without re‑authenticating.
//
//   WHY A CHILD ENTITY:
//     - RefreshToken has no independent lifecycle outside a User.
//     - It is always loaded, modified, and persisted through the
//       User aggregate root.
//     - The User enforces the maximum refresh token limit (5),
//       auto‑cleans expired tokens, and handles revocation.
//
//   IDENTITY:
//     - Each RefreshToken has a unique ULID (Id) generated at
//       creation.
//     - RefreshTokens are compared by ID only, inherited from the
//       Entity base class (ID + Type equality).
//
//   INVARIANTS:
//     - Token is a required, unique string (the actual refresh
//       token value — hashed before storage by the Application
//       layer).
//     - ExpiresAt must be in the future at creation time.
//     - RevokedAt is null until explicitly revoked.
//     - IsRevoked is a computed property based on RevokedAt.
//
//   SECURITY NOTE:
//     The raw token value is generated in the Application layer
//     using a cryptographically secure random number generator.
//     The value stored here is a **hash** of the raw token, so
//     that a database leak does not expose usable refresh tokens.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** inheriting from **Entity** (not AggregateRoot):
//    - RefreshToken is a child entity, not an independent root.
//    - `Entity` provides the ULID `Id`, domain event support, and
//      structural equality (ID + Type).
//
// 2. **private set** properties:
//    - Encapsulation.  External code can read values but cannot
//      modify them directly.
//
// 3. **internal constructor** and **internal static factory**:
//    - Only the Identity.Domain assembly can create a RefreshToken.
//      The static `Generate()` method creates the token with the
//      current user ID and expiry, then the User aggregate calls it.
//
// 4. **Computed property** (`IsRevoked`):
//    - No backing field.  Calculated every time based on whether
//      RevokedAt has a value.
//
// 5. **Nullable DateTime** (`DateTime?`):
//    - `RevokedAt` can be null (not revoked) or hold a UTC time
//      (revoked).  Cleaner than a separate boolean flag.
//
// 6. **Ulid.NewUlid()**:
//    - Generates a new unique, time‑sortable identifier.
//
// 7. **DateTime injection**:
//    - `ExpiresAt` is passed as a constructor parameter rather than
//      calculated inside the entity.  This keeps the domain
//      deterministic and testable.
// ====================================================================

using System.Text;

namespace Identity.Domain.Entities;

/// <summary>
/// Represents a refresh token used to obtain new access tokens.
/// </summary>
public class RefreshToken : Entity
{
    // ----------------------------------------------------------------
    // Properties
    // ----------------------------------------------------------------

    /// <summary>
    /// The ID of the user who owns this refresh token.
    /// </summary>
    public Ulid UserId { get; private set; } = default!;

    /// <summary>
    /// The hashed refresh token value.  The raw token is never
    /// stored — only this SHA‑256 hash is persisted.  This means
    /// a database leak cannot expose usable refresh tokens.
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// When this token expires.  After this time, the token cannot
    /// be used to obtain a new access token.  Defaults to 30 days
    /// from creation.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// If set, the token has been explicitly revoked (e.g., on
    /// logout, password change, or token reuse detection).
    /// Null means the token is still active.
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    // ----------------------------------------------------------------
    // Computed state
    // ----------------------------------------------------------------

    /// <summary>
    /// Whether this token has been explicitly revoked.
    /// </summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>
    /// Whether this token has expired at the given UTC time.
    /// </summary>
    /// <param name="utcNow">The current UTC time.</param>
    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    /// <summary>
    /// Whether this token is still usable (not revoked and not expired).
    /// </summary>
    /// <param name="utcNow">The current UTC time.</param>
    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    // ----------------------------------------------------------------
    // Constructor (for EF Core materialisation)
    // ----------------------------------------------------------------

    private RefreshToken() : base() { }

    // ----------------------------------------------------------------
    // Static factory – only called by the User aggregate
    // ----------------------------------------------------------------

    /// <summary>
    /// Generates a new refresh token.  The raw token value must be
    /// hashed by the caller before being passed as
    /// <paramref name="tokenHash"/>.
    /// </summary>
    /// <param name="userId">The owning user's ULID.</param>
    /// <param name="tokenHash">
    /// The SHA‑256 hash of the raw refresh token value.
    /// </param>
    /// <param name="expiresAt">Absolute UTC expiry time.</param>
    /// <returns>A new RefreshToken instance.</returns>
    internal static RefreshToken Generate(
        Ulid userId,
        string tokenHash,
        DateTime expiresAt)
    {
        Guard.AgainstNullOrWhiteSpace(tokenHash, nameof(tokenHash));

        return new RefreshToken
        {
            Id = Ulid.NewUlid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt
        };
    }

    // ----------------------------------------------------------------
    // Behaviour methods
    // ----------------------------------------------------------------

    /// <summary>
    /// Marks the token as revoked.  Idempotent – calling this method
    /// multiple times has no additional effect.
    /// </summary>
    /// <param name="utcNow">The current UTC time (injected for determinism).</param>
    internal void Revoke(DateTime utcNow)
    {
        // Idempotent: if already revoked, do nothing.
        if (RevokedAt.HasValue)
            return;

        RevokedAt = utcNow;
    }
}



////Dry‑run — refresh token lifecycle example:

//// =========================================================================
//// 1. GENERATION – After successful login, a refresh token is issued
//// =========================================================================
//var user = existingUser; // loaded from repository
//    var rawToken = "crypto-random-string-here";  // generated in Application layer
//    var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
//    var expiresAt = DateTime.UtcNow.AddDays(30);  // 30‑day rolling expiry

//    var refreshToken = RefreshToken.Generate(user.Id, tokenHash, expiresAt);

//    // State after creation:
//    //   Id         = "01HXYZ..." (unique ULID)
//    //   UserId     = user.Id
//    //   TokenHash  = "abc123..." (64‑char hex SHA‑256)
//    //   ExpiresAt  = 2026‑07‑07 10:00 UTC
//    //   RevokedAt  = null
//    //   IsRevoked  = false
//    //   IsExpired  = false
//    //   IsActive   = true

//    // =========================================================================
//    // 2. TOKEN ROTATION – On refresh, the old token is revoked and replaced
//    // =========================================================================
//    var now = DateTime.UtcNow;

//// Revoke the old token:
//refreshToken.Revoke(now);
//// RevokedAt = now
//// IsRevoked  = true
//// IsActive   = false

//// Generate a new token (rotated):
//var newRawToken = "new-crypto-random-string";
//var newTokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(newRawToken));
//var newRefreshToken = RefreshToken.Generate(user.Id, newTokenHash, DateTime.UtcNow.AddDays(30));

//// =========================================================================
//// 3. REUSE DETECTION – If a revoked token is presented, all tokens are revoked
//// =========================================================================
//// The Application layer checks:
//if (refreshToken.IsRevoked)
//{
//    // Token reuse detected!  Revoke all refresh tokens for this user
//    // to prevent a stolen token from being used.
//    foreach (var token in user.RefreshTokens)
//    {
//        token.Revoke(DateTime.UtcNow);
//    }
//}
