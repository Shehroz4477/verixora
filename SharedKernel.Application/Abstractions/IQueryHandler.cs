// ====================================================================
// VERIXORA – SharedKernel.Application / Abstractions / IQueryHandler.cs
// ====================================================================
// Summary:
//   Contract for a handler that processes a single type of query.
//   Queries are side‑effect‑free read operations.  Every query has
//   exactly one handler that returns a Result<TResponse>.
//
//   Why not use MediatR's IRequestHandler directly everywhere:
//     - Our abstraction guarantees that queries return Result<T>,
//       enforcing the functional error‑handling pattern.
//     - Architecture tests can verify that every query handler
//       implements this interface, not just IRequestHandler.
//     - Future changes (e.g., adding caching metadata) can be
//       centralised here without modifying 13 modules.
//
//   Usage:
//     public class GetDeviceByIdHandler
//         : IQueryHandler<GetDeviceByIdQuery, DeviceDto?>
//     {
//         public async Task<Result<DeviceDto?>> Handle(
//             GetDeviceByIdQuery query,
//             CancellationToken cancellationToken)
//         { ... }
//     }
// ====================================================================

namespace SharedKernel.Application.Abstractions;

/// <summary>
/// Handles a query of type <typeparamref name="TQuery"/> and returns
/// a result containing <typeparamref name="TResponse"/> on success.
/// </summary>
/// <typeparam name="TQuery">The query type (must implement <see cref="IQuery{TResponse}"/>).</typeparam>
/// <typeparam name="TResponse">The type of data returned by the query.</typeparam>
public interface IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Executes the query and returns the result.
    /// </summary>
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
