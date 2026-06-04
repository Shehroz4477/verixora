// ====================================================================
// VERIXORA – SharedKernel.Application / Exceptions / NotFoundException.cs
// ====================================================================
// Summary:
//   Custom exception thrown when a resource that must exist (e.g.,
//   an aggregate loaded from a valid JWT, a device mid‑provisioning)
//   is unexpectedly missing.  For expected "not found" results in
//   queries, prefer returning a Result<T>.Failure(...) instead.
//
//   Why a dedicated exception:
//     - Distinguishes invariant failures from normal business outcomes.
//     - The global exception handler maps this to HTTP 404.
//     - Carries structured data (ResourceName, ResourceKey) for
//       ProblemDetails generation.
//
//   Design rule:
//     Throw this exception only for truly exceptional "missing
//     aggregate" conditions.  For query‑level "not found", use
//     Result<T>.Failure(...).
// ====================================================================

using SharedKernel.Domain.Guard;

namespace SharedKernel.Application.Exceptions;

/// <summary>
/// Represents a resource-not-found invariant failure.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// The name of the resource type (e.g., "Device", "User").
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// The identifier of the missing resource.
    /// </summary>
    public string ResourceKey { get; }

    /// <summary>
    /// Creates a new <see cref="NotFoundException"/> for the given
    /// resource and key.  Both arguments are required.
    /// </summary>
    /// <param name="resourceName">The type of resource (e.g., "Device").</param>
    /// <param name="resourceKey">The unique identifier of the missing resource.</param>
    public NotFoundException(string resourceName, string resourceKey)
        : base($"Resource '{resourceName}' with key '{resourceKey}' was not found.")
    {
        Guard.AgainstNullOrWhiteSpace(resourceName, nameof(resourceName));
        Guard.AgainstNullOrWhiteSpace(resourceKey, nameof(resourceKey));

        ResourceName = resourceName;
        ResourceKey = resourceKey;
    }
}
