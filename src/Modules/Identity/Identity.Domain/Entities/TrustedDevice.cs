// ====================================================================
// VERIXORA – Identity.Domain / Entities / TrustedDevice.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   A child entity within the User aggregate.  Represents a device
//   that the user has explicitly marked as trusted (the "Remember
//   this device" option during login).  Trusted devices skip OTP
//   challenges on subsequent logins.
//
//   WHY A CHILD ENTITY:
//     - TrustedDevice has no independent lifecycle outside a User.
//     - It is always loaded, modified, and persisted through the
//       User aggregate root.
//     - The User enforces the maximum trusted devices limit (5) and
//       uniqueness by fingerprint.
//
//   IDENTITY:
//     - Each TrustedDevice has a unique ULID (Id) generated at
//       creation.
//     - TrustedDevices are compared by ID only, inherited from the
//       Entity base class (ID + Type equality).
//
//   INVARIANTS:
//     - DeviceFingerprint is required and must be unique within a
//       user's trusted devices collection.
//     - AddedAt is set at creation time (injected for determinism).
//
//   SIMPLICITY NOTE:
//     This entity is deliberately minimal.  It has no behaviour
//     methods of its own – the User aggregate handles all logic
//     for adding, enforcing limits, and checking uniqueness.
//     The entity exists primarily to give structure to the
//     persisted data and to enable EF Core mapping.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** inheriting from **Entity** (not AggregateRoot):
//    - TrustedDevice is a child entity, not an independent root.
//    - `Entity` provides the ULID `Id`, domain event support, and
//      structural equality (ID + Type).
//
// 2. **private set** properties:
//    - External code can read values but cannot modify them.
//      This enforces encapsulation.
//
// 3. **internal constructor**:
//    - Only the Identity.Domain assembly can create a TrustedDevice.
//      In practice, only the User aggregate calls this constructor
//      via the `TrustDevice()` method.
//
// 4. **DateTime injection**:
//    - `AddedAt` is set via a constructor parameter rather than
//      calling `DateTime.UtcNow` directly.  This keeps the domain
//      deterministic and testable.
//
// 5. **Ulid.NewUlid()**:
//    - Generates a new unique, time‑sortable identifier at creation.
//
// 6. **Guard.AgainstNullOrWhiteSpace**:
//    - SharedKernel validation helper.  Throws immediately if the
//      argument is null, empty, or whitespace.
// ====================================================================

namespace Identity.Domain.Entities;

/// <summary>
/// Represents a device explicitly trusted by the user.
/// </summary>
public class TrustedDevice : Entity
{
    // ----------------------------------------------------------------
    // Properties
    // ----------------------------------------------------------------

    /// <summary>
    /// The ID of the user who owns this trusted device.
    /// </summary>
    public Ulid UserId { get; private set; } = default!;

    /// <summary>
    /// A hash of the device's characteristics (OS type, OS version,
    /// browser, screen resolution, installed fonts, etc.).
    /// Must be unique within a user's trusted devices collection.
    /// </summary>
    public string DeviceFingerprint { get; private set; } = string.Empty;

    /// <summary>
    /// When the device was added to the trusted list.
    /// Injected for determinism and testability.
    /// </summary>
    public DateTime AddedAt { get; private set; }

    // ----------------------------------------------------------------
    // Constructor (for EF Core materialisation)
    // ----------------------------------------------------------------

    /// <summary>
    /// Parameterless constructor required by EF Core.
    /// EF Core uses this to create an empty instance, then populates
    /// properties from the database row.
    /// </summary>
    private TrustedDevice() : base() { }

    // ----------------------------------------------------------------
    // Internal constructor – only the User aggregate creates these
    // ----------------------------------------------------------------

    /// <summary>
    /// Creates a new trusted device.  Called only by the User
    /// aggregate root via <c>User.TrustDevice()</c>.
    /// </summary>
    /// <param name="userId">The owning user's ULID.</param>
    /// <param name="deviceFingerprint">
    /// A hash of the device's characteristics.
    /// </param>
    /// <param name="addedAt">
    /// The UTC time when the device was trusted (injected for
    /// determinism and testability).
    /// </param>
    internal TrustedDevice(
        Ulid userId,
        string deviceFingerprint,
        DateTime addedAt)
    {
        // The fingerprint is the only required field.
        Guard.AgainstNullOrWhiteSpace(deviceFingerprint, nameof(deviceFingerprint));

        // Generate a new unique, time‑sortable ID.
        Id = Ulid.NewUlid();

        UserId = userId;
        DeviceFingerprint = deviceFingerprint;
        AddedAt = addedAt;
    }
}

//Dry‑run — trusted device lifecycle example:

//// =========================================================================
//// 1. USER TRUSTS THEIR PHONE
//// =========================================================================
//var user = existingUser; // loaded from repository
//var now = new DateTime(2026, 6, 7, 10, 5, 0, DateTimeKind.Utc);

//// The User aggregate enforces:
////   - Max 5 trusted devices
////   - No duplicate fingerprints
////   - Raises UserDeviceTrusted domain event
//var trustedPhone = user.TrustDevice("fp-phone-safari-xyz789", now);

//// State after creation:
////   Id               = "01HXYZ..." (unique ULID)
////   UserId           = user.Id
////   DeviceFingerprint = "fp-phone-safari-xyz789"
////   AddedAt          = 2026‑06‑07 10:05 UTC

//// =========================================================================
//// 2. ON NEXT LOGIN – The same fingerprint is recognised
//// =========================================================================
//// The Application layer compares the incoming device fingerprint
//// against the user's TrustedDevices collection:
//bool isTrusted = user.TrustedDevices.Any(d =>
//    d.DeviceFingerprint == incomingFingerprint);

//// If isTrusted is true → OTP challenge is skipped.
//// If isTrusted is false → OTP is required, and the user may choose
//// to trust the device after successful verification.
