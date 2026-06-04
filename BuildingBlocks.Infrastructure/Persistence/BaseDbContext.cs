// ====================================================================
// VERIXORA – BuildingBlocks.Infrastructure / Persistence / BaseDbContext.cs
// ====================================================================
// Summary:
//   Abstract base class for all module‑specific EF Core DbContexts.
//   Provides:
//
//     - Transactional outbox pattern: domain events are stored in
//       the OutboxMessages table as part of the same SaveChanges
//       transaction.  They are later published by a background
//       OutboxProcessor, guaranteeing consistency even if the
//       process restarts.
//
//     - Automatic assembly‑based configuration loading.
//
//   Why outbox:
//     - Guarantees that domain events are never lost if the process
//       crashes after SaveChanges but before publishing.
//     - Keeps the unlock pipeline fast (200ms p95) because event
//       publishing happens asynchronously.
//     - Aligns with ADR‑010 (Audit Immutability) and ADR‑012
//       (Security‑first).
//
//   Note:
//     - Retry/connection configuration is NOT done here; each
//       module's DI configures the provider with EnableRetryOnFailure
//       when building DbContextOptions.
//     - Domain events are accessed via the Entity base class and
//       filtered to IAggregateRoot to only process aggregate roots.
//     - The OutboxProcessor (background service) will be added later
//       in BuildingBlocks.Infrastructure to process the outbox.
// ====================================================================

using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain.Base;
using SharedKernel.Domain.Events;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Base DbContext for all module contexts.
/// </summary>
public abstract class BaseDbContext : DbContext
{
    /// <summary>
    /// The outbox messages table – persisted atomically with aggregates.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// Initialises the context with the given options.
    /// No mediator is injected; events are written to the outbox only.
    /// </summary>
    protected BaseDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Applies entity configurations from the module's assembly.
    /// Each module overrides OnModelCreating in its own DbContext,
    /// calling base.OnModelCreating first to apply this shared logic.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Automatically discover and apply all IEntityTypeConfiguration<T>
        // implementations in the derived DbContext's assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetAssembly(GetType())!);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Saves aggregate changes and writes domain events to the
    /// outbox table in a single database transaction.
    /// </summary>
    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        // 1. Collect domain events from tracked aggregate roots.
        //    We query for Entity because DomainEvents lives on Entity;
        //    we filter to IAggregateRoot to only process aggregate roots.
        var aggregateRoots = ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity is IAggregateRoot && e.Entity.DomainEvents.Any())
            .ToList();

        var outboxMessages = new List<OutboxMessage>();

        foreach (var entry in aggregateRoots)
        {
            // DequeueDomainEvents atomically returns the snapshot of events
            // and clears the aggregate's internal collection.
            var events = entry.Entity.DequeueDomainEvents();

            foreach (var domainEvent in events)
            {
                // Serialise the domain event to JSON.
                var message = new OutboxMessage(
                    domainEvent.GetType().FullName!,
                    JsonSerializer.Serialize(domainEvent),
                    domainEvent.OccurredOn.UtcDateTime);
                outboxMessages.Add(message);
            }
        }

        // 2. Add outbox messages to the context (they will be saved
        //    in the same transaction as the aggregate changes).
        OutboxMessages.AddRange(outboxMessages);

        // 3. Persist aggregates + outbox messages atomically.
        return await base.SaveChangesAsync(cancellationToken);
    }
}
