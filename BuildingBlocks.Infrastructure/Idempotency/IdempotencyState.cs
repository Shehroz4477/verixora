namespace BuildingBlocks.Infrastructure.Idempotency;

/// <summary>
/// The lifecycle state of an idempotency key.
/// </summary>
public enum IdempotencyState
{
    /// <summary>No record exists (or it has expired) – safe to reserve.</summary>
    NotFound,

    /// <summary>
    /// The key has been reserved but the command has not yet completed.
    /// The caller MUST NOT re‑execute the command.
    /// </summary>
    InProgress,

    /// <summary>
    /// The command completed and its response is available.
    /// </summary>
    Completed
}
