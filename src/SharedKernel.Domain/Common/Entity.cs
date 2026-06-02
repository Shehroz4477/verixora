// ==========================================================================
// LINE-BY-LINE C# EXPLANATION (VERIXORA SHARED KERNEL – ENTITY BASE)
// ==========================================================================

// Imports generic collection types such as List<T>, Dictionary<TKey,TValue>.
// Required because we use List<object> later in this file.
using System.Collections.Generic;

// Imports IDomainEvent interface from SharedKernel.Abstractions.
// Needed to store domain events raised by the entity.
using SharedKernel.Domain.Abstractions;

// Namespace groups related domain classes together.
// SharedKernel = reusable across all modules.
// Domain = business rules layer.
// Common = shared domain building blocks.
namespace SharedKernel.Domain.Common;

// public   = accessible from any assembly/project.
// abstract = cannot be instantiated directly; must be inherited.
// class    = reference type.
// Entity<TId> = generic entity base class where TId can be Guid, int, string, etc.
public abstract class Entity<TId>
{
    // private  = accessible only inside this class.
    // readonly = reference cannot be reassigned after construction (the list object itself is fixed).
    // List<IDomainEvent> = a mutable list that holds domain events raised by this entity.
    // [] is a C# 12 collection expression that creates an empty list.
    // Concept: Field backing store for domain events.
    // What we achieve: Each entity can track its own events without exposing them to external modification.
    // Example: _domainEvents.Add(new UserRegisteredDomainEvent(userId));
    private readonly List<IDomainEvent> _domainEvents = [];

    // public = accessible everywhere.
    // TId = generic identifier type (e.g., Guid, int, string).
    // get = anyone can read Id.
    // protected set = only this class or derived classes can modify Id (ensures identity cannot be changed externally).
    // default! suppresses nullable warning until value is assigned (non-nullable TId assumption).
    // Concept: Auto-property with protected setter – encapsulates identity but allows derived classes to set it.
    // What we achieve: Entities have a stable identity that cannot be tampered with from outside.
    // Example: var entity = new User(); entity.Id = UserId.New(); // allowed only inside User class.
    public TId Id { get; protected set; } = default!;

    // protected constructor – can only be called by derived classes.
    // Parameterless constructor needed by EF Core and serialization.
    // Concept: Parameterless constructor for ORM frameworks.
    // What we achieve: EF Core can create proxy instances and materialize entities from the database.
    // Example: Used implicitly when EF Core instantiates a User entity from a database row.
    protected Entity()
    {
    }

    // Constructor that allows derived classes to set the Id at creation time.
    // protected = only derived classes can call it.
    // Concept: Constructor injection of identity.
    // What we achieve: Entities receive their identity when constructed, never after.
    // Example: public User(UserId id) : base(id) { }
    protected Entity(TId id)
    {
        Id = id;
    }

    // IReadOnlyCollection<IDomainEvent> – read-only view of the domain events list.
    // Expression-bodied property (=>) – concise syntax.
    // AsReadOnly() returns a read-only wrapper around _domainEvents – prevents external modification.
    // Concept: Exposing domain events as read-only collection.
    // What we achieve: External code (like event dispatcher) can read events but cannot add/remove directly.
    // Example: foreach (var evt in entity.DomainEvents) { await dispatcher.Dispatch(evt); }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // protected = only derived entities can raise domain events.
    // void = no return value.
    // Concept: Method to add a domain event to the entity's internal list.
    // What we achieve: Encapsulates event raising – derived entities call this method when a significant domain occurrence happens.
    // Example: AddDomainEvent(new UserPasswordChangedDomainEvent(Id, occurredOn));
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    // public – called by infrastructure after events have been dispatched (e.g., in a unit of work).
    // Concept: Clear all pending domain events.
    // What we achieve: After events are published, the entity's event list is cleared to avoid re-dispatching.
    // Example: After SaveChangesAsync, the repository calls entity.ClearDomainEvents().
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // override replaces System.Object.Equals implementation.
    // object? means nullable object parameter (allows passing null).
    // Concept: Value-based equality for entities – entities with same Id and type are considered equal.
    // What we achieve: Two separate instances representing the same database row are equal, enabling correct HashSet/Dictionary behavior.
    // Example: var user1 = new User(userId); var user2 = new User(userId); user1.Equals(user2) returns true.
    public override bool Equals(object? obj)
    {
        // Pattern matching: checks if obj is not null and of type Entity<TId>, and assigns to variable 'other'.
        // If it fails (obj is null or wrong type), returns false.
        // Concept: Type-safe casting and null check in one expression.
        // What we achieve: Ensures we only compare entities of the same generic type.
        if (obj is not Entity<TId> other)
        {
            return false;
        }

        // Fast path: reference equality – if both point to same object in memory, they are equal.
        // ReferenceEquals avoids calling virtual Equals and is very fast.
        // Concept: Memory address comparison.
        // What we achieve: Early return for identical objects, avoiding further checks.
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // If either entity has a null Id (transient, not yet persisted), they are not considered equal.
        // This prevents unsaved entities from being treated as equal.
        // Concept: Unsaved entities have no identity.
        // What we achieve: Transient entities are only equal to themselves, never to other unsaved entities.
        if (Id is null || other.Id is null)
        {
            return false;
        }

        // Use the default equality comparer for TId to compare Id values.
        // This works correctly for Guid, int, string, and any type that implements IEquatable<T>.
        // Concept: Generic equality comparison.
        // What we achieve: Correctly compares Id regardless of TId's underlying type.
        // Example: For Guid, it calls Guid.Equals; for int, Int32.Equals.
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    // Required whenever Equals is overridden (must be consistent).
    // Used by HashSet<T>, Dictionary<TKey,TValue>, and internal hashing.
    // Concept: GetHashCode must return same value for objects that are equal.
    // What we achieve: Entities with same Id produce same hash code, allowing correct storage in hash-based collections.
    // Example: new HashSet<Entity<Guid>> { user1, user2 } will treat them as a single element if Ids match.
    public override int GetHashCode()
    {
        // Ternary operator: if Id is null, return 0; otherwise return Id's hash code.
        // Concept: Handle null Id for transient entities.
        // What we achieve: All null Ids return the same hash code (0), which is fine because null Id entities are only considered equal to themselves.
        return Id is null ? 0 : Id.GetHashCode();
    }

    // Operator overloading for ==.
    // static – belongs to the type, not an instance.
    // returns bool.
    // Concept: Allows using == between two Entity<TId> objects naturally.
    // What we achieve: if (entity1 == entity2) works as expected, following the same equality logic as Equals.
    // Example: if (user1 == user2) { ... }
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        // Both null → equal.
        if (left is null && right is null)
        {
            return true;
        }

        // One null, other not null → not equal.
        if (left is null || right is null)
        {
            return false;
        }

        // Delegate to the instance Equals method.
        return left.Equals(right);
    }

    // Operator overloading for !=.
    // Simply negates the result of ==.
    // Concept: Consistent pair of operators.
    // What we achieve: if (user1 != user2) works naturally.
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }
}