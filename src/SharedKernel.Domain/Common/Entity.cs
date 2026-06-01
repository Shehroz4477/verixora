// Imports generic collection types such as List<T>, Dictionary<TKey,TValue>.
// Required because we use List<object> later in this file.
using System.Collections.Generic;

// Namespace groups related domain classes together.
// SharedKernel = reusable across all modules.
// Domain = business rules layer.
// Common = shared domain building blocks.
namespace SharedKernel.Domain.Common;

// public   = accessible from any assembly/project.
// abstract = cannot be instantiated directly; must be inherited.
// class    = reference type.
// Entity<TId> = generic entity base class where TId can be Guid, int, etc.
public abstract class Entity<TId>
{
    // private  = accessible only inside this class.
    // readonly = reference cannot be reassigned after construction.
    // List<object> stores domain events raised by this entity.
    // [] is C# collection expression creating an empty list.
    private readonly List<object> _domainEvents = [];

    // public = accessible everywhere.
    // TId = generic identifier type.
    // get = anyone can read Id.
    // protected set = only this class or derived classes can modify Id.
    // default! suppresses nullable warning until value is assigned.
    public TId Id { get; protected set; } = default!;

    // protected constructor.
    // Can only be called by derived classes.
    // Needed by EF Core and inheritance scenarios.
    protected Entity()
    {
    }

    // Constructor allowing entity creation with an identifier.
    protected Entity(TId id)
    {
        Id = id;
    }

    // IReadOnlyCollection prevents external modification.
    // Expression-bodied property (=>).
    // AsReadOnly() exposes a read-only wrapper around the list.
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    // protected = only derived entities can raise domain events.
    // void = no return value.
    protected void AddDomainEvent(object domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    // Removes all domain events after they have been dispatched.
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // override replaces System.Object.Equals implementation.
    // object? means nullable object parameter.
    public override bool Equals(object? obj)
    {
        // Pattern matching.
        // Checks type and casts in one statement.
        if (obj is not Entity<TId> other)
        {
            return false;
        }

        // Fast path:
        // If both references point to same object in memory,
        // they are definitely equal.
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Unsaved entities should not be considered equal.
        if (Id is null || other.Id is null)
        {
            return false;
        }

        // Generic equality comparison.
        // Works correctly for Guid, int, string, etc.
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    // Required whenever Equals is overridden.
    // Used by HashSet, Dictionary and internal .NET hashing.
    public override int GetHashCode()
    {
        // Ternary operator.
        return Id is null
            ? 0
            : Id.GetHashCode();
    }

    // Operator overloading for ==
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    // Operator overloading for !=
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }
}