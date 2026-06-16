// ====================================================================
// VERIXORA – SharedKernel.Domain / Base / Entity.cs
// ====================================================================
// Summary:
//   Abstract base class for all domain entities.
//   Every entity has:
//     - A unique, immutable identifier (ULID).
//     - A collection of domain events (for eventual consistency).
//     - Structural equality based on BOTH ID and concrete type.
//
//   Why ULID (our custom implementation):
//     Our SharedKernel contains a dependency‑free ULID class that
//     fulfills the ULID spec.  It is lexicographically sortable,
//     making it ideal for PostgreSQL clustered indexes.
//
//   Why domain events:
//     Enables the "tell, don't ask" principle.  Aggregates record
//     what happened; handlers react later.
//
//   IMPORTANT:
//     The internal domain event collection uses a HashSet, which
//     relies on proper equality and hash code implementations on
//     the IDomainEvent instances.  All events must be immutable and
//     provide consistent, identity‑based equality to guarantee
//     correct duplicate detection.
// ====================================================================

using SharedKernel.Domain.Events;

namespace SharedKernel.Domain.Base;

public abstract class Entity
{
    // --- Identity ---
    // Our custom ULID – see SharedKernel.Domain.Base.Ulid.
    // It is an immutable reference type that always contains a valid
    // 16‑byte value.  No default null state exists.
    public Ulid Id { get; protected set; }

    // --- Domain Events ---
    // Backed by HashSet for O(1) duplicate detection.
    private readonly HashSet<IDomainEvent> _domainEvents = new();

    // Exposed as a snapshot array to prevent external mutation.
    // Even though IReadOnlyCollection suggests read‑only, a malicious
    // or accidental cast back to HashSet could mutate the collection.
    public IReadOnlyCollection<IDomainEvent> DomainEvents
        => _domainEvents.Count == 0
            ? Array.Empty<IDomainEvent>()
            : _domainEvents.ToArray();

    // --- Constructors ---
    // 1. Parameterless constructor for EF Core materialization.
    //    EF Core creates an instance, then overwrites Id with the
    //    database value.  We generate a valid ULID anyway – there is
    //    no "invalid state" window, even for a fraction of a moment.
    protected Entity()
    {
        Id = Ulid.NewUlid();
    }

    // 2. Constructor accepting an existing ULID.
    //    Used by domain code and test fixtures that know the ID upfront.
    protected Entity(Ulid id)
    {
        Id = id;
    }

    // --- Domain Event Management ---
    /// <summary>
    /// Raises a domain event.  If the same event instance is already
    /// present (detected in O(1) via HashSet), it is silently ignored.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent is null)
            throw new ArgumentNullException(nameof(domainEvent));

        // HashSet.Add returns false if the item already exists,
        // so duplicate prevention is automatic.
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Atomically dequeues all pending domain events.
    /// Returns a snapshot and clears the internal collection.
    /// Safe to call multiple times – subsequent calls return an empty list.
    /// Uses the List constructor to avoid LINQ overhead in hot paths.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DequeueDomainEvents()
    {
        if (_domainEvents.Count == 0)
            return Array.Empty<IDomainEvent>();

        var events = new List<IDomainEvent>(_domainEvents);
        _domainEvents.Clear();
        return events;
    }

    // --- Structural Equality (ID + Type) ---
    // Two entities are the same only if they have the same ID AND the
    // same concrete type.  This prevents accidental cross‑type equality
    // (e.g., User(Id=1) == Device(Id=1) must be false).
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not Entity other) return false;
        return Id.Equals(other.Id) && GetType() == other.GetType();
    }

    public override int GetHashCode()
    {
        // Must be consistent with Equals, so we include the type.
        return HashCode.Combine(Id, GetType());
    }

    public static bool operator ==(Entity? left, Entity? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right)
        => !(left == right);
}
