// ====================================================================
// VERIXORA – SharedKernel.Domain / Results / Unit.cs
// ====================================================================
// Summary:
//   Represents a void return type in functional patterns.
//   Used with <see cref="Result{T}"/> when a CQRS handler or
//   pipeline step has no meaningful return value on success.
//
//   Why:
//     - The unlock pipeline steps return <c>Result&lt;Unit&gt;</c>
//       to signal pass/fail without returning a value.
//     - Avoids using <c>Result&lt;object&gt;</c> or special
//       void‑handling overloads.
//
//   Usage:
//     return Result&lt;Unit&gt;.Success(Unit.Value);
// ====================================================================

using System.Diagnostics;

namespace SharedKernel.Domain.Results;

/// <summary>
/// A singleton‑like type representing the absence of a value.
/// </summary>
[DebuggerDisplay("()")]
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// The only instance of <see cref="Unit"/>.
    /// </summary>
    public static readonly Unit Value = new();

    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;
    public override int GetHashCode() => 0;
    public override string ToString() => "()";

    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
}
