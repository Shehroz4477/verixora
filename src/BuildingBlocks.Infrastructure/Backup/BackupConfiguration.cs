// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Backup / BackupConfiguration.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Configuration object for the automated database backup service.
//   Holds settings such as the backup schedule, storage location, and
//   retention policy.  These values are bound from appsettings.json
//   or a secrets manager at startup.
//
//   VALIDATION:
//     Performed by <see cref="BackupConfigurationValidator"/>, which
//     implements <see cref="IValidateOptions{BackupConfiguration}"/>
//     and is registered with <c>.ValidateOnStart()</c> in the DI
//     container.
//
//   CONFIGURATION SECTION NAME:
//     Use <see cref="SectionName"/> to reference this section.
//
//   EXAMPLE APPSETTINGS:
//   {
//     "Backup": {
//       "Enabled": true,
//       "CronExpression": "0 3 * * *",
//       "StoragePath": "/backups",
//       "RetentionDays": 30,
//       "EncryptionKeyId": "a3f1b2c4-d5e6-4789-a0b1-c2d3e4f5a6b7"
//     }
//   }
// ====================================================================

namespace BuildingBlocks.Infrastructure.Backup;

/// <summary>
/// Configuration for the automated database backup service.
/// </summary>
public class BackupConfiguration
{
    /// <summary>
    /// The configuration section name ("Backup").
    /// </summary>
    public const string SectionName = "Backup";

    /// <summary>
    /// Whether the backup service is enabled.
    /// Defaults to <c>true</c> – must be explicitly disabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// A cron expression that defines when the backup job runs.
    /// Example: "0 3 * * *" for every day at 03:00 UTC.
    /// Validated by the scheduler at job registration time.
    /// </summary>
    public string CronExpression { get; set; } = "0 3 * * *";

    /// <summary>
    /// The path where backup files are stored before being
    /// uploaded to off‑site storage.  Must be explicitly configured;
    /// no platform‑specific default is assumed.
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// The number of days to retain backup files.
    /// Must be between 1 and 3650 (10 years).
    /// Defaults to 30 days.
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// The key ID (GUID) of the AES‑256 encryption key used to
    /// encrypt the backup file.  Required whenever backups are
    /// enabled.  The key material itself is stored in the
    /// secrets manager.
    /// </summary>
    public string? EncryptionKeyId { get; set; }
}
