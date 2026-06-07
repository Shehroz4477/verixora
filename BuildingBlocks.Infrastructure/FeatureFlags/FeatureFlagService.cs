// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / FeatureFlags / FeatureFlagService.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements <see cref="IFeatureFlagService"/> using Microsoft's
//   built‑in <see cref="Microsoft.FeatureManagement.IFeatureManager"/>.
//   Feature flags are defined in appsettings.json (or any
//   configuration source) under the "FeatureManagement" section.
//
//   WHY THIS IMPLEMENTATION:
//     - Uses the standard .NET Feature Management library, which
//       integrates natively with ASP.NET Core configuration and
//       supports percentage‑based rollout, time‑window filters, and
//       custom filters.
//     - Normalises feature names to lowercase before evaluation,
//       making the API case‑insensitive and preventing silent
//       misconfiguration.
//     - The library caches feature states internally, so repeated
//       calls are fast and do not hit the configuration source.
//
//   EXAMPLE APPSETTINGS:
//   {
//     "FeatureManagement": {
//       "NewDashboard": true,
//       "BetaSearch": false
//     }
//   }
//
//   REGISTRATION (in ApiHost or module DI):
//     services.AddFeatureManagement();
//     services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** implementing an interface:
//    - `FeatureFlagService : IFeatureFlagService` guarantees it
//      provides the `IsEnabledAsync` method.
//
// 2. **Constructor injection**:
//    - `IFeatureManager` is injected and stored in a readonly field.
//
// 3. **IFeatureManager** (from Microsoft.FeatureManagement):
//    - The standard .NET abstraction for evaluating feature flags.
//      It reads from configuration by default.
//
// 4. **ToLowerInvariant()**:
//    - Normalises the feature name to lowercase, making the API
//      case‑insensitive.  This prevents bugs when different
//      callers use different casing.
//
// 5. **async / await**:
//    - All I/O (reading configuration) is asynchronous.
//
// 6. **sealed** modifier:
//    - Prevents inheritance, locking the behaviour.
// ====================================================================

using Microsoft.FeatureManagement;

namespace BuildingBlocks.Infrastructure.FeatureFlags;

/// <summary>
/// Evaluates feature flags using the .NET Feature Management library.
/// </summary>
public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly IFeatureManager _featureManager;

    /// <summary>
    /// Initialises the service with the feature manager.
    /// </summary>
    public FeatureFlagService(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(
        string featureName,
        CancellationToken cancellationToken = default)
    {
        // Normalise to lowercase so that feature names are
        // case‑insensitive across all callers.
        var normalisedName = featureName.ToLowerInvariant();

        // IFeatureManager.IsEnabledAsync reads from configuration
        // and applies any registered filters.
        return await _featureManager.IsEnabledAsync(normalisedName);
    }
}
