// ====================================================================
// VERIXORA – SharedKernel.Application / Abstractions / IQuery.cs
// ====================================================================
// Summary:
//   Marker interface for all CQRS queries.
//   A query represents a read operation that returns data without
//   changing system state.  Every query has exactly one handler
//   that implements IQueryHandler<TQuery, TResponse>.
//
//   Why separate from ICommand:
//     - Queries are side‑effect‑free (CQS principle).
//     - MediatR pipeline applies different behaviours to queries
//       (e.g., caching, read‑only connection) vs. commands
//       (e.g., unit‑of‑work, idempotency).
//     - Architecture tests can enforce that query handlers never
//       modify domain state.
//
//   Usage:
//     public record GetDeviceByIdQuery(Ulid DeviceId)
//         : IQuery<DeviceDto?>;
//
//   Design note:
//     IQuery<TResponse> carries the return type, making the
//     contract explicit.  A query handler signature is:
//     IQueryHandler<TQuery, TResponse>.
// ====================================================================

namespace SharedKernel.Application.Abstractions;

/// <summary>
/// Marker interface for queries (read operations) with a specific response type.
/// </summary>
/// <typeparam name="TResponse">The type of data returned by the query.</typeparam>
public interface IQuery<TResponse>
{
}
