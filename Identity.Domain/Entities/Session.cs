// ====================================================================
// VERIXORA – Identity.Domain / Entities / Session.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   A child entity within the User aggregate.  Represents a single
//   authenticated session for a user, tied to a specific device
//   fingerprint, IP address, and User‑Agent string.
//
//   WHY A CHILD ENTITY:
//     - Sessions have no independent lifecycle outside a User.
//       They are always loaded, modified, and persisted through the
//       User aggregate root.
//     - The User enforces session limits (max 5) and lifecycle rules
//       (creation, removal, expiry, trust).
//
//   DESIGN PHILOSOPHY – "SECURITY‑FIRST SESSION MODEL":
//     This entity is deliberately designed as a **hardened security
//     boundary**, not a flexible temporal entity.  Key decisions:
//       - Expired sessions can NEVER be refreshed.  A new session
//         must be created via the refresh‑token flow.
//       - Revoked sessions are permanently dead (idempotent).
//       - Trust is only granted to active, non‑expired sessions.
//     This eliminates session resurrection, sliding‑expiration
//     abuse, and token‑replay attacks.
//
//   DETERMINISM:
//     All methods that depend on the current time accept a
//     <c>DateTime utcNow</c> parameter.  The domain entity itself
//     never calls <c>DateTime.UtcNow</c> — this keeps the domain
//     pure, testable, and replay‑safe.
//
//   IDENTITY:
//     - Each Session has a unique ULID (Id) generated at creation.
//     - Sessions are compared by ID only, inherited from the Entity
//       base class (ID + Type equality).
//
//   INVARIANTS:
//     - DeviceFingerprint and IpAddress are required.
//     - UserAgent defaults to "unknown" if not provided.
//     - ExpiresAt is set at creation (15 minutes by default).
//     - MarkTrusted() is only allowed on active, non‑revoked sessions.
//     - Refresh() is only allowed on active, non‑expired sessions
//       and enforces a hard lifetime cap from CreatedAt.
//     - Revoke() is idempotent – safe to call multiple times.
//     - IsTrusted is mutated only via the User aggregate.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** inheriting from **Entity** (not AggregateRoot):
//    - `Entity` provides the ULID `Id`, domain event support, and
//      structural equality (ID + Type).  Sessions are children of
//      User, not independent roots.
//
// 2. **private set** / **internal** properties:
//    - `private set` means external code can read the value but
//      cannot modify it.  Only methods within this class can change
//      the property.
//    - `internal` means the member is accessible from anywhere within
//      the same compiled assembly (Identity.Domain.dll) but not from
//      outside.  This allows the User aggregate to call methods on
//      Session without exposing them to the Application layer.
//
// 3. **Computed methods** (`IsExpired`, `IsActive`, `IsRevoked`):
//    - These have no backing fields.  Their values are calculated
//      every time they are called.  This means they always reflect
//      the current state without needing to call an update method.
//
// 4. **Deterministic time injection** (`DateTime utcNow` parameter):
//    - Instead of calling `DateTime.UtcNow` inside the entity (which
//      would be a hidden, non‑testable dependency), the current time
//      is passed in by the caller.  This makes unit tests
//      deterministic and allows replay‑based debugging.
//
// 5. **Idempotent methods** (`Revoke`):
//    - Calling the method twice has the same effect as calling it
//      once.  This prevents bugs when a method is accidentally called
//      multiple times (e.g., in retry scenarios).
//
// 6. **Guard.AgainstNullOrWhiteSpace**:
//    - A SharedKernel utility that throws a clear, consistent
//      exception if the argument is null, empty, or whitespace.
//      This enforces preconditions at the start of a method.
//
// 7. **Ulid.NewUlid()**:
//    - Generates a new unique, time‑sortable identifier.  Better for
//      database clustered indexes than random GUIDs.
//
// 8. **TimeSpan** for durations:
//    - A struct representing a length of time (e.g., 15 minutes).
//      Using `TimeSpan` instead of raw integers makes the code
//      self‑documenting and type‑safe.
//
// 9. **Nullable DateTime** (`DateTime?`):
//    - `DateTime? RevokedAt` can either hold a DateTime value or
//      be null.  Null means "not revoked".  This is cleaner than
//      using a separate boolean flag.
// ====================================================================

using System.Numerics;

namespace Identity.Domain.Entities;

/// <summary>
/// Represents an authenticated session for a user on a specific device.
/// Designed as a hardened security boundary with a strict lifecycle.
/// </summary>
public class Session : Entity
{
    // ----------------------------------------------------------------
    // Properties
    // ----------------------------------------------------------------

    /// <summary>
    /// The ID of the user who owns this session.
    /// This is a foreign key linking the session back to its parent
    /// User aggregate.
    /// </summary>
    public Ulid UserId { get; private set; } = default!;

    /// <summary>
    /// A hash of the device's characteristics (OS type, OS version,
    /// browser, screen resolution, installed fonts, etc.).
    /// Used to detect device changes mid‑session.  If the fingerprint
    /// changes dramatically, the session is invalidated.
    /// </summary>
    public string DeviceFingerprint { get; private set; } = string.Empty;

