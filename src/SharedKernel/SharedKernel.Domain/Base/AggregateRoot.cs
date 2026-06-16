// ====================================================================
// VERIXORA – SharedKernel.Domain / Base / AggregateRoot.cs
// ====================================================================
// Summary:
//   Abstract base class for aggregate roots.
//   Inherits from Entity and explicitly marks the type as an
//   aggregate root, providing a strong semantic boundary that
//   repositories and architecture tests can enforce.
//
//   Why a base class instead of only a marker interface:
//     - Removes ambiguity: every aggregate root clearly derives
//       from AggregateRoot.
//     - Enforces intent in the type system (no accidental root).
//     - Simplifies repository constraints: `IRepository<TAggregate>
//       where TAggregate : AggregateRoot`.
//     - Provides a natural place for future aggregate‑root‑specific
//       behaviour (e.g., versioning, concurrency tokens).
//
//   Usage:
//     public class Home : AggregateRoot { ... }
//     public class User : AggregateRoot { ... }
//     public class Device : AggregateRoot { ... }
// ====================================================================

namespace SharedKernel.Domain.Base;

public abstract class AggregateRoot : Entity, IAggregateRoot
{
    // Inherits the parameterless constructor from Entity.
    // When EF Core materializes an aggregate, it uses this
    // constructor; a valid ULID is always generated.
    protected AggregateRoot() : base() { }

    // Constructor with a known ULID for rehydration or tests.
    protected AggregateRoot(Ulid id) : base(id) { }
}
