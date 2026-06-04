// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / RateLimiting / RateLimitingExtensions.cs
// ====================================================================
// Summary:
//   Registers global and endpoint‑specific rate‑limiting policies as
//   required by ADR‑020.  All limits are defined as constants for
//   auditability, and the 429 response follows the ProblemDetails
//   standard via ASP.NET Core's WriteAsJsonAsync.
//
//   Policies:
//     • UserPolicy         – 100 req/min per user
//     • IpPolicy           – 200 req/min per IP
//     • ApiKeyPolicy       – 500 req/min per API key
//     • UnlockBurstPolicy  – 5 req / 10 sec per user (sliding window)
//
//   Identity sources:
//     - User ID from trusted HttpContext.Items (set by auth middleware).
//     - API key from X-Api-Key header (validated by auth middleware).
//     - Fallback is a constant "anonymous" to ensure a partition key.
// ====================================================================

using System.Linq;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.RateLimiting;

public static class RateLimitingExtensions
{
    // ADR‑020 limits (constants for governance)
    private const int UserPermitLimit = 100;
    private static readonly TimeSpan UserWindow = TimeSpan.FromMinutes(1);

    private const int IpPermitLimit = 200;
    private static readonly TimeSpan IpWindow = TimeSpan.FromMinutes(1);

    private const int ApiKeyPermitLimit = 500;
    private static readonly TimeSpan ApiKeyWindow = TimeSpan.FromMinutes(1);

    private const int UnlockBurstPermitLimit = 5;
    private static readonly TimeSpan UnlockBurstWindow = TimeSpan.FromSeconds(10);

    private const string AnonymousKey = "anonymous";

    /// <summary>
    /// Registers all rate‑limiting policies.
    /// </summary>
    public static IServiceCollection AddVerixoraRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Per‑user policy
            options.AddPolicy("UserPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetUserId(context) ?? AnonymousKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = UserPermitLimit,
                        Window = UserWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Per‑IP policy
            options.AddPolicy("IpPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = IpPermitLimit,
                        Window = IpWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Per‑API‑key policy
            options.AddPolicy("ApiKeyPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetApiKey(context) ?? AnonymousKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = ApiKeyPermitLimit,
                        Window = ApiKeyWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Unlock burst (sliding window, per user)
            options.AddPolicy("UnlockBurstPolicy", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetUserId(context) ?? AnonymousKey,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = UnlockBurstPermitLimit,
                        Window = UnlockBurstWindow,
                        SegmentsPerWindow = 2,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Global behaviour: 429 + ProblemDetails via WriteAsJsonAsync
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var loggerFactory = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("RateLimiting");

                logger.LogWarning(
                    "Rate limit exceeded. Endpoint: {Endpoint}, IP: {Ip}, User: {User}",
                    context.HttpContext.GetEndpoint()?.DisplayName,
                    context.HttpContext.Connection.RemoteIpAddress,
                    GetUserId(context.HttpContext) ?? AnonymousKey);   // fixed: use HttpContext

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var problem = new
                {
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Too many requests. Please try again later.",
                    instance = context.HttpContext.Request.Path
                };
                await context.HttpContext.Response.WriteAsJsonAsync(
                    problem,
                    cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Applies the rate‑limiting middleware.
    /// </summary>
    public static IApplicationBuilder UseVerixoraRateLimiting(this IApplicationBuilder app)
        => app.UseRateLimiter();

    private static string? GetUserId(HttpContext context)
        => context.Items["UserId"] as string;

    private static string? GetApiKey(HttpContext context)
        => context.Request.Headers["X-Api-Key"].FirstOrDefault();
}