    /// <summary>
    /// The IP address from which the session was created.
    /// Used for security auditing and suspicious activity detection
    /// (e.g., sudden IP change).
    /// </summary>
    public string IpAddress { get; private set; } = string.Empty;

    /// <summary>
    /// The User‑Agent header sent by the client's browser or app.
    /// Provides additional context for device identification.
    /// Defaults to "unknown" if the client sends an empty or null
    /// value.  This prevents attackers from spoofing minimal UA
    /// strings to reduce detection accuracy.
    /// </summary>
    public string UserAgent { get; private set; } = "unknown";

    /// <summary>
    /// Whether this session has been explicitly trusted by the user
    /// (the "Remember this device" option during login).
    /// Trusted devices skip OTP challenges on subsequent logins.
    /// This property is mutated only by the User aggregate via the
    /// <c>MarkTrusted()</c> method to enforce the maximum trusted
    /// devices limit (5) and to raise the appropriate domain event.
    /// </summary>
    public bool IsTrusted { get; private set; }

    /// <summary>
    /// When the session was created.  Always UTC.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the session expires.  Defaults to 15 minutes after
    /// creation, matching the JWT access token lifetime.
    /// After this time, the session is considered invalid and
    /// should not be used for authentication.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// If set, the session has been forcibly revoked (e.g., admin
    /// logout, security breach, password change).  Null means the
    /// session has not been revoked.
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    // ----------------------------------------------------------------
    // Computed state (deterministic – time is injected)
    // ----------------------------------------------------------------

    /// <summary>
    /// Whether the session has expired at the given UTC time.
    /// The time parameter is injected to keep the domain
    /// deterministic and testable.
    /// </summary>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns><c>true</c> if the session's ExpiresAt is in the past.</returns>
    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    /// <summary>
    /// Whether the session has been revoked.
    /// This is a computed property based on whether RevokedAt has a value.
    /// </summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>
    /// Whether the session is currently active – not revoked and
    /// not expired at the given UTC time.  This is the primary
    /// validity check used by authentication middleware.
    /// </summary>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns><c>true</c> if the session can still be used.</returns>
    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    // ----------------------------------------------------------------
    // Constructor (for EF Core materialisation)
    // ----------------------------------------------------------------

    /// <summary>
    /// Parameterless constructor required by EF Core.
    /// EF Core uses this to create an empty instance, then populates
    /// properties from the database row.
    /// </summary>
    private Session() : base() { }

    // ----------------------------------------------------------------
    // Internal constructor – only the User aggregate creates sessions
    // ----------------------------------------------------------------

    /// <summary>
    /// Creates a new session.  Called only by the User aggregate root
    /// via <c>User.AddSession()</c>.  All time values are injected
    /// for determinism and testability.
    /// </summary>
    /// <param name="userId">The owning user's ULID.</param>
    /// <param name="deviceFingerprint">
    /// A hash of the device's characteristics (OS, browser, screen
    /// resolution, etc.).
    /// </param>
    /// <param name="ipAddress">The client's IP address.</param>
    /// <param name="userAgent">
    /// The client's User‑Agent header.  Defaults to "unknown" if empty.
    /// </param>
    /// <param name="utcNow">The current UTC time (injected for determinism).</param>
    internal Session(
        Ulid userId,
        string deviceFingerprint,
        string ipAddress,
        string userAgent,
        DateTime utcNow)
    {
        // Required fields – fail fast if null or whitespace.
        Guard.AgainstNullOrWhiteSpace(deviceFingerprint, nameof(deviceFingerprint));
        Guard.AgainstNullOrWhiteSpace(ipAddress, nameof(ipAddress));

        // Generate a new unique, time‑sortable ID.
        Id = Ulid.NewUlid();

        UserId = userId;
        DeviceFingerprint = deviceFingerprint;
        IpAddress = ipAddress;
        UserAgent = string.IsNullOrWhiteSpace(userAgent) ? "unknown" : userAgent;
        CreatedAt = utcNow;
        ExpiresAt = utcNow.AddMinutes(15);   // JWT access token lifetime (ADR)
        IsTrusted = false;
    }

    // ----------------------------------------------------------------
    // Behaviour methods
    // ----------------------------------------------------------------

    /// <summary>
    /// Marks the session as trusted.  Only allowed on active,
    /// non‑revoked sessions.  The <paramref name="utcNow"/> parameter
    /// is used only for validation; it does not affect the entity's
    /// state beyond determining whether the session is expired.
    /// </summary>
    /// <param name="utcNow">The current UTC time (injected for determinism).</param>
    /// <exception cref="InvalidOperationException">
    /// If the session is expired or revoked.
    /// </exception>
    internal void MarkTrusted(DateTime utcNow)
    {
        // Guard: cannot trust a session that has already been killed.
        if (IsRevoked)
            throw new InvalidOperationException("Cannot trust a revoked session.");

        // Guard: cannot trust a session that has already expired.
        if (IsExpired(utcNow))
            throw new InvalidOperationException("Cannot trust an expired session.");

        IsTrusted = true;
    }

