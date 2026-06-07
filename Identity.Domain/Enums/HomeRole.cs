// ====================================================================
// VERIXORA – Identity.Domain / Enums / HomeRole.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Defines the possible roles a user can have within a Home
//   (tenant).  Roles control what actions a user can perform:
//   managing members, adding devices, controlling locks, etc.
//
//   WHY AN ENUMERATION CLASS INSTEAD OF A C# ENUM:
//     - Standard C# enums are just integers with labels.  They
//       cannot carry behaviour, additional data, or be extended.
//     - The Enumeration pattern (from SharedKernel) gives each
//       role a unique Id (persisted as an integer) and a Name
//       (for display and debugging), while still allowing the
//       class to have methods and properties.
//     - The Id is stable across refactors; the Name can change
//       without breaking the database.
//     - New roles can be added in the future without touching
//       existing code.
//
//   ROLE HIERARCHY (per Master Spec):
//     Owner  – full control over the Home, members, devices, API keys
//     Admin  – manage members (except Owners), manage devices
//     Member – use devices, view home status
//     Guest  – limited access, often time‑restricted
//
//   USAGE:
//     HomeRole.Owner  → { Id = 1, Name = "Owner" }
//     HomeRole.Admin  → { Id = 2, Name = "Admin" }
//     HomeRole.Member → { Id = 3, Name = "Member" }
//     HomeRole.Guest  → { Id = 4, Name = "Guest" }
//
//     HomeRole.FromId(1)   → HomeRole.Owner
//     HomeRole.FromName("Admin") → HomeRole.Admin
//     HomeRole.GetAll<HomeRole>() → [Owner, Admin, Member, Guest]
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** inheriting from **Enumeration**:
//    - `Enumeration` (from SharedKernel) provides the `Id` and
//      `Name` properties, equality by Id + Type, comparison by Id,
//      and the cached `GetAll<T>()` / `FromId<T>()` / `FromName<T>()`
//      methods.
//
// 2. **public static readonly fields**:
//    - Each role is a single, immutable instance stored in a static
//      field.  This guarantees there is exactly one `Owner` object
//      in the entire application, making comparisons safe and fast.
//    - `readonly` means the field can only be assigned at
//      declaration or in a static constructor, preventing
//      accidental reassignment.
//
// 3. **private constructor**:
//    - No external code can create a new `HomeRole`.  The only
//      valid instances are the static fields defined in this class.
//      This enforces the "controlled vocabulary" pattern.
//
// 4. **static methods** (`FromId`, `FromName`, `GetAll`):
//    - Inherited from the `Enumeration` base class.  These provide
//      type‑safe lookup without reflection overhead after the first
//      call (the base class caches the results per type).
// ====================================================================

using Identity.Domain.Enums;

namespace Identity.Domain.Enums;

/// <summary>
/// Represents the role of a user within a Home (tenant).
/// </summary>
public class HomeRole : Enumeration
{
    // ----------------------------------------------------------------
    // Static instances – the only valid values
    // ----------------------------------------------------------------

    /// <summary>
    /// Full control over the Home, its members, devices, and API keys.
    /// </summary>
    public static readonly HomeRole Owner = new(1, "Owner");

    /// <summary>
    /// Can manage members (except Owners) and devices.
    /// </summary>
    public static readonly HomeRole Admin = new(2, "Admin");

    /// <summary>
    /// Can use devices and view the Home status.
    /// </summary>
    public static readonly HomeRole Member = new(3, "Member");

    /// <summary>
    /// Limited access, often restricted by schedule.
    /// </summary>
    public static readonly HomeRole Guest = new(4, "Guest");

    // ----------------------------------------------------------------
    // Private constructor – prevents external instantiation
    // ----------------------------------------------------------------

    /// <summary>
    /// Creates a new HomeRole with the given Id and Name.
    /// Private to prevent external code from creating unauthorised
    /// roles.
    /// </summary>
    /// <param name="id">The integer key stored in the database.</param>
    /// <param name="name">The human‑readable display name.</param>
    private HomeRole(int id, string name) : base(id, name) { }
}



////Dry‑run — using HomeRole in domain logic:
//// =========================================================================
//// 1. COMPARING ROLES
//// =========================================================================
//var memberRole = HomeRole.Member;

//if (memberRole == HomeRole.Member)
//{
//    // This is true.  Two references to the same static instance
//    // are equal by reference AND by Id + Type.
//}

//// =========================================================================
//// 2. CHECKING IF A USER HAS SUFFICIENT PRIVILEGES
//// =========================================================================
//public bool CanManageDevices(HomeRole role)
//{
//    // Only Owners and Admins can manage devices.
//    return role == HomeRole.Owner || role == HomeRole.Admin;
//}

//// =========================================================================
//// 3. LOOKING UP A ROLE FROM THE DATABASE
//// =========================================================================
//// When reading a HomeMembership from the database, we get the
//// role's integer Id.  We convert it back to the static instance:
//var role = HomeRole.FromId(2);   // → HomeRole.Admin

//// =========================================================================
//// 4. GETTING ALL AVAILABLE ROLES (e.g., for a dropdown in the UI)
//// =========================================================================
//var allRoles = HomeRole.GetAll<HomeRole>();
//// → [Owner, Admin, Member, Guest]

//foreach (var r in allRoles)
//{
//    Console.WriteLine($"{r.Id}: {r.Name}");
//}
//// Output:
////   1: Owner
////   2: Admin
////   3: Member
////   4: Guest
