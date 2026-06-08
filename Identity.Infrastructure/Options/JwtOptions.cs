// ====================================================================
// VERIXORA – Identity.Infrastructure / Options / JwtOptions.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Configuration object for JWT token generation.  Holds the
//   secret key, issuer, audience, and token lifetime.  These
//   values are bound from appsettings.json or a secrets manager
//   (Azure Key Vault) and validated at startup.
//
//   WHY A DEDICATED OPTIONS CLASS:
//     - Centralises JWT settings for validation and binding.
//     - Supports the Options pattern (IOptions<T>) for DI.
//     - The SecretKey is REQUIRED and must come from a secure source.
//
//   EXAMPLE APPSETTINGS:
//   {
//     "Jwt": {
//       "SecretKey": "your-256-bit-secret-here...",
//       "Issuer": "verixora-api",
//       "Audience": "verixora-client",
//       "AccessTokenLifetimeMinutes": 15
//     }
//   }
// ====================================================================

namespace Identity.Infrastructure.Options;

/// <summary>
/// Configuration for JWT token generation.
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// The configuration section name ("Jwt").
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// The HMAC‑SHA256 secret key used to sign tokens.
    /// Must be at least 32 characters (256 bits).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The issuer of the token (typically the API's name).
    /// </summary>
    public string Issuer { get; set; } = "verixora-api";

    /// <summary>
    /// The intended audience of the token (typically the client app).
    /// </summary>
    public string Audience { get; set; } = "verixora-client";

    /// <summary>
    /// The lifetime of the access token in minutes.  Default is 15
    /// minutes per VERIXORA Master Spec.
    /// </summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
}
