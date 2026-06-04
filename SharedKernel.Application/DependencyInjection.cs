// ====================================================================
// VERIXORA – SharedKernel.Application / DependencyInjection.cs
// ====================================================================
// Summary:
//   Registers all SharedKernel.Application services into the
//   dependency injection container.  Every module's Application
//   layer calls this method from its own DependencyInjection.cs,
//   ensuring that pipeline behaviours (validation, logging) are
//   always configured consistently.
//
//   Why a central registration:
//     - Avoids duplicate behaviour registrations across 13 modules.
//     - Guarantees that the correct open‑generic types are
//       registered for MediatR pipeline behaviours.
//     - Makes it easy to add new cross‑cutting behaviours later
//       (idempotency, caching, authorisation) in a single place.
//
//   Usage in a module's Application layer:
//     public static IServiceCollection AddApplicationServices(
//         this IServiceCollection services)
//     {
//         services.AddSharedKernelApplication();
//         // ... module‑specific registrations
//     }
// ====================================================================

using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Application.Behaviours;

namespace SharedKernel.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Adds the SharedKernel.Application pipeline behaviours to the
    /// service collection.  Called once per module.
    /// </summary>
    public static IServiceCollection AddSharedKernelApplication(
        this IServiceCollection services)
    {
        // ------------------------------------------------------------
        // MediatR pipeline behaviours (executed in registration order)
        // ------------------------------------------------------------

        // 1. Validation – validates commands and queries before
        //    the handler runs.  Registered as open generics because
        //    MediatR resolves IPipelineBehavior<,> for each request.
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(CommandValidationBehaviour<>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(QueryValidationBehaviour<,>));

        // 2. Logging – logs every command/query with its execution
        //    time.  This is a general behaviour, not restricted to
        //    command or query types.
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(LoggingBehaviour<,>));

        return services;
    }
}
