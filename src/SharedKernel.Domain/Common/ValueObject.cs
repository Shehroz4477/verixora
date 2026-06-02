// ==========================================================================
// LINE-BY-LINE C# EXPLANATION (VERIXORA SHARED KERNEL – VALUE OBJECT BASE)
// ==========================================================================

// using System.Collections.Generic: brings IEnumerable<T> into scope
// Concept: Generic collections – needed for GetEqualityComponents() return type
// What we achieve: Ability to iterate over equality components
// Example: Without this, we'd have to write System.Collections.Generic.IEnumerable<object>
using System.Collections.Generic;

// using System.Linq: provides LINQ extension methods (SequenceEqual, Aggregate)
// Concept: LINQ to Objects – functional operations on collections
// What we achieve: Concise equality comparison and hash code aggregation
// Example: components.SequenceEqual(other.Components) compares all elements in order
using System.Linq;

// namespace declaration with file-scoped syntax (C# 10+)
// Concept: File-scoped namespace – reduces indentation
// What we achieve: All code belongs to SharedKernel.Domain.Common
namespace SharedKernel.Domain.Common;

// XML documentation comment: explains the purpose of ValueObject in DDD
/// <summary>
/// VALUE OBJECT (DDD Concept)
/// --------------------------------------------
/// A Value Object is a domain object that:
/// - Has NO identity (unlike Entity)
/// - Is defined ONLY by its values
/// - Is IMMUTABLE (should not change after creation)
/// ...
/// </summary>

// public abstract class: can be inherited but not instantiated directly
// Concept: Base class for all value objects in the system
// What we achieve: All value objects share the same equality logic, reducing duplication
// Example: Money, Email, Address all inherit from ValueObject
public abstract class ValueObject
{
    /// <summary>
    /// Defines the components that determine equality.
    /// Each derived ValueObject must return all properties
    /// that participate in equality comparison.
    /// </summary>
    // protected abstract method: derived classes MUST implement this
    // Concept: Template Method pattern – base class calls this to get equality components
    // What we achieve: Each value object decides which fields matter for equality
    // Example: Email returns Yield return Value; Address returns Street, City, ZipCode
    protected abstract IEnumerable<object> GetEqualityComponents();

    /// <summary>
    /// VALUE-BASED EQUALITY CHECK
    /// --------------------------------------------
    /// C# FEATURE: Method Override
    /// We override object.Equals to implement custom equality logic.
    /// ...
    /// </summary>
    // public override bool Equals(object? obj): overrides System.Object.Equals
    // Concept: Polymorphism – replaces default reference equality with value equality
    // What we achieve: Two value objects with same values are considered equal
    // Example: new Email("a@b.com") == new Email("a@b.com") returns true (different objects, same value)
    public override bool Equals(object? obj)
    {
        // Guard clause: if obj is null OR types differ, not equal
        // GetType() returns exact runtime type – prevents comparing Email with Address even if fields match
        // Concept: Type safety – value objects of different types are never equal
        // Example: new Email("x") vs new Address("x") – GetType() differs → false
        if (obj is null || obj.GetType() != GetType())
            return false;

        // Safe cast: since types matched, cast is safe
        var other = (ValueObject)obj;

        // LINQ SequenceEqual compares two sequences element by element in order
        // GetEqualityComponents() returns the list of component values from 'this'
        // other.GetEqualityComponents() returns the list from 'other'
        // Concept: Structural equality – each component must equal the corresponding component
        // What we achieve: All fields must match for equality to be true
        // Example: Address("Main", 123) vs Address("Main", 123) → SequenceEqual returns true
        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// HASH CODE GENERATION
    /// --------------------------------------------
    /// C# FEATURE: Hash-based collections support
    /// ...
    /// </summary>
    // public override int GetHashCode(): required when Equals is overridden
    // Concept: Consistency – if two objects are equal, their hash codes must be equal
    // What we achieve: Value objects can be used as keys in Dictionary<TKey,TValue> and HashSet<T>
    // Example: var dict = new Dictionary<Email, User>(); dict[email] = user; works correctly
    public override int GetHashCode()
    {
        // Aggregate: applies a function over each element, accumulating a result
        // Seed: 0 (starting hash value)
        // Function: (hash, component) => HashCode.Combine(hash, component)
        // HashCode.Combine combines two hash codes into one (introduced in .NET Core 2.1)
        // Concept: Combine hash codes of all components to produce final hash
        // What we achieve: Order matters – different orders produce different hash codes (good for distribution)
        // Example: components "Main" and 123 → hash = HashCode.Combine(HashCode.Combine(0, "Main"), 123)
        return GetEqualityComponents()
            .Aggregate(0, (hash, component) =>
                HashCode.Combine(hash, component));
    }

    /// <summary>
    /// OPERATOR OVERLOAD: ==
    /// --------------------------------------------
    /// Makes domain code more readable:
    /// </summary>
    // public static bool operator ==: overloads the equality operator
    // Concept: Operator overloading – allows custom behavior for built-in operators
    // What we achieve: Natural syntax vo1 == vo2 instead of vo1.Equals(vo2)
    // Example: if (email1 == email2) { ... } reads like natural language
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        // Both null → equal (consistent with null semantics)
        if (left is null && right is null)
            return true;

        // One null, other not null → not equal
        if (left is null || right is null)
            return false;

        // Delegate to the instance Equals method (which uses value-based comparison)
        return left.Equals(right);
    }

    /// <summary>
    /// OPERATOR OVERLOAD: !=
    /// --------------------------------------------
    /// Logical opposite of ==
    /// </summary>
    // Must be overloaded in pairs (if == is overloaded, != must be as well)
    // Concept: Consistency – !(left == right) should be the same as left != right
    // What we achieve: Natural syntax vo1 != vo2
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}