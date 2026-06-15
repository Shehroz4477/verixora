// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Encryption / EncryptionServiceExtensions.cs
// ====================================================================
// Summary:
//   Registers all encryption‑related services (IKeyProvider, IAadProvider,
//   IEncryptionService) and binds EncryptionOptions from configuration.
// ====================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Encryption;

public static class EncryptionServiceExtensions
{
    /// <summary>
    /// Adds the AES‑256 encryption services to the DI container.
    /// </summary>
    public static IServiceCollection AddVerixoraEncryption(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind and validate EncryptionOptions at startup
        services.AddOptions<EncryptionOptions>()
            .Bind(configuration.GetSection(EncryptionOptions.SectionName))
            .ValidateOnStart().Validate(o => { o.Validate(); return true; });

        // Register the required services as singletons
        services.AddSingleton<IKeyProvider, KeyProvider>();
        services.AddSingleton<IAadProvider, ContextAadProvider>();
        services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();

        return services;
    }
}
