// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / FeatureFlags / IFeatureFlagService.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Defines the contract for feature flag evaluation.
//   Feature flags allow new functionality to be deployed safely
//   and rolled out incrementally.  They can also serve as
//   emergency kill‑switches for problematic features.
//
//   WHY AN ABSTRACTION:
//     - Allows the implementation to be swapped (e.g., from a
//       simple configuration‑based service to Azure App Configuration
//       or LaunchDarkly) without changing consuming code.
//     - Enables mocking in unit tests.
//     - Follows the Dependency Inversion Principle.
//
//   USAGE:
//     In a controller or handler:
//       if (await _featureFlags.IsEnabledAsync("NewDashboard"))
//       {
//           return newDashboardView;
//       }
//       return oldDashboardView;
//
//   ADR‑029 REQUIREMENT:
//     All new features MUST be wrapped with feature flags.
//     This interface is the central point of enforcement.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **interface** keyword:
//    - Defines a contract that any implementing class must fulfil.
//    - Enables polymorphism and dependency inversion.
//
// 2. **public** access modifier:
//    - Accessible from any project that references this assembly.
//
// 3. **async / Task<bool>**:
//    - Feature flag evaluation may involve I/O (e.g., reading
//      configuration, querying a database, calling an external
//      service), so it must be asynchronous.
//
// 4. **string featureName** parameter:
//    - Identifies the feature.  By convention, use PascalCase
//      names (e.g., "NewDashboard", "BetaSearch").
//
// 5. **CancellationToken** (optional):
//    - Allows callers to cancel a long‑running feature check.
//      Defaults to <see cref="CancellationToken.None"/>.
//
// 6. **namespace** declaration:
//    - Keeps the interface organised within the feature flags
//      infrastructure component.
// ====================================================================

namespace BuildingBlocks.Infrastructure.FeatureFlags;

/// <summary>
/// Evaluates whether a named feature flag is currently enabled.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Checks whether the specified feature is enabled.
    /// </summary>
    /// <param name="featureName">
    /// The name of the feature flag (case‑insensitive).
    /// Example: "NewDashboard", "BetaSearch".
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// <c>true</c> if the feature is enabled; otherwise <c>false</c>.
    /// </returns>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);
}


//Dry‑run – how feature flags are used in a controller:
// Injected via constructor:
// private readonly IFeatureFlagService _featureFlags;

//public async Task<IActionResult> GetDashboard()
//    {
//        if (await _featureFlags.IsEnabledAsync("NewDashboard"))
//        {
//            return View("DashboardV2");
//        }
//        return View("Dashboard");
//    }
