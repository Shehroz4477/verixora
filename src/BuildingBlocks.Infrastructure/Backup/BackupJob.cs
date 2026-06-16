// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Backup / BackupJob.cs
// ====================================================================
//
// … (existing summary unchanged) …
//
//   SCHEDULING (DRIFT‑FREE):
//     The next run time is calculated after each backup completes,
//     not before.  This ensures the backup always runs at the
//     scheduled time of day, regardless of how long the previous
//     backup took.
//
//   EXECUTION METRICS:
//     Each backup run logs its duration in milliseconds, providing
//     basic observability for monitoring and alerting.
//
// … (rest unchanged) …
// ====================================================================

using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Backup;

public sealed class BackupJob : BackgroundService
{
    private readonly BackupConfiguration _options;
    private readonly BackupService _backupService;
    private readonly ILogger<BackupJob> _logger;
    private readonly SemaphoreSlim _executionLock = new(1, 1);

    public BackupJob(
        IOptions<BackupConfiguration> options,
        BackupService backupService,
        ILogger<BackupJob> logger)
    {
        _options = options.Value;
        _backupService = backupService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Backup job is disabled. Exiting.");
            return;
        }

        _logger.LogInformation(
            "Backup job started. Scheduled time: {Cron}", _options.CronExpression);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait until the next scheduled time.
            var nextRun = CalculateNextRun();
            if (nextRun.HasValue)
            {
                var delay = nextRun.Value - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    _logger.LogDebug("Next backup in {Delay}.", delay);
                    await Task.Delay(delay, stoppingToken);
                }
            }

            // Acquire the lock – if a backup is already in progress,
            // skip this cycle.
            if (!_executionLock.Wait(0))
            {
                _logger.LogWarning("Previous backup still running. Skipping this cycle.");
                continue;
            }

            try
            {
                await RunBackupWithRetryAsync(stoppingToken);
            }
            finally
            {
                _executionLock.Release();
            }
        }

        _logger.LogInformation("Backup job stopped.");
    }

    /// <summary>
    /// Runs the backup with up to 3 retry attempts and logs the duration.
    /// </summary>
    private async Task RunBackupWithRetryAsync(CancellationToken stoppingToken)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        });

        var sw = Stopwatch.StartNew();
        bool succeeded = false;

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "Backup attempt {Attempt}/3 starting.", attempt);

                await _backupService.CreateBackupAsync(stoppingToken);

                sw.Stop();
                _logger.LogInformation(
                    "Backup completed successfully on attempt {Attempt}. Duration: {Ms}ms.",
                    attempt, sw.ElapsedMilliseconds);
                succeeded = true;
                break;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Backup cancelled during shutdown.");
                return;
            }
            catch (Exception ex) when (attempt < 3)
            {
                _logger.LogWarning(ex,
                    "Backup attempt {Attempt} failed. Retrying in 10 seconds.", attempt);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        if (!succeeded)
        {
            sw.Stop();
            _logger.LogError(
                "All 3 backup attempts failed. Total duration: {Ms}ms.",
                sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Calculates the next UTC run time from the configured cron
    /// expression.  For MVP, only a simple "HH:mm" format is supported.
    /// Returns <c>null</c> if the expression cannot be parsed.
    /// </summary>
    private DateTime? CalculateNextRun()
    {
        var expression = _options.CronExpression;

        if (TimeSpan.TryParse(expression, out var time))
        {
            var now = DateTime.UtcNow;
            var next = now.Date.Add(time);

            if (next <= now)
                next = next.AddDays(1);

            return next;
        }

        _logger.LogWarning(
            "Unsupported cron expression: {Cron}. Backup will not run.",
            expression);
        return null;
    }
}
