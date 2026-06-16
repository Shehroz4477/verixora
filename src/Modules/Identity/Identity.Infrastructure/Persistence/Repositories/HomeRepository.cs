// ====================================================================
// VERIXORA – Identity.Infrastructure / Persistence / Repositories / HomeRepository.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements <see cref="IHomeRepository"/> using EF Core and the
//   Identity module's <see cref="IdentityDbContext"/>.  It translates
//   application‑level calls into database queries, following the
//   Repository pattern.
//
//   WHY A SEPARATE REPOSITORY IMPLEMENTATION:
//     - Keeps the Application layer (interfaces) decoupled from EF
//       Core specifics.
//     - Centralises all Home persistence logic in one place.
//     - Makes it easy to swap EF Core for a different ORM or a
//       micro‑ORM like Dapper later.
//
//   UNIT OF WORK:
//     - This repository does NOT call SaveChanges.  It only tracks
//       entities (via AddAsync) or queries them.  The actual commit
//       happens through <see cref="IUnitOfWork.SaveChangesAsync"/>,
//       which delegates to <see cref="IdentityDbContext.SaveChangesAsync"/>
//       (and in turn to <see cref="BaseDbContext.SaveChangesAsync"/>
//       for outbox processing).
//
//   MEMBERSHIP QUERY:
//     - <see cref="GetByMemberIdAsync"/> loads all Homes where the
//       specified user has a membership.  This is used for the
//       user's dashboard and authorisation checks.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** implementing an interface:
//    - `HomeRepository : IHomeRepository` guarantees it provides all
//      methods declared in the interface.
//
// 2. **Constructor injection**:
//    - The `IdentityDbContext` is injected and stored in a readonly
//      field.  This makes the repository testable.
//
// 3. **private readonly** field:
//    - `_context` cannot be changed after construction.
//
// 4. **async / await**:
//    - All database operations are asynchronous to avoid blocking
//      threads.
//
// 5. **CancellationToken**:
//    - Propagated to all EF Core calls.
//
// 6. **FirstOrDefaultAsync** / **ToListAsync**:
//    - `FirstOrDefaultAsync` returns the first matching entity or
//      null.  Used for GetById.
//    - `ToListAsync` materialises the query into a list.
//
// 7. **AddAsync**:
//    - Tracks a new entity for insertion.  No SQL is executed until
//      SaveChanges is called.
//
// 8. **sealed** modifier:
//    - Prevents inheritance.
// ====================================================================

using System.Collections.Generic;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IHomeRepository"/>.
/// </summary>
public sealed class HomeRepository : IHomeRepository
{
    private readonly IdentityDbContext _context;

    /// <summary>
    /// Initialises the repository with the Identity database context.
    /// </summary>
    public HomeRepository(IdentityDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Home?> GetByIdAsync(Ulid id, CancellationToken ct = default)
    {
        // Query the Homes table by primary key.
        // Include memberships so that the aggregate is fully loaded.
        return await _context.Homes
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Home>> GetByMemberIdAsync(
        Ulid userId, CancellationToken ct = default)
    {
        // Find all Homes where the user has a membership record.
        // This loads the full aggregate (including memberships)
        // so the caller can inspect roles and permissions.
        return await _context.Homes
            .Include(h => h.Members)
            .Where(h => h.Members.Any(m => m.UserId == userId))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(Home home, CancellationToken ct = default)
    {
        // Tracks the Home for insertion.  The actual INSERT happens
        // when SaveChanges is called (via IUnitOfWork).
        await _context.Homes.AddAsync(home, ct);
    }
}



//Dry‑run — how the repository methods translate to SQL:
// =========================================================================
// 1. GetByIdAsync
// =========================================================================
//var home = await repo.GetByIdAsync(homeId, ct);

// Generated SQL:
// SELECT h."Id", h."Name", h."MaxDevices", h."CreatedAt",
//        m."Id", m."HomeId", m."UserId", m."Role", m."JoinedAt"
// FROM identity."Homes" AS h
// LEFT JOIN identity."HomeMemberships" AS m ON h."Id" = m."HomeId"
// WHERE h."Id" = @id
// LIMIT 1


// =========================================================================
// 2. GetByMemberIdAsync
// =========================================================================
//var homes = await repo.GetByMemberIdAsync(userId, ct);

// Generated SQL:
// SELECT h."Id", h."Name", h."MaxDevices", h."CreatedAt",
//        m."Id", m."HomeId", m."UserId", m."Role", m."JoinedAt"
// FROM identity."Homes" AS h
// INNER JOIN identity."HomeMemberships" AS m ON h."Id" = m."HomeId"
// WHERE m."UserId" = @userId


// =========================================================================
// 3. AddAsync
// =========================================================================
//await repo.AddAsync(newHome, ct);

// EF Core tracks the new Home entity in the "Added" state.
// No SQL is executed yet.
// Later, when IUnitOfWork.SaveChangesAsync() is called:
// INSERT INTO identity."Homes" ("Id", "Name", "MaxDevices", "CreatedAt")
// VALUES (@id, @name, @maxDevices, @createdAt);
// INSERT INTO identity."HomeMemberships" (...) VALUES (...);

//How the repository fits into the command handler flow:
//RegisterUserHandler
//    ↓
//_homes.AddAsync(home)
//    → tracks Home for insertion
//    ↓
//_unitOfWork.SaveChangesAsync()
//    → INSERT Home + INSERT HomeMembership + INSERT OutboxMessages
//    → all in one transaction
