// ====================================================================
// VERIXORA – SharedKernel.Domain / Base / ValueObject.cs
// ====================================================================
// Summary:
//   Abstract base class for immutable value objects.
//   Equality is determined by the values of their constituent parts,
//   not by identity.  This implementation is optimised for
//   high‑frequency comparisons (authorization, unlock pipeline,
//   device matching) and is fully thread‑safe.
//
//   Key design decisions:
//     - Atomic values are computed fresh each time to guarantee
//       correctness even in edge‑case object lifecycles (EF Core
//       proxies, serializers).  Value objects are small; the
//       performance cost of recomputation is negligible.
//     - Hash code via System.HashCode (collision‑resistant).
//     - Sealed equality overrides to prevent inconsistency.
//     - Stricter return type (IReadOnlyList) for performance.
//     - ToAtomicArray() helper creates a shallow defensive copy.
//     - Boxing cost: value types (int, ULID, enums) are boxed;
//       acceptable for current scale; future strongly‑typed variant
//       possible if profiling shows bottleneck.
//     - Debugging‑friendly ToString() for audit logs.
//
//   CRITICAL DOMAIN CONTRACT:
//     The order of atomic values returned by GetAtomicValues() is
//     part of the value object's identity.  Changing the order
//     silently changes equality semantics.  All derived classes
//     MUST guarantee a deterministic, documented order.
//
//   PURITY RULE:
//     GetAtomicValues() must be pure, deterministic, and side‑effect
//     free.  It must always return the same logical values for the
//     same instance.  It must not depend on mutable state or external
//     resources.  Violating this rule can cause non‑deterministic
//     equality bugs across distributed IoT systems.
//
// FUTURE EVOLUTION (deferred, non‑breaking):
//   – Enforce immutability of atomic values: consider a runtime
//     analyser or a linter rule to guarantee that only immutable
//     types (int, string, ULID, enums, etc.) are used.
//   – Governance for ordering contract: add an [AtomicOrder]
//     attribute to declare the expected sequence; validated by
//     architecture tests.
//   – Struct‑based ValueObject<T> for ultra‑hot paths to eliminate
//     boxing (SmartLocks, FaceVerification).
//   – Deep snapshot mode for rare cases where atomic values
//     contain nested mutable objects (not required now).
// ====================================================================

namespace SharedKernel.Domain.Base;

public abstract class ValueObject
{
    // ----------------------------------------------------------------
    // GetAtomicValues – derived classes MUST return the properties
    // that define equality, in a consistent, deterministic order.
    // This method must be PURE (no side effects, no mutable state).
    // ----------------------------------------------------------------
    /// <summary>
    /// Returns the atomic components that define equality.
    /// The order MUST be deterministic and documented.
    /// Must be pure, deterministic, and side‑effect free.
    /// Use the <see cref="ToAtomicArray"/> helper to create a
    /// shallow defensive copy.
    /// Example:
    ///   protected override IReadOnlyList<object> GetAtomicValues()
    ///       => ToAtomicArray(Amount, Currency);
    /// </summary>
    protected abstract IReadOnlyList<object> GetAtomicValues();

    /// <summary>
    /// Creates a shallow defensive copy of the given values.
    /// The returned array is a snapshot; modifications to the
    /// returned array will not affect the original object,
    /// but the objects inside are NOT cloned.
    /// </summary>
    protected static IReadOnlyList<object> ToAtomicArray(params object[] values)
        => values.ToArray();

    // ----------------------------------------------------------------
    // Equality – structural comparison using the atomic components.
    // ----------------------------------------------------------------
    public sealed override bool Equals(object? obj)
    {
        // Fast path: same reference.
        if (ReferenceEquals(this, obj)) return true;
        if (obj is null) return false;

        // Must be exactly the same type.
        if (GetType() != obj.GetType()) return false;

        var other = (ValueObject)obj;
        var thisValues = GetAtomicValues();
        var otherValues = other.GetAtomicValues();

        if (thisValues.Count != otherValues.Count) return false;

        for (int i = 0; i < thisValues.Count; i++)
        {
            // object.Equals handles nulls and delegates to the
            // overridden Equals on the actual runtime type.
            if (!object.Equals(thisValues[i], otherValues[i]))
                return false;
        }

        return true;
    }

    // ----------------------------------------------------------------
    // Hash code – must be consistent with Equals.
    // ----------------------------------------------------------------
    public sealed override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var value in GetAtomicValues())
            hash.Add(value);
        return hash.ToHashCode();
    }

    // ----------------------------------------------------------------
    // Debugging‑friendly representation.
    // ----------------------------------------------------------------
    public override string ToString()
        => $"{GetType().Name} [{string.Join(", ", GetAtomicValues())}]";

    // ----------------------------------------------------------------
    // Operator overloads.
    // ----------------------------------------------------------------
    public static bool operator ==(ValueObject? left, ValueObject? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !(left == right);
}
