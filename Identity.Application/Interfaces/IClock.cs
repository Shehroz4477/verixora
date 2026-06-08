// ====================================================================
// VERIXORA – Identity.Application / Interfaces / IClock.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Provides an abstraction over the system clock so that time‑
//   dependent code can be made deterministic and testable.
//
//   WHY THIS INTERFACE EXISTS:
//     - Without it, every handler, domain entity, and service
//       would call `DateTime.UtcNow` directly.  That makes unit
//       tests non‑deterministic — the same test run at different
//       times could produce different results.
//     - With this interface, you can inject a fake clock in
//       tests that returns a fixed time.  The test always sees
//       the same "now", making assertions predictable.
//     - In production, a simple implementation returns the real
//       system time.
//
//   USAGE:
//     Instead of:   var now = DateTime.UtcNow;
//     Use:          var now = _clock.UtcNow;
//
//   WHERE IT'S USED:
//     - Command handlers (e.g., RegisterUserHandler passes the
//       current time when creating User and Home aggregates).
//     - Domain entities that need a timestamp at creation time
//       (Session, TrustedDevice, RefreshToken — all of which
//       accept `DateTime utcNow` as a constructor parameter).
//     - Any service that needs to know "what time is it now?"
//
//   WHY UTC:
//     - Always use UTC to avoid time‑zone ambiguity across
//       different servers, clients, and geographic regions.
//     - The property is named `UtcNow` to make this explicit.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **interface** keyword:
//    - Defines a contract without implementation.  Any class that
//      implements this interface MUST provide the `UtcNow` property.
//    - Enables the Dependency Inversion Principle: high‑level
//      modules (handlers) depend on this abstraction, not on the
//      concrete system clock.
//
// 2. **public** access modifier:
//    - The interface is accessible from any project that references
//      this assembly.
//
// 3. **DateTime** property:
//    - `DateTime` is a value type (struct) representing a point in
//      time.  It is always UTC — never `DateTime.Now` (local time).
//    - A property with only a getter is read‑only.  The caller can
//      read it but cannot set it.
//
// 4. **namespace** declaration:
//    - `Identity.Application.Interfaces` places this in the
//      Application layer's abstraction space.
//
// 5. **Simplicity**:
//    - This interface intentionally has only ONE member.  It does
//      not need methods like `AddDays()` or `ToUnixTimeMilliseconds()`
//      because those can be called on the returned `DateTime` value.
// ====================================================================

using Identity.Application.Commands.RegisterUser;
using Identity.Domain.Entities;
using SharedKernel.Domain.Results;

namespace Identity.Application.Interfaces;

/// <summary>
/// Provides the current UTC time, abstracted for testability.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// In production, this returns <see cref="DateTime.UtcNow"/>.
    /// In tests, it returns a fixed, predetermined value.
    /// </summary>
    DateTime UtcNow { get; }
}


////Dry‑run — how the clock is used in production and in tests:
//// ====================================================================
//// PRODUCTION IMPLEMENTATION (in Infrastructure layer)
//// ====================================================================
//public sealed class SystemClock : IClock
//{
//    // Simply delegates to the real system clock.
//    public DateTime UtcNow => DateTime.UtcNow;
//}

//// Registered as a Singleton in DI:
//// services.AddSingleton<IClock, SystemClock>();


//// ====================================================================
//// TEST IMPLEMENTATION (used in unit tests)
//// ====================================================================
//public sealed class FakeClock : IClock
//{
//    private readonly DateTime _fixedTime;

//    public FakeClock(DateTime fixedTime)
//    {
//        _fixedTime = fixedTime;
//    }

//    // Always returns the same fixed time.
//    public DateTime UtcNow => _fixedTime;
//}


//// ====================================================================
//// HOW IT'S USED IN A HANDLER:
//// ====================================================================
//public async Task<Result<RegisterUserResponse>> Handle(
//    RegisterUserCommand command, CancellationToken ct)
//    {
//        // Instead of DateTime.UtcNow (non‑deterministic):
//        var now = _clock.UtcNow;  // ← injected IClock

//        // The rest of the handler uses 'now' for all time‑dependent
//        // operations:
//        var user = User.Register(command.Email, passwordHash, now);
//        var home = Home.Create("My Home", user.Id, now);
//        // ...
//    }


//    // ====================================================================
//    // WHY THIS MATTERS — TEST EXAMPLE:
//    // ====================================================================
//    [Fact]
//    public async Task RegisterUser_ShouldSetCreatedAtToNow()
//    {
//        // Arrange: fix the clock to a known time.
//        var fixedTime = new DateTime(2026, 6, 7, 10, 0, 0, DateTimeKind.Utc);
//        var clock = new FakeClock(fixedTime);

//        var handler = new RegisterUserHandler(
//            mockUsers, mockHomes, mockHasher, clock, mockUow);

//        var command = new RegisterUserCommand("test@example.com", "password");

//        // Act:
//        var result = await handler.Handle(command, CancellationToken.None);

//        // Assert: the user's CreatedAt is exactly the fixed time.
//        var response = result.Value;
//        var user = await mockUsers.GetByIdAsync(response.UserId);
//        Assert.Equal(fixedTime, user.CreatedAt);
//        // ✅ This test passes every time, regardless of when it runs.
//    }
