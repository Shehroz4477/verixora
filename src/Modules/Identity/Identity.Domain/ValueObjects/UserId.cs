using SharedKernel.Domain.Common;
using SharedKernel.Domain.Exceptions;
using System;

namespace Identity.Domain.ValueObjects
{
    /// <summary>
    /// ==========================================================
    /// USER ID VALUE OBJECT (IDENTITY PRIMITIVE)
    /// ==========================================================
    /// WHAT:
    /// Strongly-typed identifier for User Aggregate Root.
    ///
    /// WHY:
    /// - Prevents primitive obsession (Guid misuse)
    /// - Ensures type safety across domain boundaries
    /// - Improves readability and maintainability
    ///
    /// HOW:
    /// - Wraps Guid internally
    /// - Validated via factory methods
    /// - Uses ValueObject base for equality
    ///
    /// RULE:
    /// Identity objects must NEVER be primitive types in domain.
    /// ==========================================================
    /// </summary>
    public sealed class UserId : ValueObject
    {
        public Guid Value { get; }

        private UserId(Guid value)
        {
            Value = value;
        }

        /// <summary>
        /// Generates a new unique UserId
        /// </summary>
        public static UserId New()
        {
            return new UserId(Guid.NewGuid());
        }

        /// <summary>
        /// Creates UserId from existing Guid (DB / external systems)
        /// </summary>
        public static UserId From(Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new DomainException("IDENTITY_USER_INVALID_ID");
            }

            return new UserId(value);
        }

        /// <summary>
        /// Defines equality for ValueObject base
        /// </summary>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}