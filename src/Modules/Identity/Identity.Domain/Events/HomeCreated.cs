// ====================================================================
// VERIXORA – Identity.Domain / Events / HomeCreated.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   A domain event raised when a new Home (tenant) is created.
//   Handlers in the Application layer react to this event to:
//     - Set up default authorisation policies for the Home.
//     - Send a welcome notification to the founding owner.
//     - Initialise audit logging for the new Home.
//
//   WHY A DOMAIN EVENT:
//     - Decouples the Home creation logic from its side effects.
//       The Home aggregate only records what happened; handlers
//       decide what to do about it.
//     - Enables multiple handlers to react independently without
//       the Home needing to know about them.
//
//   IMMUTABILITY:
//     - All properties are read‑only (init accessor).  Once
//       created, an event cannot be changed.
//     - The record type provides value‑based equality, so two
//       events with the same values are equal.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **record** (C# 9+):
//    - A reference type designed for immutable data.  It provides
//      value‑based equality, a `ToString()` that shows all
//      properties, and the `with` expression for creating modified
//      copies.
//    - `sealed` prevents further inheritance.
//
// 2. **Primary constructor** (C# 12):
//    - The parameters `HomeId`, `Name`, and `OwnerId` are declared
//      directly in the type definition.  They become public
//      properties automatically.
//
// 3. **init** accessor:
//    - Allows the property to be set during object initialisation
//      (e.g., `new HomeCreated { HomeId = ... }`) but not after
//      the object is fully constructed.  This enforces immutability.
//
// 4. **IDomainEvent** interface (from SharedKernel):
//    - Requires `EventId` (a ULID for deduplication) and
//      `OccurredOn` (a DateTimeOffset for the event timestamp).
//    - All domain events implement this interface so they can be
//      stored in the same outbox table.
//
// 5. **Ulid** (from SharedKernel):
//    - A time‑sortable unique identifier.  Used for `EventId` to
//      enable chronological event ordering.
//
// 6. **DateTimeOffset.UtcNow**:
//    - Captures the exact UTC instant when the event was raised.
//      `DateTimeOffset` includes the UTC offset explicitly,
//      avoiding time‑zone ambiguity.
// ====================================================================

using SharedKernel.Domain.Events;

namespace Identity.Domain.Events;

/// <summary>
/// Raised when a new Home is created.
/// </summary>
public sealed record HomeCreated(
    Ulid HomeId,
    string Name,
    Ulid OwnerId) : IDomainEvent
{
    /// <summary>
    /// Unique, time‑sortable identifier for this event instance.
    /// Used for deduplication and log sequencing.
    /// </summary>
    public Ulid EventId { get; init; } = Ulid.NewUlid();

    /// <summary>
    /// The UTC instant when the event was raised.
    /// </summary>
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
