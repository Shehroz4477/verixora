// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / HealthChecks / HealthCheckExtensions.cs
// ====================================================================
// Summary:
//   Centralised health check registration for the API host.
//   Provides a `AddVerixoraHealthChecks` extension that wires up
//   standard probes.  Modules are expected to register their own
//   DbContext checks via the `configure` callback, keeping the
//   BuildingBlocks project decoupled from any specific DbContext.
//
//   Why a callback:
//     - Each module owns its own DbContext type(s); the BuildingBlocks
//       project cannot reference them (Clean Architecture).
//     - The callback lets the host or module assembly register
//       module‑specific checks without coupling.
//
//   Kubernetes tags:
//     - "ready"  – included in the readiness probe.
//     - "database" / "mqtt" – allow filtering in dashboards.
//
//   Module requirement:
//     When adding a DbContext check, modules MUST use the tags
//     `new[] { "ready", "database" }` to keep Kubernetes probes
//     and monitoring dashboards consistent.
// ====================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.Infrastructure.HealthChecks;

public static class HealthCheckExtensions
{
    /// <summary>
    /// Registers the core VERIXORA health checks.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An optional callback to add module‑specific checks
    /// (e.g., <c>builder.AddDbContextCheck&lt;IdentityDbContext&gt;(
    /// tags: new[] { "ready", "database" })</c>).</param>
    /// <param name="mqttBrokerAddress">If provided, an MQTT health check is added.</param>
    public static IServiceCollection AddVerixoraHealthChecks(
        this IServiceCollection services,
        Action<IHealthChecksBuilder>? configure = null,
        string? mqttBrokerAddress = null)
    {
        var builder = services.AddHealthChecks();

        // Allow each module to register its own DbContext checks.
        configure?.Invoke(builder);

        // MQTT broker health – only if a broker address is configured.
        if (!string.IsNullOrWhiteSpace(mqttBrokerAddress))
        {
            builder.Add(new HealthCheckRegistration(
                "MQTT",
                sp => new MqttHealthCheck(mqttBrokerAddress),
                HealthStatus.Unhealthy,
                new[] { "ready", "mqtt" }));
        }

        return services;
    }
}
