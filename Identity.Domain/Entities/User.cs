// ====================================================================
// VERIXORA – Identity.Domain / Entities / User.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   The User aggregate root.  Represents a registered user of the
//   VERIXORA platform.  Every user belongs to at least one Home
//   (tenant) and has authentication credentials, sessions, trusted
//   devices, and optionally API keys.
//
//   WHY AN AGGREGATE ROOT:
//     - The User is the single entry point for all identity‑related
//       operations: registration, login, session management, device
//       trust, and password changes.
//     - Child entities (Session, TrustedDevice, RefreshToken) are
//       only accessible through the User.  They have no independent
//       lifecycle.
//     - Repositories persist only the User; child entities are
//       persisted automatically as part of the User's aggregate
//       (EF Core cascade).
//
//   INVARIANTS (RULES ENFORCED BY THIS CLASS):
//     1. Email must be non‑empty, contain exactly one '@', and have
//        a dot in the domain part (e.g., "user@example.com").
//     2. Email uniqueness across the system is enforced at the
//        database level (unique index), not here.
//     3. Email must be verified before the user can access protected
//        resources (enforced in the Application layer).
//     4. Maximum 5 active sessions per user.
//     5. Maximum 5 trusted devices per user.
//     6. Maximum 5 active refresh tokens per user.
//     7. Password hash must never be exposed outside the aggregate.
//     8. Device fingerprints must be unique within the trusted
//        devices collection.
//
//   CHILD COLLECTIONS:
//     - Backed by `List<T>` rather than `HashSet<T>`.  This avoids
//       coupling to `Equals`/`GetHashCode` implementations on child
//       entities, which can break when EF Core proxy objects are
//       involved.
//     - All lookups use ID‑based comparison (`Any(s => s.Id == id)`)
//       for the same reason.
//     - Exposed as `IReadOnlyCollection<T>` via `.AsReadOnly()` to
//       prevent external code from adding or removing items.
//
//   DOMAIN EVENTS RAISED:
//     - UserRegistered          – a new user was created
//     - UserEmailVerified       – email verification completed
//     - UserSessionCreated      – a new session was added
//     - UserSessionRemoved      – a session was removed (logout)
//     - UserDeviceTrusted       – a device was added to trusted list
//     - UserRefreshTokenIssued  – a refresh token was generated
//     - UserRefreshTokenRevoked – a refresh token was revoked
//     - UserPasswordChanged     – the password was updated
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** inheriting from **AggregateRoot**:
//    - `AggregateRoot` (from SharedKernel) inherits from `Entity`,
//      which provides the ULID identity (`Id` property) and the
//      domain event infrastructure (`RaiseDomainEvent`,
//      `DomainEvents`, `DequeueDomainEvents`).
//
// 2. **private set** properties:
//    - External code can read the value but cannot modify it
//      directly.  All state changes go through named methods
//      (`VerifyEmail`, `ChangePassword`, etc.) that enforce
//      business rules.  This is the "Tell, Don't Ask" principle.
//
// 3. **List<T>** for child collections:
//    - A resizable array.  Simpler than `HashSet<T>` and does not
//      depend on correct equality implementations.
//    - `RemoveAll(predicate)` is used to remove expired tokens
//      in a single O(n) pass.
//
// 4. **IReadOnlyCollection<T>** with **AsReadOnly()**:
//    - Wraps the internal `List<T>` in a `ReadOnlyCollection<T>`.
//      Callers can enumerate, count, and use LINQ, but cannot cast
//      back to `List` to mutate the collection.
//
// 5. **private const int** for limits:
//    - Compile‑time constants.  Changing a limit requires a
//      deliberate code change and recompilation, which is
//      intentional for security‑sensitive boundaries.
//
// 6. **Factory method** (`Register`):
//    - The constructor is `private`.  The only way to create a new
//      `User` is through the static `Register` method, which
//      guarantees all invariants are satisfied at creation time.
//    - This pattern is called the "Named Constructor" or "Factory
//      Method" pattern.
//
// 7. **Behaviour methods** (`VerifyEmail`, `AddSession`, etc.):
//    - Each method represents a single business operation.
//    - They validate inputs with `Guard`, enforce invariants,
//      mutate state, and raise a domain event to notify the rest
//      of the system.
//
// 8. **Domain events**:
//    - Raised via `RaiseDomainEvent()` (inherited from `Entity`).
//    - Events are stored in a list inside the aggregate.  When the
//      `BaseDbContext` saves changes, it dequeues these events into
//      the outbox table for later asynchronous processing.
//
// 9. **Guard.AgainstNullOrWhiteSpace / Guard.AgainstNull**:
//    - SharedKernel utility methods that throw clear, consistent
//      exceptions when preconditions are violated.
//    - This keeps repetitive validation code out of every method.
//
// 10. **DateTime.UtcNow**:
//     - Always use UTC to avoid time‑zone ambiguity across
//       different servers, clients, and geographic regions.
//
// 11. **ID‑based lookups** (`_sessions.FirstOrDefault(s => s.Id == id)`):
//     - Uses the ULID identity to locate entities.  Avoids reliance
//       on reference equality or overridden `Equals`, which can
//       break when EF Core proxy classes are involved.
//
// 12. **List<T>.RemoveAll** with a predicate:
//     - Removes all items that match the given condition in a single
//       pass.  More efficient than iterating and calling `Remove`
//       individually, which would be O(n²).
// ====================================================================

