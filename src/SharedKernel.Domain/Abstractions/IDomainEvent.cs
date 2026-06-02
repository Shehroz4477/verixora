// ==========================================================================
// LINE-BY-LINE C# EXPLANATION (VERIXORA SHARED KERNEL)
// ==========================================================================

// file-scoped namespace declaration (C# 10+)
// Concept: Namespace – organizes types into logical containers
// What we achieve: This interface belongs to SharedKernel.Domain.Abstractions
// Example: Fully qualified name is SharedKernel.Domain.Abstractions.IDomainEvent
namespace SharedKernel.Domain.Abstractions;

// XML documentation comment – describes the purpose of the interface
// Concept: Documentation comments – generate API docs and IntelliSense tooltips
// What we achieve: Other developers see this explanation when using IDomainEvent
/// <summary>
/// DOMAIN EVENT (DDD - Domain-Driven Design)
/// --------------------------------------------
/// A Domain Event represents something that has ALREADY HAPPENED
/// inside the domain.
/// ...
/// </summary>

// public interface: defines a contract that implementing classes must follow
// Concept: Interface – a set of method/property signatures without implementation
// What we achieve: Decouples domain event definition from specific implementations
// Example: Any class (e.g., UserRegistered) can implement IDomainEvent
public interface IDomainEvent
{
    // XML documentation comment – describes the property
    /// <summary>
    /// UTC timestamp of when the event occurred in the domain.
    /// 
    /// WHY UTC?
    /// - Avoids timezone inconsistencies across systems
    /// ...
    /// </summary>

    // property with getter only (no setter) – enforces immutability
    // Concept: Read-only property – value can be set only in constructor
    // What we achieve: Domain events are immutable after creation (rule of events)
    // Example: event.OccurredOnUtc returns a DateTime like "2025-06-02 14:30:00 UTC"
    DateTime OccurredOnUtc { get; }
}