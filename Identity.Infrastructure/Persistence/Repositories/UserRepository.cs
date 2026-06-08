// ====================================================================
// VERIXORA – Identity.Infrastructure / Persistence / Repositories / UserRepository.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements <see cref="IUserRepository"/> using EF Core and the
//   Identity module's <see cref="IdentityDbContext"/>.  It translates
//   application‑level calls into database queries, following the
//   Repository pattern.
//
//   WHY A SEPARATE REPOSITORY IMPLEMENTATION:
//     - Keeps the Application layer (interfaces) decoupled from EF
//       Core specifics.
//     - Centralises all User persistence logic in one place.
//     - Makes it easy to swap EF Core for a different ORM or a
//       micro‑ORM like Dapper later.
//
//   EMAIL LOOKUP STRATEGY:
//     - The Email column is encrypted with a random IV, so we cannot
//       query it directly for equality.
//     - Instead, we use the **EmailHash** column, which is a SHA‑256
//       hash of the normalised email.  We compute the hash from the
//       caller‑supplied email and query the hash column.
//     - This gives fast, deterministic lookups while keeping the
//       email itself encrypted at rest.
//
//   UNIT OF WORK:
//     - This repository does NOT call SaveChanges.  It only tracks
//       entities (via AddAsync) or queries them.  The actual commit
//       happens through <see cref="IUnitOfWork.SaveChangesAsync"/>,
//       which delegates to <see cref="IdentityDbContext.SaveChangesAsync"/>
//       (and in turn to <see cref="BaseDbContext.SaveChangesAsync"/>
//       for outbox processing).
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** implementing an interface:
//    - `UserRepository : IUserRepository` guarantees it provides all
//      methods declared in the interface.
//
// 2. **Constructor injection**:
//    - The `IdentityDbContext` is injected and stored in a readonly
//      field.  This makes the repository testable with an in‑memory
//      database or mock.
//
// 3. **private readonly** field:
//    - `_context` cannot be changed after construction, preventing
//      accidental reassignment.
//
// 4. **async / await**:
//    - All database operations are asynchronous to avoid blocking
//      threads.
//
// 5. **CancellationToken**:
//    - Propagated to all EF Core calls so that the operation can be
//      cancelled if the request times out or the server shuts down.
//
// 6. **FirstOrDefaultAsync** / **AnyAsync**:
//    - `FirstOrDefaultAsync` returns the first matching entity or
//      null.  Used for GetByEmail and GetById.
//    - `AnyAsync` returns true if at least one matching row exists.
//      Used for ExistsByEmail — more efficient than loading the full
//      entity.
//
// 7. **AddAsync**:
//    - Tracks a new entity for insertion.  No SQL is executed until
//      SaveChanges is called.
//
// 8. **SHA256.HashData** + **Convert.ToBase64String**:
//    - Computes the EmailHash the same way the User entity does.
//      This ensures the lookup hash matches the stored hash.
//
// 9. **sealed** modifier:
//    - Prevents inheritance.  Repository implementations should not
//      be extended by other classes.
// ====================================================================

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    /// <summary>
    /// Initialises the repository with the Identity database context.
    /// </summary>
    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Ulid id, CancellationToken ct = default)
    {
        // Query the Users table by primary key.
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        // Compute the hash of the normalised email.
        var emailHash = ComputeEmailHash(email);

        // Query by the EmailHash column, which is indexed and unique.
        return await _context.Users
            .FirstOrDefaultAsync(u => u.EmailHash == emailHash, ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        var emailHash = ComputeEmailHash(email);

        // Efficient existence check – does not load the full entity.
        return await _context.Users
            .AnyAsync(u => u.EmailHash == emailHash, ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        // Tracks the user for insertion.  The actual INSERT happens
        // when SaveChanges is called (via IUnitOfWork).
        await _context.Users.AddAsync(user, ct);
    }

    // ----------------------------------------------------------------
    // Private helpers
    // ----------------------------------------------------------------

    /// <summary>
    /// Computes the SHA‑256 hash of the normalised email.
    /// Must match the algorithm used in <see cref="User.Register"/>.
    /// </summary>
    private static string ComputeEmailHash(string email)
    {
        var bytes = Encoding.UTF8.GetBytes(email);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

//Dry‑run — how the repository methods translate to SQL:
// =========================================================================
// 1. GetByIdAsync
// =========================================================================
//var user = await repo.GetByIdAsync(someUlid, ct);

// Generated SQL:
// SELECT u."Id", u."Email", u."EmailHash", u."PasswordHash", ...
// FROM identity."Users" AS u
// WHERE u."Id" = @id
// LIMIT 1


// =========================================================================
// 2. GetByEmailAsync
// =========================================================================
//var user = await repo.GetByEmailAsync("alice@example.com", ct);

// 1. C# computes: emailHash = Base64(SHA256("alice@example.com"))
//    → "abc123base64hash..."
// 2. EF Core generates:
// SELECT u."Id", u."Email", u."EmailHash", ...
// FROM identity."Users" AS u
// WHERE u."EmailHash" = 'abc123base64hash...'
// LIMIT 1


// =========================================================================
// 3. ExistsByEmailAsync
// =========================================================================
//bool exists = await repo.ExistsByEmailAsync("alice@example.com", ct);

// Generated SQL:
// SELECT EXISTS (
//   SELECT 1
//   FROM identity."Users" AS u
//   WHERE u."EmailHash" = 'abc123base64hash...'
// )


// =========================================================================
// 4. AddAsync
// =========================================================================
//await repo.AddAsync(newUser, ct);

// EF Core tracks the new User entity in the "Added" state.
// No SQL is executed yet.
// Later, when IUnitOfWork.SaveChangesAsync() is called:
// INSERT INTO identity."Users" ("Id", "Email", "EmailHash", ...)
// VALUES (@id, @email, @emailHash, ...)



//How the repository fits into the command handler flow:
//RegisterUserHandler
//    ↓
//_users.ExistsByEmailAsync(email)
//    → queries EmailHash column
//    ↓ (false)
//_users.AddAsync(user)
//    → tracks user for insertion
//    ↓
//_unitOfWork.SaveChangesAsync()
//    → INSERT User, INSERT Home, INSERT OutboxMessages
//    → all in one transaction