using Identity.Domain.Events;

namespace Identity.Domain.Entities;

/// <summary>
/// Represents a registered user of the VERIXORA platform.
/// </summary>
public class User : AggregateRoot
{
    // ----------------------------------------------------------------
    // Aggregate size limits (invariants)
    // ----------------------------------------------------------------

    private const int MaxSessions = 5;
    private const int MaxTrustedDevices = 5;
    private const int MaxRefreshTokens = 5;

    // ----------------------------------------------------------------
    // Properties
    // ----------------------------------------------------------------

    /// <summary>
    /// The user's unique email address (normalised to lowercase).
    /// Used for login, communication, and identity verification.
    /// Uniqueness is enforced by a database unique index.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// The Argon2id hash of the user's password.  The raw password
    /// is never stored anywhere in the system.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the user has verified their email address.
    /// </summary>
    public bool EmailVerified { get; private set; }

    /// <summary>
    /// The user's optional phone number (for OTP challenges on
    /// unrecognised devices).
    /// </summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>
    /// UTC timestamp of when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    // ----------------------------------------------------------------
    // Child collections (backed by List, exposed as read‑only)
    // ----------------------------------------------------------------

    private readonly List<Session> _sessions = new();
    /// <summary>
    /// Active sessions for this user.  Maximum <see cref="MaxSessions"/>.
    /// </summary>
    public IReadOnlyCollection<Session> Sessions => _sessions.AsReadOnly();

    private readonly List<TrustedDevice> _trustedDevices = new();
    /// <summary>
    /// Devices explicitly trusted by this user.  Maximum
    /// <see cref="MaxTrustedDevices"/>.
    /// </summary>
    public IReadOnlyCollection<TrustedDevice> TrustedDevices => _trustedDevices.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = new();
    /// <summary>
    /// Active refresh tokens for this user.  Maximum
    /// <see cref="MaxRefreshTokens"/>.
    /// </summary>
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // ----------------------------------------------------------------
    // Constructors
    // ----------------------------------------------------------------

    /// <summary>
    /// Parameterless constructor for EF Core materialisation.
    /// EF Core calls this when loading a User from the database,
    /// then populates the properties via reflection or compiled
    /// setters.
    /// </summary>
    private User() : base() { }

    // ----------------------------------------------------------------
    // Factory method – the only public way to create a new User
    // ----------------------------------------------------------------

