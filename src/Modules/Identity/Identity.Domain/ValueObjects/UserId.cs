// ==========================================================================
// LINE-BY-LINE C# EXPLANATION (VERIXORA DOMAIN LAYER)
// ==========================================================================

// using directive: imports ValueObject base class for DDD value objects
// Concept: Namespace import – allows use of types without full qualification
// What we achieve: Write "ValueObject" instead of "SharedKernel.Domain.Common.ValueObject"
// Example: Without this, we'd have to fully qualify the inheritance
using SharedKernel.Domain.Common;

// using directive: imports DomainException for invariant violations
// Concept: Exception handling in domain layer
// What we achieve: Throw domain-specific exceptions when business rules fail
// Example: throw new DomainException("IDENTITY_USER_INVALID_ID");
using SharedKernel.Domain.Exceptions;

// using System: provides fundamental types like Guid, string
// Concept: Base Class Library (BCL) – essential types
// What we achieve: Use Guid and other core types without System. prefix
using System;

// namespace declaration with traditional block scope (not file-scoped)
// Concept: Namespace – organizes types into logical groups
// What we achieve: This class belongs to Identity.Domain.ValueObjects
// Example: Fully qualified name is Identity.Domain.ValueObjects.UserId
namespace Identity.Domain.ValueObjects
{
    // XML documentation comment: explains purpose and design decisions
    // Concept: Documentation comments – generate API docs and IntelliSense hints
    // What we achieve: Other developers see this explanation when using UserId
    // Example: Hover over UserId in IDE to see this text
    /// <summary>
    /// ==========================================================
    /// USER ID VALUE OBJECT (IDENTITY PRIMITIVE)
    /// ==========================================================
    /// WHAT:
    /// Strongly-typed identifier for User Aggregate Root.
    /// ...
    /// </summary>

    // sealed class: cannot be inherited from; inherits from ValueObject abstract base
    // Concept: Sealed modifier prevents inheritance (optimization + design clarity)
    // What we achieve: Value objects should not be extended; ensures equality logic stays correct
    // Example: class DerivedUserId : UserId { } would cause compiler error CS0509
    public sealed class UserId : ValueObject
    {
        // public get-only auto-property: exposes the underlying Guid value
        // Concept: Immutable property – can be read, but only set inside the class (via private constructor)
        // What we achieve: Encapsulation – external code can read but never modify the ID
        // Example: var idValue = userId.Value; // gets the Guid
        public Guid Value { get; }

        // private constructor: only the class itself can instantiate
        // Concept: Factory pattern – forces creation through static methods, ensuring validation runs
        // What we achieve: No bypass of validation; all UserId instances are valid
        // Example: new UserId(Guid.Empty) is impossible outside this class
        private UserId(Guid value)
        {
            // Assign the validated Guid to the read-only property
            Value = value;
        }

        // XML comment: explains what New() does
        /// <summary>
        /// Generates a new unique UserId
        /// </summary>
        // static factory method: creates a brand new UserId with a fresh random Guid
        // Concept: Factory method – provides a meaningful name for creation scenarios
        // What we achieve: Domain knows how to generate a new identity (not just "new UserId()")
        // Example: var userId = UserId.New(); // userId.Value is a new Guid, never empty
        public static UserId New()
        {
            // Guid.NewGuid() generates a cryptographically strong random UUID (version 4)
            // Concept: Static method on Guid struct – creates a unique 128-bit value
            // What we achieve: Distributed-safe, globally unique identifier
            // Example: returns something like "3f2504e0-4f89-11d3-9a0c-0305e82c3301"
            return new UserId(Guid.NewGuid());
        }

        // XML comment: explains From() usage
        /// <summary>
        /// Creates UserId from existing Guid (DB / external systems)
        /// </summary>
        // static factory method: rehydrates a UserId from a known Guid (e.g., from database)
        // Concept: Factory method for reconstitution – not for new generation
        // What we achieve: Separates "create new" from "recreate existing" – improves intent
        // Example: var userId = UserId.From(savedGuid);
        public static UserId From(Guid value)
        {
            // Guard clause: checks for empty Guid (00000000-0000-0000-0000-000000000000)
            // Concept: Defensive programming – fail fast on invalid input
            // What we achieve: Business rule: "User ID cannot be empty"
            // Example: UserId.From(Guid.Empty) throws DomainException with code IDENTITY_USER_INVALID_ID
            if (value == Guid.Empty)
            {
                // throw DomainException with error code (localization handled later)
                // Concept: Exception – signals invariant violation in domain layer
                // What we achieve: Stops creation of invalid identity; consistent with VERIXORA rules
                // Example: API middleware catches this and returns 400 with localized message
                throw new DomainException("IDENTITY_USER_INVALID_ID");
            }

            // All validations passed: create new UserId instance via private constructor
            // Concept: Object instantiation using `new` with private constructor (allowed inside the class)
            // What we achieve: Only valid, non-empty Guid objects become UserId
            // Example: returns UserId object wrapping the provided Guid
            return new UserId(value);
        }

        // XML comment: explains role in equality
        /// <summary>
        /// Defines equality for ValueObject base
        /// </summary>
        // Override of protected abstract method from ValueObject base class
        // Concept: Yield return creates an iterator over the components that define equality
        // What we achieve: The ValueObject base uses these components to compute Equals/GetHashCode
        // Example: Two UserId objects with the same underlying Guid are considered equal
        protected override IEnumerable<object> GetEqualityComponents()
        {
            // Only the Guid Value matters for equality – no other fields
            // Concept: Yield return – returns one element at a time; here just one element
            // What we achieve: Two UserId instances with same Guid are value-equal
            // Example: UserId.From(g1) == UserId.From(g1) true
            yield return Value;
        }
    }
}