    /// <summary>
    /// Refreshes the session expiry within a bounded sliding window.
    /// The new expiry cannot exceed <c>CreatedAt + maxLifetime</c>.
    /// Cannot be called on a revoked or expired session – an expired
    /// session must be replaced entirely via the refresh‑token flow.
    /// </summary>
    /// <param name="utcNow">The current UTC time (injected).</param>
    /// <param name="slidingWindow">
    /// How long to extend from now (e.g., 15 minutes).
    /// </param>
    /// <param name="maxLifetime">
    /// Absolute maximum lifetime from creation (e.g., 30 minutes).
    /// This is the hard cap – the session can never live longer than
    /// <c>CreatedAt + maxLifetime</c>, regardless of how many times
    /// Refresh is called.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// If the session is revoked, expired, or the hard lifetime cap
    /// would be exceeded.
    /// </exception>
    internal void Refresh(DateTime utcNow, TimeSpan slidingWindow, TimeSpan maxLifetime)
    {
        // Guard: cannot refresh a session that has been forcibly killed.
        if (IsRevoked)
            throw new InvalidOperationException("Cannot refresh a revoked session.");

        // Guard: cannot resurrect an expired session.  A new session
        // must be created via the refresh‑token flow.
        if (IsExpired(utcNow))
            throw new InvalidOperationException(
                "Cannot refresh an expired session.  " +
                "A new session must be created via the refresh token flow.");

        // Calculate the proposed new expiry time.
        var newExpiry = utcNow.Add(slidingWindow);

        // Calculate the absolute maximum expiry time allowed.
        var hardCap = CreatedAt.Add(maxLifetime);

        // If the proposed expiry exceeds the hard cap, reject it.
        if (newExpiry > hardCap)
            throw new InvalidOperationException(
                "Session lifetime exceeded maximum allowed window.  " +
                "A new session must be created via the refresh token flow.");

        // Apply the new expiry.
        ExpiresAt = newExpiry;
    }

    /// <summary>
    /// Revokes the session forcibly (e.g., admin logout, security
    /// breach, password change).  Idempotent – calling this method
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




////Dry‑run — complete session lifecycle example:
//// =========================================================================
//// 1. CREATION – User logs in at 10:00 UTC
//// =========================================================================
//var session = new Session(
//    userId,                                     // ULID of the user
//    "fp-laptop-chrome-abc123",                  // device fingerprint
//    "203.0.113.42",                              // IP address
//    "Mozilla/5.0 Chrome/120",                    // User‑Agent
//    new DateTime(2026, 6, 7, 10, 0, 0, DateTimeKind.Utc));

//    // State after creation:
//    //   Id              = "01HXYZ..." (unique ULID)
//    //   UserId          = user.Id
//    //   DeviceFingerprint = "fp-laptop-chrome-abc123"
//    //   IpAddress       = "203.0.113.42"
//    //   UserAgent       = "Mozilla/5.0 Chrome/120"
//    //   IsTrusted       = false
//    //   CreatedAt       = 10:00 UTC
//    //   ExpiresAt       = 10:15 UTC
//    //   RevokedAt       = null
//    //   IsExpired(10:00) = false
//    //   IsActive(10:00)  = true

//    // =========================================================================
//    // 2. TRUST – User clicks "Remember this device" at 10:02 UTC
//    // =========================================================================
//    var now1 = new DateTime(2026, 6, 7, 10, 2, 0, DateTimeKind.Utc);
//session.MarkTrusted(now1);

//// IsTrusted = true

//// =========================================================================
//// 3. REFRESH – User is active, session is extended at 10:10 UTC
//// =========================================================================
//var now2 = new DateTime(2026, 6, 7, 10, 10, 0, DateTimeKind.Utc);
//session.Refresh(
//    now2,
//    TimeSpan.FromMinutes(15),      // sliding window
//    TimeSpan.FromMinutes(30));     // hard cap from creation

//// newExpiry = 10:10 + 15 = 10:25
//// hardCap   = 10:00 + 30 = 10:30
//// 10:25 < 10:30 → allowed
//// ExpiresAt = 10:25 UTC

//// =========================================================================
//// 4. REVOKE – Admin forces logout at 10:12 UTC
//// =========================================================================
//var now3 = new DateTime(2026, 6, 7, 10, 12, 0, DateTimeKind.Utc);
//session.Revoke(now3);

//// RevokedAt = 10:12 UTC
//// IsRevoked  = true
//// IsActive   = false
//// Calling Revoke() again has no effect (idempotent).

//// =========================================================================
//// 5. EXPIRED – Session times out at 10:30 UTC
//// =========================================================================
//var now4 = new DateTime(2026, 6, 7, 10, 30, 0, DateTimeKind.Utc);
//// IsExpired(now4) = true (10:30 >= 10:25)
//// Refresh() would throw – expired sessions cannot be refreshed.
//// A new session must be created via the refresh‑token flow.
