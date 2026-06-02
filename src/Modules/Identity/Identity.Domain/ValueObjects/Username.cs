// ==========================================================================
// LINE-BY-LINE C# EXPLANATION (VERIXORA DOMAIN LAYER)
// ==========================================================================

// using directive: imports types from SharedKernel.Domain.Common namespace
// Concept: Namespace import (C# basic)
// What we achieve: Access to ValueObject base class without full path
// Example: Without this, we'd write SharedKernel.Domain.Common.ValueObject
using SharedKernel.Domain.Common;

// using directive: imports DomainException class for invariant violations
// Concept: Exception handling in domain layer
// What we achieve: Throw domain-specific exceptions when business rules fail
// Example: throw new DomainException("IDENTITY_USERNAME_EMPTY");
using SharedKernel.Domain.Exceptions;

// using System: imports fundamental types like String, Int32
// Concept: Base class library (BCL) types
// What we achieve: Use string, int, etc. without System. prefix
using System;

// using System.Collections.Generic: brings IEnumerable<T>, HashSet<T>, IReadOnlySet<T>
// Concept: Generic collections
// What we achieve: Use strongly-typed collections for reserved names list
// Example: new HashSet<string> { "admin", "root" }
using System.Collections.Generic;

// using System.Text.RegularExpressions: provides Regex class for pattern matching
// Concept: Regular expression engine
// What we achieve: Validate username character pattern efficiently
// Example: new Regex("^[a-z0-9_]+$") matches only lowercase letters, digits, underscore
using System.Text.RegularExpressions;

// namespace declaration with file-scoped syntax (C# 10+)
// Concept: File-scoped namespace – reduces indentation and nesting
// What we achieve: All types in this file belong to Identity.Domain.ValueObjects
// Example: Fully qualified name becomes Identity.Domain.ValueObjects.Username
namespace Identity.Domain.ValueObjects;

// sealed class: cannot be inherited from
// Concept: Sealed modifier prevents inheritance (optimization + design clarity)
// What we achieve: Value objects should not be extended; ensures equality logic stays correct
// Example: new DerivedUsername() would break ValueObject equality – sealed prevents that
public sealed class Username : ValueObject  // inherits from abstract ValueObject base
{
    // const field: compile-time constant, cannot change
    // Concept: Constant literal – stored in metadata, no memory allocation per instance
    // What we achieve: Enforces minimum username length as a business rule
    // Example: new string('a', 2) would be rejected because 2 < MinLength
    private const int MinLength = 3;

    // const field: maximum allowed length for username
    // Concept: Same as above – compile-time constant
    // What we achieve: Upper boundary for username length (business invariant)
    // Example: Username longer than 30 characters rejected
    private const int MaxLength = 30;

    // [StringLength] attribute: metadata for validation (from DataAnnotations)
    // Concept: Attribute – adds declarative information to the field
    // What we achieve: Optional hint for tooling (e.g., EF Core, Swagger) that max length is 30
    // Example: When this value object is used in a DTO, tools can read this attribute
    // Note: This does NOT enforce runtime validation – the regex does that.
    [System.ComponentModel.DataAnnotations.StringLength(MaxLength)]

    // static readonly Regex: compiled regex pattern shared across all instances
    // Concept: Static field exists once per type, not per instance. Readonly prevents reassignment.
    // What we achieve: Memory efficiency + performance (one regex object for all Username validations)
    // RegexOptions.Compiled tells .NET to JIT-compile the regex into IL for faster matching.
    // Example: AllowedPattern.IsMatch("user_123") returns true; "User@123" returns false
    private static readonly Regex AllowedPattern =
        new("^[a-z0-9_]+$", RegexOptions.Compiled);

