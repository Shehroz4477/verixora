// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Logging / SerilogEnricher.cs
// ====================================================================
// Summary:
//   A Serilog enricher that extracts tenant, user, and correlation
//   context from the current HTTP request (JWT‑populated Items) or
//   from a scoped ambient context for background services.  It
//   attaches these properties to every log event.
//
//   Security:
//     - Only trusted HttpContext.Items (set by authentication
//       middleware) are read; headers are never used to prevent
//       spoofing.
//
//   Observability:
//     - CorrelationId is sourced from the active OpenTelemetry
//       trace (Activity.Current.TraceId), guaranteeing consistency
//       with distributed tracing.
//
//   Background workers:
//     - Use AmbientContext.Begin() in a using block to scope the
//       context and prevent accidental leakage between jobs.
// ====================================================================

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace BuildingBlocks.Infrastructure.Logging;

public class VerixoraLogEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public VerixoraLogEnricher(IHttpContextAccessor? httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // 1. Request pipeline – trusted Items only.
        if (_httpContextAccessor?.HttpContext is { } httpContext)
        {
            AddPropertyIfPresent(logEvent, propertyFactory, "TenantId",
                httpContext.Items["TenantId"] as string);
            AddPropertyIfPresent(logEvent, propertyFactory, "UserId",
                httpContext.Items["UserId"] as string);
            AddPropertyIfPresent(logEvent, propertyFactory, "CorrelationId",
                Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier);
            return;
        }

        // 2. Background services – scoped ambient context.
        AddPropertyIfPresent(logEvent, propertyFactory, "TenantId",
            AmbientContext.TenantId);
        AddPropertyIfPresent(logEvent, propertyFactory, "UserId",
            AmbientContext.UserId);
        AddPropertyIfPresent(logEvent, propertyFactory, "CorrelationId",
            AmbientContext.CorrelationId ?? Activity.Current?.TraceId.ToString());
    }

    private static void AddPropertyIfPresent(
        LogEvent logEvent,
        ILogEventPropertyFactory factory,
        string propertyName,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            logEvent.AddPropertyIfAbsent(
                factory.CreateProperty(propertyName, value));
        }
    }

    /// <summary>
    /// Scoped ambient identity for background workers.
    /// Use <c>using var _ = AmbientContext.Begin(tenantId, userId, correlationId);</c>
    /// at the start of a job to prevent context leakage.
    /// </summary>
    public static class AmbientContext
    {
        private static readonly AsyncLocal<string?> _tenantId = new();
        private static readonly AsyncLocal<string?> _userId = new();
        private static readonly AsyncLocal<string?> _correlationId = new();

        public static string? TenantId => _tenantId.Value;
        public static string? UserId => _userId.Value;
        public static string? CorrelationId => _correlationId.Value;

        /// <summary>
        /// Returns a disposable scope that sets the ambient context
        /// and clears it on disposal.
        /// </summary>
        public static IDisposable Begin(string? tenantId, string? userId, string? correlationId)
        {
            _tenantId.Value = tenantId;
            _userId.Value = userId;
            _correlationId.Value = correlationId;
            return new AmbientScope();
        }

        private sealed class AmbientScope : IDisposable
        {
            public void Dispose()
            {
                _tenantId.Value = null;
                _userId.Value = null;
                _correlationId.Value = null;
            }
        }
    }
}
