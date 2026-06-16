// ====================================================================
// VERIXORA – Identity.Domain / Events / MemberAdded.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   A domain event raised when a new member is added to a Home.
//   Handlers in the Application layer react to this event to:
//     - Grant default permissions to the new member.
//     - Send a notification to the member.
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
// 3. **HomeRole** (Enumeration):
//    - Type‑safe role using the SharedKernel Enumeration pattern.
//
// 4. **Ulid.NewUlid()** – generates a unique, time‑sortable event ID.
//
// 5. **DateTimeOffset.UtcNow** – captures the exact UTC instant.
// ====================================================================

using Identity.Domain.Enums;
using SharedKernel.Domain.Events;

namespace Identity.Domain.Events;

/// <summary>
/// Raised when a user is added as a member of a Home.
/// </summary>
public sealed record MemberAdded(
    Ulid HomeId,
    Ulid UserId,
    HomeRole Role) : IDomainEvent
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
