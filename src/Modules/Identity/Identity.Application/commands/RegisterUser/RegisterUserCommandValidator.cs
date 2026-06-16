// ====================================================================
// VERIXORA – Identity.Application / Commands / RegisterUser / RegisterUserCommandValidator.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Validates the shape of the RegisterUserCommand before it reaches
//   the handler.  This validator is automatically invoked by the
//   CommandValidationBehaviour in the MediatR pipeline.  If
//   validation fails, the pipeline short‑circuits and returns a
//   Result.Failure without ever calling the handler.
//
//   WHY FLUENTVALIDATION:
//     - Keeps validation rules declarative and separate from the
//       handler.  The handler only deals with business rules
//       (duplicate email), not input shape.
//     - The SharedKernel's CommandValidationBehaviour automatically
//       discovers and runs all IValidator<T> implementations
//       registered in the DI container.
//     - Validation errors are collected into a structured
//       ValidationException, which the API host maps to a 400
//       Bad Request response with ProblemDetails.
//
//   SEPARATION OF CONCERNS:
//     - FluentValidation  = shape validation (required, length,
//                            format, regex).
//     - Handler           = business rules (email uniqueness).
//     - Domain            = invariants (email format, password
//                            hash format).
//
//   WHAT THIS VALIDATOR CHECKS:
//     1. Email is not empty.
//     2. Email matches a basic email pattern (contains '@' and a
//        domain part).
//     3. Password is not empty.
//     4. Password has a minimum length of 8 characters.
//
//   WHAT IT DOES NOT CHECK (handler's responsibility):
//     - Whether the email is already registered (requires a
//       database call).
//     - Password complexity beyond minimum length (can be added
//       later as a domain policy).
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** inheriting from **AbstractValidator<T>**:
//    - `AbstractValidator<T>` is the base class provided by the
//      FluentValidation library.  It gives us the `RuleFor()`
//      method and the `Validate()` / `ValidateAsync()` methods.
//    - The generic parameter `T` is the type we want to validate
//      — in this case, `RegisterUserCommand`.
//    - By inheriting from this base class, the MediatR pipeline
//      can automatically discover and invoke our validator.
//
// 2. **RuleFor()**:
//    - A fluent method that selects a property of the command
//      and starts a chain of validation rules for that property.
//    - `RuleFor(x => x.Email)` means "validate the Email property".
//    - The lambda expression `x => x.Email` is an expression tree
//      — FluentValidation uses it to know WHICH property is being
//      validated so it can include the property name in error
//      messages (e.g., "'Email' is required.").
//
// 3. **NotEmpty()**:
//    - A built‑in FluentValidation rule that checks the property
//      is not null and not an empty string.  If the value is null
//      or "", validation fails.
//    - This is the first rule in the chain for both Email and
//      Password.
//
// 4. **EmailAddress()**:
//    - A built‑in FluentValidation rule that checks the property
//      matches a standard email address pattern.
//    - It verifies the presence of an '@' symbol with characters
//      on both sides and a domain part.
//    - This is a basic check; it does NOT verify that the domain
//      actually exists or has MX records.
//
// 5. **MinimumLength(int)**:
//    - A built‑in FluentValidation rule that checks the string
//      property has at least the specified number of characters.
//    - Here we require at least 8 characters for passwords.
//    - Additional complexity rules (uppercase, digits, special
//      characters) can be added later if needed.
//
// 6. **WithMessage(string)**:
//    - Sets a custom error message for the preceding rule.
//    - If validation fails, this message is returned to the client
//      as part of the error response.
//    - Placeholders like `{PropertyName}` are automatically
//      replaced with the actual property name at runtime, but
//      we use explicit messages for clarity.
//
// 7. **sealed** modifier:
//    - Prevents other classes from inheriting from this validator.
//    - Validation rules should not be overridden by subclasses —
//      it would make the validation logic unpredictable.
//
// 8. **public** constructor:
//    - The DI container creates an instance of this validator via
//      assembly scanning (`AddValidatorsFromAssembly`).
//    - All validation rules are defined inside the constructor
//      because `RuleFor()` must be called when the validator is
//      instantiated.
//
// 9. **namespace**:
//    - The validator lives in the same namespace as the command
//      it validates: `Identity.Application.Commands.RegisterUser`.
//    - This is the Vertical Slice pattern — all code for a single
//      use case lives together in one folder.
// ====================================================================

