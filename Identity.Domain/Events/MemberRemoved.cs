// ====================================================================
// VERIXORA – Identity.Domain / Events / MemberRemoved.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   A domain event raised when a member is removed from a Home.
//   Handlers in the Application layer react to this event to:
//     - Revoke the user's permissions for that Home.
//     - Invalidate any active sessions scoped to that Home.
//     - Update the audit log.
//
//   WHY A DOMAIN EVENT:
//     - Decouples the Home aggregate from side effects.
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
/// Raised when a member is removed from a Home.
/// </summary>
public sealed record MemberRemoved(
    Ulid HomeId,
    Ulid UserId) : IDomainEvent
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
