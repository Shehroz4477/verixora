using System.Collections.Generic;
using System.Linq;

namespace SharedKernel.Domain.Common;

/// <summary>
/// VALUE OBJECT (DDD Concept)
/// --------------------------------------------
/// A Value Object is a domain object that:
/// - Has NO identity (unlike Entity)
/// - Is defined ONLY by its values
/// - Is IMMUTABLE (should not change after creation)
///
/// Example:
/// - Money (Amount + Currency)
/// - Email Address
/// - Physical Address
///
/// IMPORTANT RULE:
/// Two Value Objects are equal if ALL their values are equal.
/// </summary>

public abstract class ValueObject
{
    /// <summary>
    /// Defines the components that determine equality.
    /// Each derived ValueObject must return all properties
    /// that participate in equality comparison.
    /// </summary>
    protected abstract IEnumerable<object> GetEqualityComponents();

    /// <summary>
    /// VALUE-BASED EQUALITY CHECK
    /// --------------------------------------------
    /// C# FEATURE: Method Override
    /// We override object.Equals to implement custom equality logic.
    ///
    /// WHY?
    /// Default behavior compares memory reference (WRONG for Value Objects).
    /// We need value-based comparison instead.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;

        // LINQ SequenceEqual compares each element in order
        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// HASH CODE GENERATION
    /// --------------------------------------------
    /// C# FEATURE: Hash-based collections support
    ///
    /// WHY REQUIRED?
    /// If Equals is overridden, GetHashCode MUST also be overridden
    /// to maintain consistency in:
    /// - Dictionary
    /// - HashSet
    ///
    /// Otherwise behavior becomes unpredictable.
    /// </summary>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(0, (hash, component) =>
                HashCode.Combine(hash, component));
    }

    /// <summary>
    /// OPERATOR OVERLOAD: ==
    /// --------------------------------------------
    /// Makes domain code more readable:
    ///
    /// Instead of:
    ///     vo1.Equals(vo2)
    ///
    /// We can write:
    ///     vo1 == vo2
    /// </summary>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// OPERATOR OVERLOAD: !=
    /// --------------------------------------------
    /// Logical opposite of ==
    /// </summary>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}