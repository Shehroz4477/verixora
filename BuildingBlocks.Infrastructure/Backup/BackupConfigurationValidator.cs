// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Backup / BackupConfigurationValidator.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Validates <see cref="BackupConfiguration"/> at application startup
//   via the Options pattern.  Implements
//   <see cref="IValidateOptions{BackupConfiguration}"/> so that
//   validation runs automatically when <c>.ValidateOnStart()</c> is
//   called during DI registration.
//
//   This keeps the configuration class a pure data object (POCO),
//   adhering to the Single Responsibility Principle.
// ====================================================================

using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Backup;

/// <summary>
/// Validates the <see cref="BackupConfiguration"/> options.
/// </summary>
public sealed class BackupConfigurationValidator : IValidateOptions<BackupConfiguration>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, BackupConfiguration options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        // CronExpression must be provided; the scheduler validates syntax.
        if (string.IsNullOrWhiteSpace(options.CronExpression))
            return ValidateOptionsResult.Fail(
                "BackupConfiguration: CronExpression is required when backups are enabled.");

        // StoragePath must be provided.
        if (string.IsNullOrWhiteSpace(options.StoragePath))
            return ValidateOptionsResult.Fail(
                "BackupConfiguration: StoragePath is required when backups are enabled.");

        // RetentionDays must be within a reasonable range.
        if (options.RetentionDays is < 1 or > 3650)
            return ValidateOptionsResult.Fail(
                "BackupConfiguration: RetentionDays must be between 1 and 3650.");

        // EncryptionKeyId must be a valid GUID.
        if (string.IsNullOrWhiteSpace(options.EncryptionKeyId) ||
            !Guid.TryParse(options.EncryptionKeyId, out _))
            return ValidateOptionsResult.Fail(
                "BackupConfiguration: EncryptionKeyId must be a valid GUID when backups are enabled.");

        return ValidateOptionsResult.Success;
    }
}
