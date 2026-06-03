// ====================================================================
// VERIXORA – SharedKernel.Domain / Base / Enumeration.cs
// ====================================================================
// Summary:
//   Implements the "Enumeration" pattern (Jimmy Bogard).
//   Provides a type‑safe alternative to primitive C# enums.
//   Each enumeration value is a static field on the derived class,
//   giving it a unique Id and a human‑readable Name.
//
//   Why this pattern instead of enum:
//     - Standard enums cannot carry behaviour or extra data.
//     - Enumeration classes can have methods, additional properties,
//       and are extensible (new values can be added in derived classes).
//     - The Id persists cleanly in the database as an integer;
//       the Name is for display and debugging.
//     - Enables pattern matching and richer domain logic
//       than a primitive enum value.
//
//   Usage in VERIXORA:
//     DeviceStatus (Online, Offline, Maintenance, Decommissioned)
//     LockState   (Locked, Unlocked, Jammed)
//     AlertSeverity (Info, Warning, Critical)
//     HomeRole    (Owner, Admin, Member, Guest)
//
//   Example:
//     public class HomeRole : Enumeration
//     {
//         public static readonly HomeRole Owner  = new(1, "Owner");
//         public static readonly HomeRole Admin  = new(2, "Admin");
//         public static readonly HomeRole Member = new(3, "Member");
//         public static readonly HomeRole Guest  = new(4, "Guest");
//         private HomeRole(int id, string name) : base(id, name) { }
//     }
//
//   PERFORMANCE NOTE:
//     The `GetAll<T>()` method uses a per‑type cached array to avoid
//     repeated reflection allocations.  This makes it safe for hot
//     paths such as authorization pipelines, device status checks,
//     and API serialization.
//
//   IMMUTABILITY WARNING:
//     Derived classes MUST NOT expose public constructors.
//     Instances are intended to be static singletons defined as
//     `public static readonly` fields.  Creating new instances at
//     runtime can break identity semantics and domain invariants.
//     All values MUST be declared as static readonly fields.
// ====================================================================

using System.Reflection;

namespace SharedKernel.Domain.Base;

public abstract class Enumeration : IComparable
{
    /// <summary>
    /// The integer key stored in the database.
    /// Compact, fast to index, and stable across refactors.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// The human‑readable display name.
    /// Used in logs, APIs, and UI to identify the value.
    /// </summary>
    public string Name { get; }

    // Protected constructor – only the derived class itself can
    // create instances.  This guarantees the only valid values are
    // the static fields defined on the subclass.
    protected Enumeration(int id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Returns the Name for display purposes.
    /// </summary>
    public override string ToString() => Name;

    // ----------------------------------------------------------------
    // Type‑safe, per‑T cache (no unsafe casts).
    // The nested generic static class ensures each closed type
    // gets its own cache, initialized exactly once by the CLR.
    // ----------------------------------------------------------------
    /// <summary>
    /// Returns all defined values of a specific Enumeration subclass,
    /// sorted by their Id.  The result is cached per type and
    /// initialised once; subsequent calls are allocation‑free.
    /// Example: HomeRole.GetAll&lt;HomeRole&gt;() -> [Owner, Admin, Member, Guest]
    /// </summary>
    public static IReadOnlyList<T> GetAll<T>() where T : Enumeration
    {
        return EnumerationCache<T>.Values;
    }

    // Thread‑safe, type‑specific cache – automatically lazy.
    private static class EnumerationCache<T> where T : Enumeration
    {
        internal static readonly IReadOnlyList<T> Values = LoadValues();

        private static IReadOnlyList<T> LoadValues()
        {
            // Discover all public static fields of the exact enum type.
            var values = typeof(T)
                .GetFields(BindingFlags.Public |
                           BindingFlags.Static |
                           BindingFlags.DeclaredOnly)
                .Select(field => field.GetValue(null))
                .Cast<T>()
                .OrderBy(x => x.Id)        // deterministic ordering
                .ToArray();

            // Domain invariant: no duplicate Ids.
            // This protects against accidental copy/paste errors
            // when defining static fields.
            var duplicateIds = values
                .GroupBy(x => x.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Count > 0)
                throw new InvalidOperationException(
                    $"Duplicate Id(s) found in {typeof(T).Name}: {string.Join(", ", duplicateIds)}.");

            return values;
        }
    }

    // ----------------------------------------------------------------
    // Lookup helpers – essential for database hydration, API mapping,
    // and event deserialization.
    // ----------------------------------------------------------------
    /// <summary>
    /// Finds an enumeration value by its integer Id.
    /// Returns null if no match is found.
    /// </summary>
    public static T? FromId<T>(int id) where T : Enumeration
        => GetAll<T>().FirstOrDefault(x => x.Id == id);

    /// <summary>
    /// Finds an enumeration value by its Name (case‑insensitive).
    /// Returns null if the name is null, empty, or no match is found.
    /// </summary>
    public static T? FromName<T>(string name) where T : Enumeration
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return GetAll<T>().FirstOrDefault(
            x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    // ----------------------------------------------------------------
    // Equality – two enumerations are equal if they are the same type
    // and have the same Id.  This prevents cross‑type equality bugs
    // (e.g., LockState.Open != DeviceStatus.Open even if both have Id=1).
    // ----------------------------------------------------------------
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (obj is not Enumeration other) return false;
        // Must be the same concrete type AND have the same Id.
        return GetType() == other.GetType() && Id == other.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    // ----------------------------------------------------------------
    // Comparison – strict: same type required.
    // ----------------------------------------------------------------
    /// <summary>
    /// Compares to another enumeration.  The other object MUST be
    /// of the exact same derived type; otherwise an
    /// <see cref="ArgumentException"/> is thrown.
    /// </summary>
    public int CompareTo(object? other)
    {
        if (other is null) return 1;
        if (other is not Enumeration enumeration || GetType() != other.GetType())
            throw new ArgumentException(
                $"Object must be of the exact type {GetType().Name}.");
        return Id.CompareTo(enumeration.Id);
    }

    // Operator overloads for == and !=
    public static bool operator ==(Enumeration? left, Enumeration? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Enumeration? left, Enumeration? right)
        => !(left == right);
}
