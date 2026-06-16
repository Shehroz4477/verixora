// ====================================================================
// VERIXORA – Identity.Domain / Entities / Home.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   The Home aggregate root.  Represents a tenant (a home, a rental
//   property, or a small business) in the VERIXORA multi‑tenant
//   system.  Every user belongs to one or more Homes, and every
//   device belongs to exactly one Home.
//
//   WHY AN AGGREGATE ROOT:
//     - Home is the entry point for managing memberships and roles.
//     - Repositories persist only the Home; memberships are
//       persisted as part of the Home's aggregate.
//     - The Home enforces member limits, role assignments, and
//       device capacity.
//
//   WHY NOT PART OF USER AGGREGATE:
//     - A Home exists independently of any single user.  Owners
//       can be removed, but the Home persists.
//     - Homes are the primary isolation boundary for all
//       authorisation, audit logs, and events (per Master Spec).
//
//   INVARIANTS:
//     - Name is required and must not be empty.
//     - MaxDevices is configurable per Home (default 20).
//     - A Home must have at least one Owner.
//     - The same user cannot have duplicate memberships in the
//       same Home.
//     - Memberships are immutable once created; only the Role
//       can change.
//
//   DOMAIN EVENTS RAISED:
//     - HomeCreated  – when a new Home is created
//     - MemberAdded  – when a user is added to the Home
//     - MemberRemoved – when a user is removed from the Home
//     - RoleChanged  – when a member's role is updated
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** inheriting from **AggregateRoot**:
//    - `AggregateRoot` provides the ULID identity (`Id`) and the
//      domain event infrastructure.
//
// 2. **private set** properties – encapsulation.
//
// 3. **List<T>** for child collections, exposed as `IReadOnlyCollection<T>`.
//
// 4. **Factory method** (`Create`) – the only public way to create a Home.
//
// 5. **Behaviour methods** – encapsulate business rules, validate
//    input, enforce invariants, and raise domain events.
//
// 6. **Enum** (`HomeRole`) – type‑safe role definitions using the
//    SharedKernel Enumeration pattern.
//
// 7. **Guard** – SharedKernel validation helpers.
//
// 8. **DateTime injection** – keeps the domain deterministic and testable.
// ====================================================================

using Identity.Domain.Enums;
using Identity.Domain.Events;

namespace Identity.Domain.Entities;

/// <summary>
/// Represents a tenant (home, rental, or small business) in the
/// VERIXORA multi‑tenant system.
/// </summary>
public class Home : AggregateRoot
{
    // ----------------------------------------------------------------
    // Properties
    // ----------------------------------------------------------------

    /// <summary>
    /// The display name of the home.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The maximum number of devices allowed in this home.
    /// Configurable per home, defaults to 20 per Master Spec.
    /// </summary>
    public int MaxDevices { get; private set; } = 20;

    /// <summary>
    /// When the home was created.  Injected for determinism.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The members (users) who belong to this home, along with
    /// their roles.
    /// </summary>
    private readonly List<HomeMembership> _members = new();
    public IReadOnlyCollection<HomeMembership> Members => _members.AsReadOnly();

    // ----------------------------------------------------------------
    // Constructor (for EF Core materialisation)
    // ----------------------------------------------------------------

    private Home() : base() { }

    // ----------------------------------------------------------------
    // Factory method
    // ----------------------------------------------------------------

    /// <summary>
    /// Creates a new Home with the given name and founding owner.
    /// The owner is automatically added as a member with the
    /// <see cref="HomeRole.Owner"/> role.
    /// </summary>
    /// <param name="name">The display name of the home.</param>
    /// <param name="ownerId">The ULID of the founding owner.</param>
    /// <param name="createdAt">The current UTC time (injected).</param>
    /// <returns>A new Home instance.</returns>
    public static Home Create(string name, Ulid ownerId, DateTime createdAt)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        var home = new Home
        {
            Name = name.Trim(),
            MaxDevices = 20,             // default per Master Spec
            CreatedAt = createdAt
        };

        // The founding owner is automatically added as a member.
        home.AddMemberInternal(ownerId, HomeRole.Owner, createdAt);

