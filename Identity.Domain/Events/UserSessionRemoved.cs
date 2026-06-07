// ====================================================================
// VERIXORA – Identity.Domain / Events / UserSessionRemoved.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   A domain event raised when a user's session is removed (e.g.,
//   the user logs out or an admin forces logout).  Handlers in
//   the Application layer react to this event to:
//     - Update the audit log.
//     - Notify monitoring systems.
//
//   WHY A DOMAIN EVENT:
//     - Decouples the User aggregate from side effects.
//     - Allows multiple handlers to react independently.
//
//   IMMUTABILITY:
//     - All properties are read‑only (init accessor).  Once
//       created, an event cannot be changed.
//     - The record type provides value‑based equality.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **record** (C# 9+) with **primary constructor** (C# 12):
//    - Immutable data type with value‑based equality.
//    - Parameters become public properties automatically.
//
// 2. **IDomainEvent** interface (from SharedKernel):
//    - Requires `EventId` (ULID) and `OccurredOn` (DateTimeOffset).
//
// 3. **Ulid.NewUlid()** – generates a unique, time‑sortable event ID.
//
// 4. **DateTimeOffset.UtcNow** – captures the exact UTC instant.
// ====================================================================

using SharedKernel.Domain.Events;

namespace Identity.Domain.Events;

/// <summary>
/// Raised when a user's session is removed.
/// </summary>
public sealed record UserSessionRemoved(
    Ulid UserId,
    Ulid SessionId) : IDomainEvent
{
    /// <summary>
    /// Unique, time‑sortable identifier for this event instance.
    /// </summary>
    public Ulid EventId { get; init; } = Ulid.NewUlid();

    /// <summary>
    /// The UTC instant when the event was raised.
    /// </summary>
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
