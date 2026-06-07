// ====================================================================
// VERIXORA – Identity.Domain / Entities / HomeMembership.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   A child entity within the Home aggregate.  Represents the
//   relationship between a User and a Home, including the role
//   that the user has within that Home.
//
//   WHY A CHILD ENTITY:
//     - HomeMembership has no independent lifecycle outside a Home.
//     - It is always loaded, modified, and persisted through the
//       Home aggregate root.
//     - The Home enforces duplicate membership checks and role
//       change rules.
//
//   IDENTITY:
//     - Each HomeMembership has a unique ULID (Id) generated at
//       creation.
//     - HomeMemberships are compared by ID only, inherited from
//       the Entity base class (ID + Type equality).
//
//   INVARIANTS:
//     - HomeId and UserId together uniquely identify a membership
//       (enforced by a database unique index, not here).
//     - Role is required and must be a valid HomeRole.
//     - JoinedAt is set at creation time (injected for determinism).
//
//   ROLE CHANGE:
//     - The role can be changed via ChangeRole().
//     - The Home aggregate root controls when role changes are
//       allowed and raises the RoleChanged domain event.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** inheriting from **Entity** (not AggregateRoot):
//    - HomeMembership is a child entity, not an independent root.
//
// 2. **private set** properties – encapsulation.
//
// 3. **internal constructor**:
//    - Only the Identity.Domain assembly can create a
//      HomeMembership.  In practice, only the Home aggregate
//      calls this constructor.
//
// 4. **HomeRole** (Enumeration):
//    - A type‑safe enumeration class.  Using it instead of a
//      primitive integer makes the code self‑documenting and
//      prevents invalid role assignments.
//
// 5. **DateTime injection**:
//    - `JoinedAt` is passed as a constructor parameter rather
//      than calculated inside the entity.  This keeps the domain
//      deterministic and testable.
//
// 6. **Ulid.NewUlid()** – generates a unique, time‑sortable ID.
//
// 7. **Guard** – SharedKernel validation helpers.
// ====================================================================

using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

/// <summary>
/// Represents a user's membership in a Home with a specific role.
/// </summary>
public class HomeMembership : Entity
{
    // ----------------------------------------------------------------
    // Properties
    // ----------------------------------------------------------------

    /// <summary>
    /// The ID of the Home this membership belongs to.
    /// </summary>
    public Ulid HomeId { get; private set; } = default!;

    /// <summary>
    /// The ID of the User who is a member of the Home.
    /// </summary>
    public Ulid UserId { get; private set; } = default!;

    /// <summary>
    /// The role this user has within the Home (Owner, Admin,
    /// Member, or Guest).
    /// </summary>
    public HomeRole Role { get; private set; } = default!;

    /// <summary>
    /// When the user joined this Home.  Injected for determinism
    /// and testability.
    /// </summary>
    public DateTime JoinedAt { get; private set; }

    // ----------------------------------------------------------------
    // Constructor (for EF Core materialisation)
    // ----------------------------------------------------------------

    /// <summary>
    /// Parameterless constructor required by EF Core.
    /// </summary>
    private HomeMembership() : base() { }

    // ----------------------------------------------------------------
    // Internal constructor – only the Home aggregate creates these
    // ----------------------------------------------------------------

    /// <summary>
    /// Creates a new membership.  Called only by the Home aggregate
    /// root via <c>Home.AddMember()</c> or <c>AddMemberInternal()</c>.
    /// </summary>
    /// <param name="homeId">The Home's ULID.</param>
    /// <param name="userId">The User's ULID.</param>
    /// <param name="role">The initial role.</param>
    /// <param name="joinedAt">
    /// The UTC time when the user joined (injected for determinism).
    /// </param>
    internal HomeMembership(
        Ulid homeId,
        Ulid userId,
        HomeRole role,
        DateTime joinedAt)
    {
        Guard.AgainstNull(role, nameof(role));

        Id = Ulid.NewUlid();
        HomeId = homeId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
    }

    // ----------------------------------------------------------------
    // Behaviour methods
    // ----------------------------------------------------------------

    /// <summary>
    /// Changes the member's role.
    /// Called by the Home aggregate root, which enforces business
    /// rules (e.g., who can change roles) and raises the
    /// RoleChanged domain event.
    /// </summary>
    /// <param name="newRole">The new role to assign.</param>
    internal void ChangeRole(HomeRole newRole)
    {
        Guard.AgainstNull(newRole, nameof(newRole));

        // No change – nothing to do.
        if (Role == newRole)
            return;

        Role = newRole;
    }
}


////Dry‑run — membership lifecycle example:
//// =========================================================================
//// 1. CREATION – A user joins a Home
//// =========================================================================
//var home = existingHome; // loaded from repository
//    var now = DateTime.UtcNow;

//home.AddMember(userId, HomeRole.Member, now);

//// A new HomeMembership is created internally:
////   Id       = "01HXYZ..." (unique ULID)
////   HomeId   = home.Id
////   UserId   = userId
////   Role     = HomeRole.Member
////   JoinedAt = now
//// Domain event: MemberAdded is raised.

//// =========================================================================
//// 2. ROLE CHANGE – The Home Owner promotes a Member to Admin
//// =========================================================================
//home.ChangeRole(userId, HomeRole.Admin);

//// The membership's Role is now HomeRole.Admin.
//// Domain event: RoleChanged is raised.

//// =========================================================================
//// 3. REMOVAL – The Home Owner removes a member
//// =========================================================================
//home.RemoveMember(userId);

//// The membership is removed from the Home's Members collection.
//// Domain event: MemberRemoved is raised.
