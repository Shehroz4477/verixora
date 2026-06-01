namespace SharedKernel.Domain.Abstractions;

/// <summary>
/// DOMAIN EVENT (DDD - Domain-Driven Design)
/// --------------------------------------------
/// A Domain Event represents something that has ALREADY HAPPENED
/// inside the domain.
///
/// It is used to:
/// - Decouple domain logic from application logic
/// - Enable CQRS side effects (notifications, logging, workflows)
/// - Improve scalability and maintainability
///
/// Example events:
/// - UserRegistered
/// - DeviceActivated
/// - DoorUnlocked
///
/// IMPORTANT RULES:
/// - Domain Events are immutable (cannot change after creation)
/// - They represent past events, not future actions
/// </summary>

public interface IDomainEvent
{
    /// <summary>
    /// UTC timestamp of when the event occurred in the domain.
    /// 
    /// WHY UTC?
    /// - Avoids timezone inconsistencies across systems
    /// - Required for distributed systems (IoT, cloud, APIs)
    /// - Standard practice in enterprise architectures
    /// </summary>
    DateTime OccurredOnUtc { get; }
}