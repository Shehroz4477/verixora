// ====================================================================
// VERIXORA – Identity.Infrastructure / Services / SystemClock.cs
// ====================================================================
//
// ARCHITECTURAL ROLE:
//   Implements <see cref="IClock"/> by returning the real system
//   UTC time.  This is the production implementation registered in
//   the DI container as a Singleton.  In tests, it is replaced by
//   a FakeClock that returns a fixed, predetermined time, making
//   all time‑dependent code deterministic.
//
//   WHY AN ABSTRACTION:
//     - Direct calls to <see cref="DateTime.UtcNow"/> are non‑
//       deterministic and make unit tests flaky.
//     - Wrapping the system clock behind <see cref="IClock"/> allows
//       the application to control time in tests while using the
//       real clock in production.
//     - All command handlers and domain entities that need the
//       current time depend on <see cref="IClock"/>, never on
//       <see cref="DateTime"/> directly.
//
//   REGISTRATION:
//     services.AddSingleton<IClock, SystemClock>();
//
//   THREAD‑SAFETY:
//     <see cref="DateTime.UtcNow"/> is a static property that reads
//     the system clock.  It is thread‑safe by definition.
//
// --------------------------------------------------------------------
// C# CONCEPTS USED IN THIS FILE:
//
// 1. **class** implementing an interface:
//    - `SystemClock : IClock` guarantees the `UtcNow` property is
//      provided.
//
// 2. **DateTime** (value type):
//    - Represents a point in time.  `DateTime.UtcNow` returns the
//      current UTC time, independent of the server's local time
//      zone.  Always use UTC to avoid time‑zone ambiguity.
//
// 3. **sealed** modifier:
//    - Prevents inheritance.  There is no reason to extend this
//      simple implementation.
//
// 4. **Expression‑bodied property** (`=>`):
//    - A concise syntax for a read‑only property that returns a
//      single expression.  `public DateTime UtcNow => DateTime.UtcNow;`
//      is equivalent to `public DateTime UtcNow { get { return DateTime.UtcNow; } }`.
// ====================================================================

using Identity.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Returns the real system UTC time.  Registered as a Singleton.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}


//Dry‑run — how the clock is used in production and tests:
// =========================================================================
// PRODUCTION USAGE (registered as Singleton in DI):
// =========================================================================
// In Program.cs:
//   services.AddSingleton<IClock, SystemClock>();
//
// In a command handler:
//   var now = _clock.UtcNow;  // returns the real current UTC time
//
// Example: user registration
//   var user = User.Register(email, passwordHash, _clock.UtcNow);
//   → user.CreatedAt = 2026‑06‑07 14:30:00 UTC (real time)


// =========================================================================
// TEST USAGE (injected FakeClock with a fixed time):
// =========================================================================
// In a unit test:
//   var fixedTime = new DateTime(2026, 6, 7, 10, 0, 0, DateTimeKind.Utc);
//   var clock = new FakeClock(fixedTime);
//   var handler = new RegisterUserHandler(..., clock);
//
//   var result = await handler.Handle(command, ct);
//
//   // Assert: the user's CreatedAt is exactly the fixed time.
//   Assert.Equal(fixedTime, result.Value.CreatedAt);
//   // ✅ This test passes every time, regardless of when it runs.


// =========================================================================
// FakeClock (used in tests):
// =========================================================================
// public sealed class FakeClock : IClock
// {
//     private readonly DateTime _fixedTime;
//     public FakeClock(DateTime fixedTime) => _fixedTime = fixedTime;
//     public DateTime UtcNow => _fixedTime;
// }
