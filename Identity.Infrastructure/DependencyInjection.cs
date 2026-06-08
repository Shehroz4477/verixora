// ====================================================================
// VERIXORA – Identity.Infrastructure / DependencyInjection.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Registers all Identity.Infrastructure services into the
//   dependency injection container.  This includes the EF Core
//   DbContext, repositories, and application service
//   implementations (password hasher, JWT token service, system
//   clock).  The Identity.Presentation layer (or ApiHost) calls
//   this method to wire up the module's dependencies.
//
//   WHY A CENTRAL REGISTRATION:
//     - Keeps the composition root clean and predictable.
//     - Ensures all implementations are registered with the correct
//       lifetimes (DbContext = Scoped, Repositories = Scoped,
//       Services = Singleton where appropriate).
//     - Avoids scattering AddScoped/AddSingleton calls across
//       multiple files.
//
//   USAGE (in Identity.Presentation or ApiHost):
//     services.AddIdentityInfrastructure(configuration);
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **static class** with **extension method**:
//    - Adds behaviour to `IServiceCollection` without modifying it.
//    - The `this` keyword on the first parameter makes it an
//      extension method.
//
// 2. **AddDbContext<TContext>**:
//    - Registers the EF Core DbContext with the DI container.
//    - The `Scoped` lifetime means a new instance is created per
//      HTTP request, which is the recommended pattern for EF Core.
//
// 3. **AddScoped<TInterface, TImplementation>**:
//    - Registers a service with a scoped lifetime.  A new instance
//      is created once per HTTP request.
//
// 4. **AddSingleton<TInterface, TImplementation>**:
//    - Registers a service with a singleton lifetime.  The same
//      instance is shared across all requests.
//
// 5. **Configuration binding**:
//    - `configuration.GetSection(JwtOptions.SectionName)` reads the
//      "Jwt" section from appsettings.json and binds it to the
//      `JwtOptions` class.
// ====================================================================

using Identity.Application.Interfaces;
using Identity.Infrastructure.Options;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;
using Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

/// <summary>
/// Registers all Identity.Infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Identity Infrastructure layer services to the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ------------------------------------------------------------
        // EF Core DbContext (Scoped – one per HTTP request)
        // ------------------------------------------------------------
        // Reads the connection string from configuration.
        // Uses PostgreSQL with transient fault retry handling (ADR‑033).
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions
                    .EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null)));

        // ------------------------------------------------------------
        // Repositories (Scoped – same lifetime as DbContext)
        // ------------------------------------------------------------
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHomeRepository, HomeRepository>();

        // ------------------------------------------------------------
        // Application services (Singleton where stateless)
        // ------------------------------------------------------------
        // PasswordHasher is stateless and thread‑safe → Singleton.
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // SystemClock is stateless → Singleton.
        services.AddSingleton<IClock, SystemClock>();

        // JwtTokenService depends on IOptions<JwtOptions> which is
        // also a Singleton → register as Singleton.
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // ------------------------------------------------------------
        // Options (bound from configuration)
        // ------------------------------------------------------------
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        // The IUnitOfWork implementation is registered by
        // BuildingBlocks.Infrastructure or the ApiHost.  It is not
        // specific to the Identity module.

        return services;
    }
}
