// ====================================================================
// VERIXORA – Identity.Application / DependencyInjection.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Registers all Identity.Application services into the dependency
//   injection container.  This includes MediatR handlers, FluentValidation
//   validators, and the SharedKernel pipeline behaviours.
//
//   WHY A CENTRAL REGISTRATION:
//     - Avoids scattering MediatR and FluentValidation registrations
//       across multiple files.
//     - Ensures all command/query handlers and validators are
//       discovered automatically via assembly scanning.
//     - The SharedKernel.Application pipeline behaviours
//       (validation, logging) are also registered here.
//
//   USAGE (in Identity.Presentation or ApiHost):
//     services.AddIdentityApplication();
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **static class** with **extension method**:
//    - Adds behaviour to `IServiceCollection` without modifying it.
//    - The `this` keyword on the first parameter makes it an
//      extension method.
//
// 2. **Assembly scanning** with `typeof(DependencyInjection).Assembly`:
//    - MediatR and FluentValidation scan the Identity.Application
//      assembly for all handlers and validators, automatically
//      registering them with the DI container.
//
// 3. **AddSharedKernelApplication()**:
//    - Registers the pipeline behaviours (CommandValidationBehaviour,
//      QueryValidationBehaviour, LoggingBehaviour) from the
//      SharedKernel.
// ====================================================================

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Application;

namespace Identity.Application;

/// <summary>
/// Registers all Identity.Application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Identity Application layer services to the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddIdentityApplication(
        this IServiceCollection services)
    {
        // Register MediatR – scans this assembly for all
        // IRequestHandler<TRequest, TResponse> implementations
        // (command and query handlers).
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Register FluentValidation validators – scans this
        // assembly for all IValidator<T> implementations.
        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly);

        // Register SharedKernel pipeline behaviours:
        //   - CommandValidationBehaviour<>
        //   - QueryValidationBehaviour<,>
        //   - LoggingBehaviour<,>
        services.AddSharedKernelApplication();

        return services;
    }
}
