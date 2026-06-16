// ====================================================================
// VERIXORA – SharedKernel.Application / Exceptions / ValidationException.cs
// ====================================================================
// Summary:
//   Custom exception thrown when a command or query fails validation.
//   Carries a collection of validation errors so that the API host
//   can produce a structured 400 Bad Request response.
//
//   Why a dedicated exception:
//     - The FluentValidation pipeline behaviour validates requests
//       before they reach the handler.  When validation fails, the
//       behaviour throws this exception instead of returning a
//       Result failure, so that MediatR's exception‑handling
//       middleware can catch it and map it to a ProblemDetails
//       response.
//     - Keeps the handler code clean — handlers only receive
//       valid requests.
//
//   Why not just return Result:
//     - The validation behaviour is a cross‑cutting concern.
//       Returning a Result would require every handler to return
//       a Result, even for queries.  Instead, we let exceptions
//       bubble up to the global exception handler, which already
//       knows how to produce a consistent error response.
//
//   Usage:
//     throw new ValidationException(
//         new Dictionary<string, string[]>
//         {
//             ["Email"] = new[] { "Email is required." },
//             ["Password"] = new[] { "Password must be at least 8 characters." }
//         });
// ====================================================================

namespace SharedKernel.Application.Exceptions;

/// <summary>
/// Represents one or more validation failures.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// A dictionary of property‑level errors.
    /// The key is the property name (or empty string for global errors),
    /// and the value is an array of error messages.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Creates a new <see cref="ValidationException"/> with the given
    /// property‑level errors.
    /// </summary>
    /// <param name="errors">A non‑empty dictionary of validation failures.</param>
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
    }

    /// <summary>
    /// Builds a human‑readable summary of the validation errors.
    /// </summary>
    private static string BuildMessage(IReadOnlyDictionary<string, string[]> errors)
    {
        var count = errors.Values.Sum(v => v.Length);
        if (count == 1)
            return $"Validation failed: {errors.Values.First().First()}";

        return $"Validation failed with {count} errors.";
    }
}