    /// <summary>
    /// Creates a new, unverified user with the given email and
    /// hashed password.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="passwordHash">An Argon2id hash of the password.</param>
    /// <param name="utcNow">
    /// The current UTC time (injected for determinism and testability).
    /// </param>
    /// <returns>A new User instance with EmailVerified = false.</returns>
    /// <exception cref="ArgumentException">
    /// If email or passwordHash is null, empty, or has an invalid
    /// format.
    /// </exception>
    public static User Register(string email, string passwordHash, DateTime utcNow)
    {
        // Step 1: Validate preconditions.  Fail fast if any input
        // is invalid.
        Guard.AgainstNullOrWhiteSpace(email, nameof(email));
        Guard.AgainstNullOrWhiteSpace(passwordHash, nameof(passwordHash));

        // Step 2: Normalise the email to lowercase and trim
        // whitespace.  This makes email lookup case‑insensitive.
        email = email.Trim().ToLowerInvariant();

        // Step 3: Validate the email format.  This is a basic check;
        // a full Email value object would be more robust.
        if (!IsValidEmail(email))
            throw new ArgumentException("Email format is invalid.", nameof(email));

        // Step 4: Create the user and set the initial state.
        var user = new User
        {
            Email = email,
            PasswordHash = passwordHash,
            EmailVerified = false,
            CreatedAt = utcNow
        };

        // Step 5: Notify the system that a new user has been created.
        // Handlers can send a welcome email, initialise a Home, etc.
        user.RaiseDomainEvent(new UserRegistered(user.Id, user.Email));

        return user;
    }

    // ----------------------------------------------------------------
    // Behaviour methods
    // ----------------------------------------------------------------

    /// <summary>
    /// Marks the user's email as verified.  Calling this method
    /// on an already‑verified user has no effect (idempotent).
    /// </summary>
    public void VerifyEmail()
    {
        if (EmailVerified) return;          // already verified – nothing to do

        EmailVerified = true;
        RaiseDomainEvent(new UserEmailVerified(Id));
    }

    /// <summary>
    /// Creates a new session for this user on the given device.
    /// </summary>
    /// <param name="deviceFingerprint">A hash identifying the device.</param>
    /// <param name="ipAddress">The client's IP address.</param>
    /// <param name="userAgent">The client's User‑Agent header.</param>
    /// <param name="utcNow">The current UTC time (injected for determinism).</param>
    /// <returns>The newly created Session.</returns>
    /// <exception cref="InvalidOperationException">
    /// If the maximum number of sessions (<see cref="MaxSessions"/>)
    /// has been reached.
    /// </exception>
    public Session AddSession(string deviceFingerprint, string ipAddress, string userAgent, DateTime utcNow)
    {
        Guard.AgainstNullOrWhiteSpace(deviceFingerprint, nameof(deviceFingerprint));
        Guard.AgainstNullOrWhiteSpace(ipAddress, nameof(ipAddress));

        if (_sessions.Count >= MaxSessions)
            throw new InvalidOperationException(
                $"Maximum sessions ({MaxSessions}) reached.");

        var session = new Session(Id, deviceFingerprint, ipAddress, userAgent, utcNow);
        _sessions.Add(session);
        RaiseDomainEvent(new UserSessionCreated(Id, session.Id));
        return session;
    }

    /// <summary>
    /// Removes a session (e.g., when the user logs out).
    /// </summary>
    /// <param name="session">The session to remove.</param>
    /// <exception cref="InvalidOperationException">
    /// If the session is null or does not belong to this user.
    /// </exception>
    public void RemoveSession(Session session)
    {
        // Null check first – fail fast.
        Guard.AgainstNull(session, nameof(session));

        // Locate the session by its unique ID.  Using ID‑based
        // lookup avoids EF‑Core proxy equality issues.
        var existing = _sessions.FirstOrDefault(s => s.Id == session.Id)
            ?? throw new InvalidOperationException("Session not found.");

        _sessions.Remove(existing);

        // Notify the system.
        RaiseDomainEvent(new UserSessionRemoved(Id, session.Id));
    }

    /// <summary>
    /// Adds a device to the user's trusted devices list.
    /// </summary>
    /// <param name="deviceFingerprint">A hash of the device's characteristics.</param>
    /// <param name="addedAt">The current UTC time (injected for determinism).</param>
    /// <returns>The newly created TrustedDevice.</returns>
    /// <exception cref="InvalidOperationException">
    /// If the limit is reached or the device is already trusted.
    /// </exception>
    public TrustedDevice TrustDevice(string deviceFingerprint, DateTime addedAt)
    {
        Guard.AgainstNullOrWhiteSpace(deviceFingerprint, nameof(deviceFingerprint));

        // Enforce the device limit.
        if (_trustedDevices.Count >= MaxTrustedDevices)
            throw new InvalidOperationException(
                $"Maximum trusted devices ({MaxTrustedDevices}) reached.");

        // Check for duplicate fingerprints (case‑sensitive).
        if (_trustedDevices.Any(d => d.DeviceFingerprint == deviceFingerprint))
            throw new InvalidOperationException("Device is already trusted.");

        var device = new TrustedDevice(Id, deviceFingerprint, addedAt);
        _trustedDevices.Add(device);

        // Notify the system.
        RaiseDomainEvent(new UserDeviceTrusted(Id, deviceFingerprint));

        return device;
    }

