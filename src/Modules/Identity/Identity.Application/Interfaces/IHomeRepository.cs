// ====================================================================
// VERIXORA – Identity.Application / Interfaces / IHomeRepository.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Defines the contract for persisting and retrieving Home
//   (tenant) aggregates.  The interface lives in the Application
//   layer and is implemented in the Infrastructure layer (EF Core),
//   following the Dependency Inversion Principle.
//
//   WHY A REPOSITORY INTERFACE:
//     - Decouples command/query handlers from EF Core.
//     - Enables unit testing by mocking the repository.
//     - Allows swapping persistence technology without touching
//       application logic.
//
//   WHY ONLY AGGREGATE ROOTS:
//     - Only the Home aggregate root is persisted directly.
//     - Child entities (HomeMembership) are persisted as part of
//       the Home aggregate via EF Core cascade.
//
//   WHY NO UpdateAsync():
//     - EF Core's change tracker automatically detects changes to
//       tracked entities.  Loading a Home via GetByIdAsync(), calling
//       methods like AddMember() or ChangeRole(), and then calling
//       IUnitOfWork.SaveChangesAsync() is sufficient — no explicit
//       "update" call is needed.  This keeps the repository minimal
//       and consistent with the Unit of Work pattern.
//
//   METHODS:
//     - GetByIdAsync       – fetch a Home by its ULID
//     - GetByMemberIdAsync – fetch all Homes a user belongs to
//     - AddAsync           – track a new Home for insertion
//
//   FUTURE EVOLUTION:
//     GetByMemberIdAsync currently returns full aggregates.  If a
//     user belongs to many Homes, this can evolve into a paged
//     query or a separate read‑side IHomeQueries returning DTOs.
//
//   CONSISTENCY:
//     Both IUserRepository and IHomeRepository follow the same
//     minimal pattern: GetById, GetBy*, Add, and an optional
//     existence check — no UpdateAsync().
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **interface** keyword:
//    - Defines a contract without implementation.  Any implementing
//      class MUST provide all declared methods.
//
// 2. **public** access modifier:
//    - The interface is accessible from any project that references
//      this assembly.
//
// 3. **Task<T>** return types:
//    - `Task<Home?>` – asynchronous operation returning a Home or null.
//    - `Task<IReadOnlyCollection<Home>>` – asynchronous operation
//      returning a read‑only collection.
//    - `Task` – asynchronous operation with no return value.
//
// 4. **IReadOnlyCollection<Home>**:
//    - A read‑only view of a collection.  The caller can enumerate
//      and count, but cannot modify the collection.
//
// 5. **Ulid** parameter type:
//    - Strong typing prevents accidentally passing a wrong type of
//      identifier.
//
// 6. **CancellationToken** parameter:
//    - Allows the caller to cancel the database operation if the
//      request times out or the server shuts down.
//
// 7. **Home?** (nullable reference type):
//    - The `?` indicates the method may return null if no Home
//      is found.  The compiler enforces null checks.
//
// 8. **namespace** declaration:
//    - `Identity.Application.Interfaces` indicates this is an
//      application‑layer abstraction for dependency inversion.
// ====================================================================

using System.Numerics;
using Identity.Domain.Entities;
using SharedKernel.Domain.Base;

namespace Identity.Application.Interfaces;

/// <summary>
/// Contract for persisting and retrieving <see cref="Home"/> aggregates.
/// </summary>
public interface IHomeRepository
{
    /// <summary>
    /// Retrieves a Home by its unique identifier, including all
    /// memberships.
    /// </summary>
    /// <param name="id">The Home's ULID.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The Home, or <c>null</c> if not found.</returns>
    Task<Home?> GetByIdAsync(Ulid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all Homes that the specified user is a member of.
    /// For MVP, returns the full aggregates.  Future versions may
    /// switch to a paged or DTO‑based query for performance.
    /// </summary>
    /// <param name="userId">The user's ULID.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A read‑only collection of Homes.</returns>
    Task<IReadOnlyCollection<Home>> GetByMemberIdAsync(
        Ulid userId, CancellationToken ct = default);

    /// <summary>
    /// Tracks a new Home for insertion.  The actual database commit
    ///  happens when <c>SaveChangesAsync</c> is called.
    /// </summary>
    /// <param name="home">The Home aggregate to insert.</param>
    /// <param name="ct">A cancellation token.</param>
    Task AddAsync(Home home, CancellationToken ct = default);
}

//Dry‑run — complete repository usage pattern:
// =========================================================================
// 1. CREATE (during user registration)
// =========================================================================
//await _homes.AddAsync(home, ct);
//    await _unitOfWork.SaveChangesAsync(ct);
//    // SQL: INSERT INTO Homes + INSERT INTO HomeMemberships
//    // Both committed atomically.

//    // =========================================================================
//    // 2. READ – single Home
//    // =========================================================================
//    var home = await _homes.GetByIdAsync(homeId, ct);
//    // SQL: SELECT * FROM Homes WHERE Id = @id
//    // Returns Home with Memberships loaded, or null.

//    // =========================================================================
//    // 3. READ – all Homes for a user
//    // =========================================================================
//    var homes = await _homes.GetByMemberIdAsync(userId, ct);
//    // SQL: SELECT * FROM Homes h
//    //      JOIN HomeMemberships hm ON h.Id = hm.HomeId
//    //      WHERE hm.UserId = @userId
//    // Returns IReadOnlyCollection<Home>.

//    // =========================================================================
//    // 4. UPDATE – add a member (tracked entity, no UpdateAsync needed)
//    // =========================================================================
//    var home = await _homes.GetByIdAsync(homeId, ct);
//home.AddMember(newUserId, HomeRole.Member, now);
//await _unitOfWork.SaveChangesAsync(ct);
//// EF Core detects the change and generates the UPDATE.
