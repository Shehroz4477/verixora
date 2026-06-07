// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Secrets / SecretsExtensions.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Provides an extension method for integrating Azure Key Vault into
//   the ASP.NET Core configuration system.  This is the concrete
//   implementation of ADR‑031 (Secrets Management).
//
//   WHY KEY VAULT:
//     - Connection strings, JWT signing keys, MQTT credentials, and
//       encryption keys are NEVER stored in configuration files.
//     - Key Vault provides centralised, audited, access‑controlled
//       secret storage.
//     - The Key Vault URI is the ONLY secret that must be present in
//       the environment (typically set at deployment time).
//
//   HOW IT WORKS:
//     1. The hosting environment provides `AZURE_KEY_VAULT_URI`.
//     2. The application authenticates using `DefaultAzureCredential`,
//        which tries managed identity, Visual Studio credentials, and
//        environment variables in order.
//     3. Key Vault secrets override any matching keys in the
//        application's configuration.
//
//   USAGE (in Program.cs):
//     builder.Configuration.AddAzureKeyVaultIfConfigured();
//
//   FALLBACK:
//     If `AZURE_KEY_VAULT_URI` is not set, Key Vault is skipped
//     silently.  This allows local development without Azure.
//     In production, the environment variable MUST be set; a
//     missing URI will cause the application to start without
//     secrets, which is a deployment‑level concern handled by
//     the infrastructure team, not by the application code.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **static class** with **extension methods** – adds behaviour to
//    `IConfigurationBuilder`.
// 2. **this IConfigurationBuilder builder** – extension method syntax.
// 3. **Environment.GetEnvironmentVariable** – reads the Key Vault URI.
// 4. **DefaultAzureCredential** – chain of Azure credentials.
// 5. **SecretClient** – reads secrets from Key Vault.
// 6. **AddAzureKeyVault** – the ASP.NET Core extension that adds
//    Key Vault as a configuration source.
// ====================================================================

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

namespace BuildingBlocks.Infrastructure.Secrets;

/// <summary>
/// Extension methods for integrating Azure Key Vault into configuration.
/// </summary>
public static class SecretsExtensions
{
    private const string KeyVaultUriEnvVar = "AZURE_KEY_VAULT_URI";

    /// <summary>
    /// Adds Azure Key Vault as a configuration source if a Key Vault
    /// URI is configured in the environment.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IConfigurationBuilder AddAzureKeyVaultIfConfigured(
        this IConfigurationBuilder builder)
    {
        var keyVaultUri = Environment.GetEnvironmentVariable(KeyVaultUriEnvVar);

        // If no Key Vault URI is configured, skip silently.
        // This allows local development without Azure dependencies.
        if (string.IsNullOrWhiteSpace(keyVaultUri))
        {
            return builder;
        }

        // Authenticate using the default Azure credential chain.
        var credential = new DefaultAzureCredential();
        var client = new SecretClient(new Uri(keyVaultUri), credential);

        // Add Key Vault as a configuration source.  Secrets are
        // loaded on demand and cached internally.
        builder.AddAzureKeyVault(client, new AzureKeyVaultConfigurationOptions
        {
            // Reload secrets periodically (every 5 minutes).
            ReloadInterval = TimeSpan.FromMinutes(5)
        });

        return builder;
    }
}