    // static readonly IReadOnlySet<string>: immutable set of reserved names
    // Concept: IReadOnlySet<T> (introduced in .NET 5) provides read-only collection semantics
    // What we achieve: Prevent accidental modification of reserved words list; O(1) lookup via HashSet
    // StringComparer.OrdinalIgnoreCase makes comparisons case-insensitive
    // Example: ReservedNames.Contains("ADMIN") returns true (ignores case)
    private static readonly IReadOnlySet<string> ReservedNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "admin",
            "root",
            "system",
            "null",
            "undefined"
        };

    // public get-only auto-property: exposes the actual username string
    // Concept: Immutable property – can be read by anyone, but only set inside the class (via private constructor)
    // What we achieve: Encapsulation – external code cannot change the value after creation
    // Example: var name = username.Value; // gets "john_doe"
    public string Value { get; }

    // private constructor: only the class itself can instantiate
    // Concept: Factory pattern – forces creation through static Create method, ensuring validation runs
    // What we achieve: Cannot bypass validation; no invalid Username instances exist
    // Example: new Username("bad") is impossible outside this class
    private Username(string value)
    {
        Value = value; // assign the validated, normalized value to the read-only property
    }

    /// <summary>
    /// Creates a validated and normalized Username
    /// </summary>
    // static factory method: responsible for all validation and construction
    // Concept: Factory method – centralizes creation logic, can return null or throw (here we throw)
    // What we achieve: Single entry point for domain object creation; ensures invariants hold
    // Example: Username.Create("  John_123  ") returns Username with Value = "john_123"
    public static Username Create(string input)
    {
        // Guard clause: checks for null, empty, or whitespace-only string
        // Concept: Defensive programming – fail fast on invalid input
        // What we achieve: Business rule: "Username must not be empty"
        // Example: Username.Create("   ") throws DomainException with code IDENTITY_USERNAME_EMPTY
        if (string.IsNullOrWhiteSpace(input))
            throw new DomainException("IDENTITY_USERNAME_EMPTY");

        // Normalization: Trim whitespace and convert to lowercase
        // Concept: String manipulation methods: Trim() removes leading/trailing spaces; ToLowerInvariant() uses culture-insensitive casing
        // What we achieve: Consistent storage and comparison ("John" and "john" become same)
        // Example: input = "  Bob_99  " → normalized = "bob_99"
        var normalized = input.Trim().ToLowerInvariant();

        // Length validation against MinLength and MaxLength constants
        // Concept: Comparison operators on string.Length property
        // What we achieve: Enforces business rule on character count
        // Example: normalized = "ab" (length 2) throws IDENTITY_USERNAME_LENGTH_INVALID
        if (normalized.Length < MinLength || normalized.Length > MaxLength)
            throw new DomainException("IDENTITY_USERNAME_LENGTH_INVALID");

        // Regex pattern matching: ensures only allowed characters (a-z, 0-9, underscore)
        // Concept: IsMatch returns bool; regex defined earlier
        // What we achieve: Character set rule – no uppercase, spaces, special chars except underscore
        // Example: normalized = "user.name" (dot not allowed) → throws INVALID_FORMAT
        if (!AllowedPattern.IsMatch(normalized))
            throw new DomainException("IDENTITY_USERNAME_INVALID_FORMAT");

        // Reserved name check: case-insensitive lookup in the read-only set
        // Concept: IReadOnlySet.Contains() – O(1) hash lookup
        // What we achieve: Prevent system-critical names from being used by regular users
        // Example: normalized = "admin" → throws IDENTITY_USERNAME_RESERVED
        if (ReservedNames.Contains(normalized))
            throw new DomainException("IDENTITY_USERNAME_RESERVED");

        // All validations passed: create new Username instance via private constructor
        // Concept: Object instantiation using `new` with private constructor (allowed inside the class)
        // What we achieve: Only valid, normalized Username objects enter the system
        // Example: returns Username object with Value = "bob_99"
        return new Username(normalized);
    }

    /// <summary>
    /// Value-based equality definition (handled by ValueObject base)
    /// </summary>
    // Override of protected abstract method from ValueObject base class
    // Concept: yield return creates an iterator over the components that define equality
    // What we achieve: The ValueObject base uses these components to compute Equals/GetHashCode
    // Example: Two Username objects with same Value ("john") will be considered equal
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value; // only the string Value matters for equality
    }
}