    /// <summary>
    /// Issues a new refresh token.  Any already‑expired tokens are
    /// cleaned up first to make room.
    /// </summary>
    /// <param name="tokenHash">
    /// The SHA‑256 hash of the raw refresh token value.
    /// </param>
    /// <param name="expiresAt">Absolute UTC expiry time.</param>
    /// <returns>The newly created RefreshToken.</returns>
    /// <exception cref="InvalidOperationException">
    /// If the refresh token limit is reached.
    /// </exception>
    public RefreshToken IssueRefreshToken(string tokenHash, DateTime expiresAt)
    {
        // Remove expired tokens first to free capacity.
        CleanupExpiredRefreshTokens();

        // Enforce the token limit.
        if (_refreshTokens.Count >= MaxRefreshTokens)
            throw new InvalidOperationException(
                $"Maximum refresh tokens ({MaxRefreshTokens}) reached.");

        var token = RefreshToken.Generate(Id, tokenHash, expiresAt);
        _refreshTokens.Add(token);

        // Notify the system.
        RaiseDomainEvent(new UserRefreshTokenIssued(Id, token.Id));

        return token;
    }

    /// <summary>
    /// Revokes a specific refresh token and removes it from the
    /// active collection.
    /// </summary>
    /// <param name="token">The token to revoke.</param>
    /// <exception cref="InvalidOperationException">
    /// If the token is null or does not belong to this user.
    /// </exception>
    public void RevokeRefreshToken(RefreshToken token)
    {
        Guard.AgainstNull(token, nameof(token));

        var existing = _refreshTokens.FirstOrDefault(t => t.Id == token.Id)
            ?? throw new InvalidOperationException("Refresh token not found.");

        _refreshTokens.Remove(existing);

        // Notify the system.
        RaiseDomainEvent(new UserRefreshTokenRevoked(Id, token.Id));
    }

    /// <summary>
    /// Changes the user's password to a new Argon2id hash.
    /// </summary>
    /// <param name="newPasswordHash">The new hashed password.</param>
    public void ChangePassword(string newPasswordHash)
    {
        Guard.AgainstNullOrWhiteSpace(newPasswordHash, nameof(newPasswordHash));
        PasswordHash = newPasswordHash;

        // Notify the system.
        RaiseDomainEvent(new UserPasswordChanged(Id));
    }

    // ----------------------------------------------------------------
    // Private helpers
    // ----------------------------------------------------------------

    /// <summary>
    /// Removes all refresh tokens whose expiry time has passed.
    /// Called automatically before issuing a new token to free
    /// capacity.
    /// </summary>
    private void CleanupExpiredRefreshTokens()
    {
        // RemoveAll is O(n) and avoids the O(n²) cost of iterating
        // and calling Remove individually.
        _refreshTokens.RemoveAll(t => t.ExpiresAt < DateTime.UtcNow);
    }

    /// <summary>
    /// Validates that the email contains exactly one '@' with
    /// non‑empty local and domain parts, and that the domain
    /// contains at least one dot.
    /// </summary>
    /// <param name="email">The email address to validate (already trimmed and lowercased).</param>
    /// <returns><c>true</c> if the email format is acceptable; otherwise <c>false</c>.</returns>
    private static bool IsValidEmail(string email)
    {
        // Find the single '@' character.
        int atIndex = email.IndexOf('@');

        // '@' must exist, must not be the first character, must not
        // be the last character, and there must be only one.
        if (atIndex <= 0 || atIndex != email.LastIndexOf('@') || atIndex == email.Length - 1)
            return false;

        // The domain part (after the '@') must contain at least one
        // dot (e.g., "example.com").
        return email.LastIndexOf('.') > atIndex;
    }
}
