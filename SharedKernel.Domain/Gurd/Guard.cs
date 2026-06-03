// ====================================================================
// VERIXORA – SharedKernel.Domain / Guard / Guard.cs
// ====================================================================
// Summary:
//   A static utility class for precondition checks.  Every method
//   follows the "fail‑fast" principle – it throws an exception
//   immediately if the check fails.  This keeps repetitive
//   validation code out of domain objects and ensures error
//   messages are consistent across all 13 modules.
//
//   Why a static class:
//     - No state – purely procedural helpers.
//     - Callable from any domain constructor, method, or
//       application service without dependency injection.
//     - Zero external dependencies (pure BCL + SharedKernel).
//
//   Future consideration:
//     At large scale, the class can be split into specialised
//     partial classes: Guard.String, Guard.Numeric, Guard.Time,
//     Guard.Collections, Guard.Domain.  This is not required
//     for the current module count but improves discoverability.
//
//   Testability:
//     The Guard.SetClock() / ResetClock() methods provide a
//     thread‑safe way to fix the clock in unit tests.
//
//   Usage example in a domain entity:
//     public Device(DeviceName name, int batteryPercent)
//     {
//         Guard.AgainstNull(name, nameof(name));
//         Guard.AgainstOutOfRange(batteryPercent, 0, 100, nameof(batteryPercent));
//         Name = name;
//         BatteryPercent = batteryPercent;
//     }
// ====================================================================

using System.Collections;
using System.Collections.Generic;
using SharedKernel.Domain.Base;

namespace SharedKernel.Domain.Guard;

public static class Guard
{
    // ---- Pluggable clock ----
    private static readonly object _clockLock = new();
    private static Func<DateTimeOffset> _utcNow = () => DateTimeOffset.UtcNow;

    /// <summary>
    /// Retrieves the current UTC time, respecting any test override.
    /// </summary>
    public static DateTimeOffset UtcNow
    {
        get
        {
            lock (_clockLock)
                return _utcNow();
        }
    }

    /// <summary>
    /// Overrides the clock for testing.  Throws if <paramref name="clock"/> is null.
    /// </summary>
    public static void SetClock(Func<DateTimeOffset> clock)
    {
        if (clock is null)
            throw new ArgumentNullException(nameof(clock));

        lock (_clockLock)
            _utcNow = clock;
    }

    /// <summary>
    /// Restores the system UTC clock.
    /// </summary>
    public static void ResetClock()
    {
        lock (_clockLock)
            _utcNow = () => DateTimeOffset.UtcNow;
    }

    // ----------------------------------------------------------------
    // String guards
    // ----------------------------------------------------------------

    public static void AgainstNullOrWhiteSpace(string? value, string parameterName)
    {
        if (value is null)
            throw new ArgumentNullException(parameterName);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                $"'{parameterName}' cannot be empty or whitespace.",
                parameterName);
    }

    // ----------------------------------------------------------------
    // Numeric guards
    // ----------------------------------------------------------------

    public static void AgainstNegativeOrZero(int value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentException(
                $"'{parameterName}' must be > 0. Actual: {value}.",
                parameterName);
    }

    public static void AgainstOutOfRange(int value, int min, int max, string parameterName)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"'{parameterName}' must be between {min} and {max} inclusive. Actual: {value}.");
    }

    // ----------------------------------------------------------------
    // Null guards (reference types)
    // ----------------------------------------------------------------
    public static void AgainstNull(object? value, string parameterName)
    {
        if (value is null)
            throw new ArgumentNullException(parameterName);
    }

    public static void AgainstNull<T>(T? value, string parameterName) where T : class
    {
        if (value is null)
            throw new ArgumentNullException(parameterName);
    }

    // ----------------------------------------------------------------
    // Default value guard (value types)
    // ----------------------------------------------------------------
    public static void AgainstDefault<T>(T value, string parameterName)
        where T : struct, IEquatable<T>
    {
        if (EqualityComparer<T>.Default.Equals(value, default))
            throw new ArgumentException(
                $"'{parameterName}' cannot be the default value of {typeof(T).Name}.",
                parameterName);
    }

    // ----------------------------------------------------------------
    // Enum validity guard
    // ----------------------------------------------------------------
    public static void AgainstInvalidEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : Enumeration
    {
        if (value is null)
            throw new ArgumentNullException(parameterName);

        var all = Enumeration.GetAll<TEnum>();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Id == value.Id && all[i].GetType() == value.GetType())
                return;
        }

        throw new ArgumentOutOfRangeException(
            parameterName,
            $"'{parameterName}' is not a valid {typeof(TEnum).Name}. Actual: {value}.");
    }

    // ----------------------------------------------------------------
    // Collection guard
    // ----------------------------------------------------------------
    public static void AgainstNullOrEmptyCollection(IEnumerable? collection, string parameterName)
    {
        if (collection is null)
            throw new ArgumentNullException(parameterName);

        IEnumerator enumerator = collection.GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
                throw new ArgumentException(
                    $"'{parameterName}' must contain at least one element.",
                    parameterName);
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    // ----------------------------------------------------------------
    // Temporal guards (with tolerance for IoT clock skew)
    // ----------------------------------------------------------------
    public static void AgainstFutureDate(
        DateTimeOffset value,
        string parameterName,
        int toleranceSeconds = 0)
    {
        var now = UtcNow;
        var upper = now.AddSeconds(toleranceSeconds);

        if (value > upper)
            throw new ArgumentException(
                $"'{parameterName}' cannot be in the future. Actual: {value}, now: {now}.",
                parameterName);
    }

    public static void AgainstPastDate(
        DateTimeOffset value,
        string parameterName,
        int toleranceSeconds = 0)
    {
        var now = UtcNow;
        var lower = now.AddSeconds(-toleranceSeconds);

        if (value < lower)
            throw new ArgumentException(
                $"'{parameterName}' cannot be in the past. Actual: {value}, now: {now}.",
                parameterName);
    }
}