        // Notify the system.
        home.RaiseDomainEvent(new HomeCreated(home.Id, home.Name, ownerId));

        return home;
    }

    // ----------------------------------------------------------------
    // Behaviour methods
    // ----------------------------------------------------------------

    /// <summary>
    /// Adds a user to this home with the specified role.
    /// </summary>
    /// <param name="userId">The ULID of the user to add.</param>
    /// <param name="role">The role to assign.</param>
    /// <param name="addedAt">The current UTC time (injected).</param>
    /// <exception cref="InvalidOperationException">
    /// If the user is already a member of this home.
    /// </exception>
    public void AddMember(Ulid userId, HomeRole role, DateTime addedAt)
    {
        // Prevent duplicate membership.
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this home.");

        AddMemberInternal(userId, role, addedAt);

        // Notify the system.
        RaiseDomainEvent(new MemberAdded(Id, userId, role));
    }

    /// <summary>
    /// Removes a user from this home.
    /// </summary>
    /// <param name="userId">The ULID of the user to remove.</param>
    /// <exception cref="InvalidOperationException">
    /// If the user is not a member of this home.
    /// </exception>
    public void RemoveMember(Ulid userId)
    {
        var membership = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new InvalidOperationException("User is not a member of this home.");

        _members.Remove(membership);

        // Notify the system.
        RaiseDomainEvent(new MemberRemoved(Id, userId));
    }

    /// <summary>
    /// Changes the role of an existing member.
    /// </summary>
    /// <param name="userId">The ULID of the member.</param>
    /// <param name="newRole">The new role to assign.</param>
    /// <exception cref="InvalidOperationException">
    /// If the user is not a member of this home.
    /// </exception>
    public void ChangeRole(Ulid userId, HomeRole newRole)
    {
        var membership = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new InvalidOperationException("User is not a member of this home.");

        membership.ChangeRole(newRole);

        // Notify the system.
        RaiseDomainEvent(new RoleChanged(Id, userId, newRole));
    }

    /// <summary>
    /// Updates the maximum number of devices allowed in this home.
    /// </summary>
    /// <param name="maxDevices">The new maximum (must be positive).</param>
    public void UpdateMaxDevices(int maxDevices)
    {
        Guard.AgainstNegativeOrZero(maxDevices, nameof(maxDevices));
        MaxDevices = maxDevices;
    }

    // ----------------------------------------------------------------
    // Private helpers
    // ----------------------------------------------------------------

    /// <summary>
    /// Adds a member without raising a domain event.
    /// Used internally during Home creation to avoid duplicate events.
    /// </summary>
    private void AddMemberInternal(Ulid userId, HomeRole role, DateTime addedAt)
    {
        var membership = new HomeMembership(Id, userId, role, addedAt);
        _members.Add(membership);
    }
}


////Dry‑run — home lifecycle example:
//// =========================================================================
//// 1. CREATE A HOME
//// =========================================================================
//var now = DateTime.UtcNow;
//    var home = Home.Create("My Smart Home", ownerUserId, now);

//// State after creation:
////   Id         = "01HXYZ..." (unique ULID)
////   Name       = "My Smart Home"
////   MaxDevices = 20
////   CreatedAt  = now
////   Members    = [{ UserId: ownerUserId, Role: Owner }]
////   Domain event: HomeCreated is raised

//    // =========================================================================
//    // 2. ADD A MEMBER (Guest)
//    // =========================================================================
//home.AddMember(guestUserId, HomeRole.Guest, DateTime.UtcNow);

//// Members.Count = 2
//// Domain event: MemberAdded is raised

//// =========================================================================
//// 3. CHANGE A MEMBER'S ROLE (Guest → Admin)
//// =========================================================================
//home.ChangeRole(guestUserId, HomeRole.Admin);

//// The guest is now an Admin.
//// Domain event: RoleChanged is raised

//// =========================================================================
//// 4. REMOVE A MEMBER
//// =========================================================================
//home.RemoveMember(guestUserId);

//// Members.Count = 1
//// Domain event: MemberRemoved is raised

//// =========================================================================
//// 5. UPDATE DEVICE CAPACITY
//// =========================================================================
//home.UpdateMaxDevices(50);

//// MaxDevices = 50
