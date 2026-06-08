// ====================================================================
// VERIXORA – Identity.Application / Commands / VerifyEmail / VerifyEmailCommandValidator.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Validates the shape of the VerifyEmailCommand before it reaches
//   the handler.  The only field is UserId, which must not be the
//   default ULID (all zeros).
//
//   WHY FLUENTVALIDATION:
//     - Keeps validation declarative and separate from the handler.
//     - Automatically invoked by the CommandValidationBehaviour.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** inheriting from **AbstractValidator<T>**:
//    - The FluentValidation base class.
//
// 2. **RuleFor()**:
//    - Selects the property to validate and starts a rule chain.
//
// 3. **NotEqual()**:
//    - Built‑in rule that checks the value is not equal to a
//      specified constant.  Here we compare against the default
//      Ulid (which is a 16‑byte array of zeros, representing
//      "00000000000000000000000000").
//
// 4. **WithMessage(string)**:
//    - Custom error message.
//
// 5. **sealed** modifier:
//    - Prevents inheritance.
// ====================================================================

using FluentValidation;
using SharedKernel.Domain.Base;

namespace Identity.Application.Commands.VerifyEmail;

/// <summary>
/// Validates the shape of the <see cref="VerifyEmailCommand"/>.
/// </summary>
public sealed class VerifyEmailCommandValidator
    : AbstractValidator<VerifyEmailCommand>
{
    /// <summary>
    /// Defines the validation rules.
    /// </summary>
    public VerifyEmailCommandValidator()
    {
        // ------------------------------------------------------------
        // UserId validation
        // ------------------------------------------------------------
        RuleFor(x => x.UserId)
            // The UserId must not be the default/empty ULID.
            .NotEqual(Ulid.Empty)
            .WithMessage("A valid user ID is required.");
    }
}
