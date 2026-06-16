// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Tracing / OpenTelemetryExtensions.cs
// ====================================================================
// Summary:
//   Extension methods for configuring OpenTelemetry tracing and
//   metrics in the API host.  Every module automatically benefits
//   from these settings because the host calls this method during
//   startup.  The observability stack fulfills ADR‑021 and provides
//   the foundation for the 200‑ms p95 unlock pipeline SLA.
//
//   Tracing:
//     - Every incoming HTTP request creates a trace span.
//     - The unlock pipeline steps are instrumented to create child
//       spans, giving full visibility into each stage.
//     - The EF Core instrumentation captures database commands as
//       spans, showing query latency.
//     - Exports traces to the console (development) and can be
//       extended to an OTLP collector (production).
//
//   Metrics:
//     - ASP.NET Core metrics (request duration, rate, errors).
//     - Custom metrics (unlock pipeline latency, device online/offline).
//     - Exported via the Prometheus endpoint (configured separately
//       in the host's Program.cs).
// ====================================================================

using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace BuildingBlocks.Infrastructure.Tracing;

public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing with ASP.NET Core and EF Core
    /// instrumentations.  Traces are exported to the console for
    /// development; replace with an OTLP exporter for production.
    /// </summary>
    public static IServiceCollection AddVerixoraTracing(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    // Creates a span for every incoming HTTP request.
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Enrich spans with tenant/user information
                        // from the authenticated request.
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            if (request.HttpContext.Items["TenantId"] is string tenantId)
                                activity.SetTag("tenant.id", tenantId);
                            if (request.HttpContext.Items["UserId"] is string userId)
                                activity.SetTag("user.id", userId);
                        };
                    })

                    // Captures EF Core commands as spans.
                    .AddEntityFrameworkCoreInstrumentation()

                    // Ensures that spans are created for background services
                    // (e.g., OutboxProcessor, MQTT handlers).
                    .AddSource("Verixora.*")

                    // Console exporter for local development.
                    // Replace with .AddOtlpExporter() for production
                    // to send traces to Jaeger, Zipkin, or an OTLP collector.
                    .AddConsoleExporter();
            });

        return services;
    }

    /// <summary>
    /// Adds OpenTelemetry metrics with ASP.NET Core instrumentation.
    /// Custom metrics (unlock pipeline duration, device status) are
    /// emitted via <see cref="System.Diagnostics.Metrics.Meter"/> and
    /// captured automatically.
    /// </summary>
    public static IServiceCollection AddVerixoraMetrics(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                builder
                    // ASP.NET Core request metrics (duration, count, errors).
                    .AddAspNetCoreInstrumentation()

                    // Collects runtime metrics (GC, thread pool, etc.).
                    .AddRuntimeInstrumentation()

                    // Prometheus exporter is configured in the host's
                    // Program.cs via app.MapPrometheusScrapingEndpoint().
                    // This builder enables the underlying meter listener.
                    .AddPrometheusExporter();
            });

        return services;
    }
}
