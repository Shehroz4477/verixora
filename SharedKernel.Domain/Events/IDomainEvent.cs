// ====================================================================
// VERIXORA – SharedKernel.Domain / Events / IDomainEvent.cs
// ====================================================================
// Summary:
//   Contract for all domain events.
//   Every domain event must carry a unique, sortable EventId (for
//   deduplication in distributed scenarios) and the UTC instant
//   when it occurred (with explicit offset to prevent ambiguity).
//
//   Design decisions (per architect review):
//     - EventId is a ULID, not a Guid.  ULIDs are time‑sortable,
//       making them ideal for event logs, replay debugging, and
//       distributed trace correlation.  This aligns with our
//       entity identity strategy.
//     - OccurredOn is a DateTimeOffset (UTC), not DateTime.
//       DateTimeOffset explicitly carries the UTC offset, removing
//       any ambiguity about time zone in multi‑region IoT systems.
//
//   Temporal model (critical distinction):
//     - EventId   = system ordering + uniqueness.
//                    The ULID timestamp defines the canonical
//                    sequence of events.  This is the authoritative
//                    order for replay and deduplication.
//     - OccurredOn = domain truth timestamp.
//                    This is the best‑known, domain‑reported time
//                    when the event logically happened.  In IoT
//                    systems, device clocks may drift or be
//                    inaccurate; this field is treated as
//                    "best‑effort truth", not guaranteed.
//
//   Correlation metadata (CorrelationId, CausationId) is deferred.
//   When needed, it will be added via a separate IHasCorrelation
//   interface or an envelope pattern to keep this contract minimal.
//
//   Future evolution:
//     - A separate IIntegrationEvent interface may be introduced for
//       cross‑module / cross‑service events, distinct from intra‑module
//       domain events.
// ====================================================================

using SharedKernel.Domain.Base;

namespace SharedKernel.Domain.Events;

public interface IDomainEvent
{
    /// <summary>
    /// Unique, time‑sortable identifier for this event instance.
    /// Used for deduplication, idempotency, and log sequencing.
    /// </summary>
    Ulid EventId { get; }

    /// <summary>
    /// The UTC instant when the event was raised, with explicit
    /// offset to avoid time‑zone ambiguity.
    /// Treated as best‑effort domain‑reported time.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
