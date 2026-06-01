using System;

namespace SharedKernel.Domain.Exceptions
{
    /// <summary>
    /// ==========================================================
    /// DOMAIN EXCEPTION (VERIXORA CORE CONCEPT)
    /// ==========================================================
    /// 
    /// 📌 WHAT IS THIS?
    /// This is a BASE exception used for ALL business rule violations
    /// inside the Domain layer of VERIXORA.
    /// 
    /// 📌 SIMPLE MEANING:
    /// If something breaks a BUSINESS RULE (not technical failure),
    /// we throw this exception.
    /// 
    /// Example:
    /// - User tries login but email is not verified
    /// - Device is locked but unlock is attempted
    /// - Invalid state transition in aggregate
    /// 
    /// ==========================================================
    /// WHY WE NEED THIS (VERY IMPORTANT)
    /// ==========================================================
    /// Without this:
    /// ❌ We would use System.Exception everywhere (bad practice)
    /// ❌ We cannot identify domain vs system errors
    /// ❌ Debugging becomes confusing in large systems
    /// ❌ Frontend cannot map errors properly
    /// 
    /// With this:
    /// ✅ Clear business rule failure indicator
    /// ✅ Clean separation from Infrastructure/Application errors
    /// ✅ Easier logging and monitoring
    /// 
    /// ==========================================================
    /// CLEAN ARCHITECTURE RULE
    /// ==========================================================
    /// Domain layer MUST NOT depend on:
    /// - UI (frontend)
    /// - API responses
    /// - Database
    /// - Localization
    /// 
    /// Domain ONLY expresses "WHAT is wrong", not "HOW to show it"
    /// ==========================================================
    /// </summary>
    public class DomainException : Exception
    {
        /// <summary>
        /// ==========================================================
        /// PROPERTY: Code
        /// ==========================================================
        /// 📌 WHAT IS THIS?
        /// A machine-readable identifier for the error.
        /// 
        /// Example:
        /// USER_EMAIL_NOT_VERIFIED
        /// DEVICE_LOCKED
        /// ACCESS_DENIED
        /// 
        /// 📌 WHY STRING, NOT ENUM?
        /// ✔ String allows distributed modules (micro/monolith safe)
        /// ✔ Easier to extend without recompiling shared contracts
        /// ✔ Works well with logging + APIs + frontend mapping
        /// 
        /// ALTERNATIVE:
        /// ❌ enum DomainErrorCode
        /// → safer at compile-time but less flexible for modular systems
        /// ==========================================================
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// ==========================================================
        /// CONSTRUCTOR 1
        /// ==========================================================
        /// 📌 PURPOSE:
        /// Used when ONLY error code is needed.
        /// 
        /// 📌 WHY PASS CODE TO base(code)?
        /// Because Exception requires a message.
        /// We reuse "code" as internal technical message for logs.
        /// 
        /// NOTE:
        /// This message is NOT for users.
        /// It is only for debugging/logging purposes.
        /// ==========================================================
        /// </summary>
        public DomainException(string code)
            : base(code)
        {
            Code = code;
        }

        /// <summary>
        /// ==========================================================
        /// CONSTRUCTOR 2
        /// ==========================================================
        /// 📌 PURPOSE:
        /// Used when wrapping another exception.
        /// 
        /// Example:
        /// SQL error → becomes DomainException
        /// NullReference → becomes DomainException
        /// 
        /// 📌 WHY innerException?
        /// ✔ Preserves original technical error
        /// ✔ Helps debugging root cause
        /// ✔ Maintains full exception stack trace
        /// 
        /// ==========================================================
        /// </summary>
        public DomainException(string code, Exception innerException)
            : base(code, innerException)
        {
            Code = code;
        }
    }
}