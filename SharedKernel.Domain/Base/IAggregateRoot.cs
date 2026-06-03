// ====================================================================
// VERIXORA – SharedKernel.Domain / Base / IAggregateRoot.cs
// ====================================================================
// Summary:
//   Marker interface for aggregate roots.
//   In Domain‑Driven Design, an aggregate root is the single entry
//   point for modifications to a cluster of related entities.
//   Repositories should ONLY persist aggregate roots; child entities
//   are accessed through the root.
//
//   Why an empty marker interface:
//     - The identity (Id) is already defined in the Entity base class.
//     - Adding methods here would force every aggregate to implement
//       them, even if they don't apply.
//     - The marker enables compile‑time checks: repository signatures
//       can accept `IAggregateRoot` to prevent accidentally saving
//       a non‑root entity.
//     - Architecture validation can detect violations (e.g., a
//       repository referencing an entity that isn't an aggregate root).
//
//   Usage (prefer the base class pattern):
//     public class Home : AggregateRoot { ... }
//     public class User : AggregateRoot { ... }
//
//   Note:
//     This interface is kept for flexibility; the recommended
//     usage is via the abstract `AggregateRoot` base class.
// ====================================================================

namespace SharedKernel.Domain.Base;

/// <summary>
/// Marker interface for aggregate roots.
/// Implement this on any entity that serves as the entry point
/// for a consistency boundary.
/// </summary>
public interface IAggregateRoot
{
    // Intentionally empty – serves as a compile‑time marker.
}
