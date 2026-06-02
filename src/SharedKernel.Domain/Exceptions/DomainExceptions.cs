// ==========================================================================
// LINE-BY-LINE C# EXPLANATION (VERIXORA SHARED KERNEL – DOMAIN EXCEPTION)
// ==========================================================================

// using System: imports fundamental types like Exception, string
// Concept: Base Class Library (BCL) – Exception lives in System namespace
// What we achieve: Use Exception without fully qualifying System.Exception
using System;

// namespace declaration with traditional block scope
// Concept: Organizes types into a logical container
// What we achieve: DomainException belongs to SharedKernel.Domain.Exceptions
// Example: Fully qualified name is SharedKernel.Domain.Exceptions.DomainException
namespace SharedKernel.Domain.Exceptions
{
    // XML documentation comment – explains purpose and design decisions
    /// <summary>
    /// ==========================================================
    /// DOMAIN EXCEPTION (VERIXORA CORE CONCEPT)
    /// ...
    /// </summary>

    // public class DomainException : Exception – inherits from System.Exception
    // Concept: Custom exception – extends built-in exception with domain-specific properties
    // What we achieve: Distinguish business rule violations from technical failures
    // Example: throw new DomainException("USER_EMAIL_NOT_VERIFIED");
    public class DomainException : Exception
    {
        /// <summary>
        /// Property: Code – machine-readable error identifier
        /// </summary>
        // public string Code { get; } – auto-implemented read-only property
        // Concept: Getter-only auto-property – can be set only in constructor
        // What we achieve: Exposes error code without allowing modification after creation
        // Example: catch (DomainException ex) { log.LogError("Error code: {Code}", ex.Code); }
        public string Code { get; }

        /// <summary>
        /// Constructor 1 – code only
        /// </summary>
        // public constructor with single string parameter
        // Concept: Constructor overloading – provide different ways to create exception
        // What we achieve: Minimal constructor when only error code is needed (no inner exception)
        // Example: throw new DomainException("DEVICE_LOCKED");
        public DomainException(string code)
            // : base(code) – calls base class (Exception) constructor with the code string as message
            // Concept: Base constructor chaining – ensures base class is properly initialized
            // What we achieve: Exception.Message gets the code (useful for debugging/logs)
            : base(code)
        {
            // Assign the code parameter to the read-only Code property
            Code = code;
        }

        /// <summary>
        /// Constructor 2 – code with inner exception
        /// </summary>
        // public constructor with code and innerException parameters
        // Concept: Constructor overloading – allows wrapping another exception
        // What we achieve: Preserve original exception (e.g., SQL error) inside domain exception
        // Example: try { db.Save(); } catch (SqlException ex) { throw new DomainException("DB_SAVE_FAILED", ex); }
        public DomainException(string code, Exception innerException)
            // : base(code, innerException) – passes code as message and innerException to base
            // Concept: Inner exception chain – maintains full stack trace and root cause
            // What we achieve: Debugging tools can drill into the original technical error
            : base(code, innerException)
        {
            // Assign the code parameter to the read-only Code property
            Code = code;
        }
    }
}