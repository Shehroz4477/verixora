// ====================================================================
// VERIXORA – Identity.Infrastructure / Persistence / IdentityDbContext.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   The EF Core DbContext for the Identity bounded context.  It
//   inherits from <see cref="BaseDbContext"/>, which provides the
//   transactional outbox pattern (domain events are stored in the
//   OutboxMessages table atomically with aggregate changes) and
//   automatic assembly‑based configuration loading.
//
//   WHY A SEPARATE DBCONTEXT PER MODULE:
//     - Each module owns its own schema and migrations.
//     - Prevents cross‑module coupling at the database level.
//     - Allows independent deployment and evolution of each module.
//
//   AGGREGATE ROOTS:
//     - User – registered users with sessions, trusted devices,
//       and refresh tokens.
//     - Home – tenants with memberships and roles.
//
//   SCHEMA:
//     - All tables are in the "identity" schema by default.
//     - Entity configurations (indexes, constraints, value
//       converters) are defined in separate configuration classes
//       and discovered automatically via assembly scanning.
//
//   HOW THE OUTBOX WORKS (inherited from BaseDbContext):
//     When SaveChangesAsync is called, the base class:
//       1. Collects domain events from tracked User and Home
//          aggregates.
//       2. Serialises them into OutboxMessage objects.
//       3. Adds those OutboxMessages to the same transaction.
//       4. Commits everything atomically.
//     No mediator or event publisher is involved – events are
//     persisted first, then processed asynchronously by the
//     OutboxProcessor.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** inheriting from **BaseDbContext**:
//    - `BaseDbContext` (from BuildingBlocks.Infrastructure) provides
//      the outbox pattern, automatic configuration discovery, and
//      domain event dequeuing.
//
// 2. **DbSet<TEntity>** properties:
//    - Each `DbSet<T>` represents a database table.  EF Core uses
//      these to create migrations and generate SQL queries.
//    - Only aggregate roots are exposed as DbSets.  Child entities
//      (Session, TrustedDevice, etc.) are mapped as owned types via
//      configuration, not as top‑level DbSets.
//
// 3. **Constructor** accepting **DbContextOptions<T>**:
//    - The options are configured by the module's DI registration
//      (e.g., `AddDbContext<IdentityDbContext>(options => ...)`).
//    - They specify the database provider (PostgreSQL), connection
//      string, and any provider‑specific settings.
//
// 4. **OnModelCreating**:
//    - Overridden to apply entity configurations from this
//      assembly.  `ApplyConfigurationsFromAssembly` scans for all
//      `IEntityTypeConfiguration<T>` implementations and applies
//      them automatically.
//    - Also sets the default schema to "identity".
//
// 5. **sealed** modifier:
//    - Prevents further inheritance.  The Identity module's
//      persistence should not be extended by other modules.
// ====================================================================

using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the Identity module.
/// </summary>
public sealed class IdentityDbContext : BaseDbContext, IUnitOfWork
{
    /// <summary>
    /// The Users table – stores registered users and their child
    /// entities (Sessions, TrustedDevices, RefreshTokens).
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// The Homes table – stores tenants and their memberships.
    /// </summary>
    public DbSet<Home> Homes => Set<Home>();

    /// <summary>
    /// Initialises the context with the given options.
    /// </summary>
    /// <param name="options">
    /// The EF Core options, typically configured with
    /// <c>UseNpgsql(connectionString)</c> and retry policies.
    /// </param>
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configures the entity models for the Identity module.
    /// Scans this assembly for <see cref="IEntityTypeConfiguration{T}"/>
    /// implementations and applies them automatically.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The base class calls ApplyConfigurationsFromAssembly for
        // the derived context's assembly, which discovers all
        // IEntityTypeConfiguration<T> classes in this project
        // (UserConfiguration, HomeConfiguration, etc.).
        base.OnModelCreating(modelBuilder);

        // Set the default schema for all tables in this context.
        modelBuilder.HasDefaultSchema("identity");
    }
}



//Dry‑run — how the DbContext is used in a repository and what happens during SaveChanges:
// ====================================================================
// REGISTRATION (in Program.cs or module DI):
// ====================================================================
//services.AddDbContext<IdentityDbContext>(options =>
//    options.UseNpgsql(connectionString)
//           .EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null));


// ====================================================================
// REPOSITORY USAGE (UserRepository):
// ====================================================================
//public sealed class UserRepository : IUserRepository
//{
//    private readonly IdentityDbContext _context;

//    public UserRepository(IdentityDbContext context)
//    {
//        _context = context;
//    }

//    public async Task<User?> GetByIdAsync(Ulid id, CancellationToken ct)
//    {
//        return await _context.Users
//            .FirstOrDefaultAsync(u => u.Id == id, ct);
//        // SQL: SELECT * FROM identity."Users" WHERE "Id" = @id LIMIT 1
//    }

//    public async Task AddAsync(User user, CancellationToken ct)
//    {
//        await _context.Users.AddAsync(user, ct);
//        // Tracks the user in the "Added" state.  No SQL yet.
//    }
//}


// ====================================================================
// WHAT HAPPENS DURING SaveChangesAsync:
// ====================================================================
// The handler calls:
//   await _unitOfWork.SaveChangesAsync(ct);
//
// Which delegates to IdentityDbContext.SaveChangesAsync() →
// BaseDbContext.SaveChangesAsync():
//
// Step 1: Collect domain events
//   - Scans ChangeTracker for Entity instances that implement
//     IAggregateRoot and have pending domain events.
//   - Finds User aggregate (has UserRegistered event).
//   - Finds Home aggregate (has HomeCreated event).
//
// Step 2: Dequeue and serialise
//   - Dequeues events from each aggregate (calls DequeueDomainEvents).
//   - Serialises each event to JSON.
//   - Creates OutboxMessage objects with EventType, EventPayload,
//     and OccurredOnUtc.
//
// Step 3: Add outbox messages to the transaction
//   - OutboxMessages.AddRange(outboxMessages);
//   - Now the outbox rows will be INSERTed in the same transaction
//     as the aggregate changes.
//
// Step 4: Commit transaction
//   - base.SaveChangesAsync() executes:
//     BEGIN TRANSACTION
//     INSERT INTO identity."Users" (...) VALUES (...)
//     INSERT INTO identity."Homes" (...) VALUES (...)
//     INSERT INTO identity."HomeMemberships" (...) VALUES (...)
//     INSERT INTO "OutboxMessages" (...) VALUES (...)  -- UserRegistered
//     INSERT INTO "OutboxMessages" (...) VALUES (...)  -- HomeCreated
//     COMMIT TRANSACTION
//
// If the database is unavailable, the entire transaction is rolled
// back – no partial data, no lost events.
