// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Persistence / OutboxMessage.cs
// ====================================================================
// Summary:
//   Represents a serialized domain event stored in the outbox table.
//   The outbox pattern guarantees that domain events are persisted
//   atomically with the aggregate changes, then published later by
//   a background processor.  This ensures reliable event delivery
//   even across process restarts.
// ====================================================================

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// An outbox message containing a serialized domain event.
/// </summary>
public class OutboxMessage
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>The CLR type name of the event.</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>JSON‑serialized event payload.</summary>
    public string EventPayload { get; private set; } = string.Empty;

    /// <summary>When the event was originally raised.</summary>
    public DateTime OccurredOnUtc { get; private set; }

    /// <summary>When this outbox message was created.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>True if the event has been successfully published.</summary>
    public bool Processed { get; private set; }

    /// <summary>When the event was published (or null if not yet).</summary>
    public DateTime? ProcessedAtUtc { get; private set; }

    /// <summary>Number of retry attempts.</summary>
    public int RetryCount { get; private set; }

    /// <summary>Last error message if processing failed.</summary>
    public string? Error { get; private set; }

    // Private constructor for EF Core materialization
    private OutboxMessage() { }

    /// <summary>
    /// Creates a new outbox message from a domain event.
    /// </summary>
    /// <param name="eventType">The CLR type name of the event.</param>
    /// <param name="eventPayload">The JSON‑serialized event payload.</param>
    /// <param name="occurredOnUtc">When the event originally occurred.</param>
    public OutboxMessage(string eventType, string eventPayload, DateTime occurredOnUtc)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        EventPayload = eventPayload;
        OccurredOnUtc = occurredOnUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Marks the message as successfully processed.</summary>
    public void MarkProcessed()
    {
        Processed = true;
        ProcessedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Records a failure for retry analysis.</summary>
    public void MarkFailed(string error)
    {
        RetryCount++;
        Error = error;
    }
}
