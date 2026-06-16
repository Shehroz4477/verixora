// ====================================================================
// VERIXORA – Identity.Application / Commands / Login / LoginCommandValidator.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Validates the shape of the LoginCommand before it reaches the
//   handler.  This validator is automatically invoked by the
//   CommandValidationBehaviour in the MediatR pipeline.  If
//   validation fails, the pipeline short‑circuits and returns a
//   Result.Failure without ever calling the handler.
//
//   WHY FLUENTVALIDATION:
//     - Keeps validation rules declarative and separate from the
//       handler.  The handler only deals with business rules
//       (invalid credentials, email not verified), not input shape.
//     - The SharedKernel's CommandValidationBehaviour automatically
//       discovers and runs all IValidator<T> implementations.
//     - Validation errors are collected into a structured
//       ValidationException, which the API host maps to a 400
//       Bad Request response with ProblemDetails.
//
//   SEPARATION OF CONCERNS:
//     - FluentValidation  = shape validation (required, format).
//     - Handler           = business rules (password verification,
//                            email verified check).
//     - Domain            = invariants (session creation rules).
//
//   WHAT THIS VALIDATOR CHECKS:
//     1. Email is not empty.
//     2. Email matches a basic email pattern.
//     3. Password is not empty.
//
//   WHAT IT DOES NOT CHECK (handler's responsibility):
//     - Whether the email exists in the database.
//     - Whether the password is correct.
//     - Whether the email has been verified.
//     - Device fingerprint / IP / User‑Agent are not validated here
//       because they are optional context for session creation.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** inheriting from **AbstractValidator<T>**:
//    - `AbstractValidator<T>` is the base class provided by
//      FluentValidation.  It gives us the `RuleFor()` method.
//    - The generic parameter `T` is the type to validate — in
//      this case, `LoginCommand`.
//
// 2. **RuleFor()**:
//    - A fluent method that selects a property of the command
//      and starts a chain of validation rules for that property.
//    - `RuleFor(x => x.Email)` means "validate the Email property".
//
// 3. **NotEmpty()**:
//    - A built‑in rule that checks the property is not null and
//      not an empty string.
//
// 4. **EmailAddress()**:
//    - A built‑in rule that checks the property matches a
//      standard email address pattern (contains '@' and a domain).
//
// 5. **WithMessage(string)**:
//    - Sets a custom error message for the preceding rule.
//
// 6. **sealed** modifier:
//    - Prevents inheritance.  Validation rules should not be
//      overridden by subclasses.
//
// 7. **public** constructor:
//    - The DI container creates an instance via assembly scanning.
//    - All rules are defined in the constructor.
// ====================================================================

using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Identity.Application.Commands.Login;

/// <summary>
/// Validates the shape of the <see cref="LoginCommand"/>.
/// </summary>
public sealed class LoginCommandValidator
    : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Defines the validation rules for the login command.
    /// </summary>
    public LoginCommandValidator()
    {
        // ------------------------------------------------------------
        // Email validation rules
        // ------------------------------------------------------------
        RuleFor(x => x.Email)
            // Rule 1: The email must not be null or empty.
            .NotEmpty()
            .WithMessage("Email is required.")

            // Rule 2: The email must look like a valid email address.
            .EmailAddress()
            .WithMessage("A valid email address is required.");

        // ------------------------------------------------------------
        // Password validation rules
        // ------------------------------------------------------------
        RuleFor(x => x.Password)
            // Rule 1: The password must not be null or empty.
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}



////Dry‑run — how the validator is invoked and what happens on failure:
//// ====================================================================
//// SCENARIO 1: Valid request — all rules pass
//// ====================================================================
//var command = new LoginCommand(
//    "alice@example.com", "s3cr3t!!", "fp-abc", "203.0.113.1", "Chrome/120");
//    // Email: "alice@example.com" → not empty ✓, valid email ✓
//    // Password: "s3cr3t!!" → not empty ✓

//    // The CommandValidationBehaviour calls:
//    var validationResult = await validator.ValidateAsync(command, ct);
//    // → validationResult.IsValid == true
//    // → The pipeline continues to the handler.


//    // ====================================================================
//    // SCENARIO 2: Missing email
//    // ====================================================================
//    var command = new LoginCommand("", "s3cr3t!!", "", "", "");
//    // Email: "" → fails NotEmpty()

//    // Validation result:
//    //   IsValid = false
//    //   Errors = [
//    //     { PropertyName = "Email", ErrorMessage = "Email is required." }
//    //   ]

//    // The handler is never called.  The pipeline returns a 400 Bad Request.


//    // ====================================================================
//    // SCENARIO 3: Invalid email format
//    // ====================================================================
//    var command = new LoginCommand("not-an-email", "s3cr3t!!", "", "", "");

//    // Validation result:
//    //   IsValid = false
//    //   Errors = [
//    //     { PropertyName = "Email", ErrorMessage = "A valid email address is required." }
//    //   ]


//    // ====================================================================
//    // SCENARIO 4: Missing password
//    // ====================================================================
//    var command = new LoginCommand("alice@example.com", "", "", "", "");

//    // Validation result:
//    //   IsValid = false
//    //   Errors = [
//    //     { PropertyName = "Password", ErrorMessage = "Password is required." }
//    //   ]


//    // ====================================================================
//    // SCENARIO 5: Both missing
//    // ====================================================================
//    var command = new LoginCommand("", "", "", "", "");

//// Validation result:
////   IsValid = false
////   Errors = [
////     { PropertyName = "Email", ErrorMessage = "Email is required." },
////     { PropertyName = "Password", ErrorMessage = "Password is required." }
////   ]