using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Identity.Application.Commands.RegisterUser;

/// <summary>
/// Validates the shape of the <see cref="RegisterUserCommand"/>.
/// </summary>
public sealed class RegisterUserCommandValidator
    : AbstractValidator<RegisterUserCommand>
{
    /// <summary>
    /// Defines the validation rules for the registration command.
    /// </summary>
    public RegisterUserCommandValidator()
    {
        // ------------------------------------------------------------
        // Email validation rules
        // ------------------------------------------------------------
        RuleFor(x => x.Email)
            // Rule 1: The email must not be null or empty.
            .NotEmpty()
            .WithMessage("Email is required.")

            // Rule 2: The email must look like a valid email address
            // (contains '@' and a domain part).
            .EmailAddress()
            .WithMessage("A valid email address is required.");

        // ------------------------------------------------------------
        // Password validation rules
        // ------------------------------------------------------------
        RuleFor(x => x.Password)
            // Rule 1: The password must not be null or empty.
            .NotEmpty()
            .WithMessage("Password is required.")

            // Rule 2: The password must be at least 8 characters long.
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
    }
}




////Dry‑run — how the validator is invoked and what happens on failure:
//// ====================================================================
//// SCENARIO 1: Valid request — all rules pass
//// ====================================================================
//var command = new RegisterUserCommand("alice@example.com", "s3cr3t!!");
//    // Email: "alice@example.com"  → not empty ✓, valid email format ✓
//    // Password: "s3cr3t!!"        → not empty ✓, length 8 ✓

//    // The CommandValidationBehaviour calls:
//    var validationResult = await validator.ValidateAsync(command, ct);
//    // → validationResult.IsValid == true
//    // → The pipeline continues to the handler.  No exception is thrown.


//    // ====================================================================
//    // SCENARIO 2: Missing email — NotEmpty fails
//    // ====================================================================
//    var command = new RegisterUserCommand("", "s3cr3t!!");

//    // Validation result:
//    //   IsValid = false
//    //   Errors = [
//    //     { PropertyName = "Email", ErrorMessage = "Email is required." }
//    //   ]

//    // The CommandValidationBehaviour catches the validation failure,
//    // builds a ValidationException, and the pipeline short‑circuits.
//    // The handler is NEVER called.


//    // ====================================================================
//    // SCENARIO 3: Invalid email format — EmailAddress fails
//    // ====================================================================
//    var command = new RegisterUserCommand("not-an-email", "s3cr3t!!");

//    // Validation result:
//    //   IsValid = false
//    //   Errors = [
//    //     { PropertyName = "Email", ErrorMessage = "A valid email address is required." }
//    //   ]


//    // ====================================================================
//    // SCENARIO 4: Short password — MinimumLength fails
//    // ====================================================================
//    var command = new RegisterUserCommand("alice@example.com", "short");

//    // Validation result:
//    //   IsValid = false
//    //   Errors = [
//    //     { PropertyName = "Password", ErrorMessage = "Password must be at least 8 characters." }
//    //   ]


//    // ====================================================================
//    // SCENARIO 5: Multiple errors — all failures collected at once
//    // ====================================================================
//    var command = new RegisterUserCommand("", "");

//// Validation result:
////   IsValid = false
////   Errors = [
////     { PropertyName = "Email",    ErrorMessage = "Email is required." },
////     { PropertyName = "Password", ErrorMessage = "Password is required." },
////     { PropertyName = "Password", ErrorMessage = "Password must be at least 8 characters." }
////   ]

//// All three errors are returned to the client in a single 400 Bad
//// Request response.  The client can fix all issues at once instead
//// of submitting multiple times.
