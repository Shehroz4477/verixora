// ====================================================================
// VERIXORA – SharedKernel.Application / Abstractions / ICommand.cs
// ====================================================================
// Summary:
//   Marker interfaces for CQRS commands.
//   Now extend MediatR’s IRequest<TResponse> so that commands can be
//   dispatched via IMediator.Send<TResponse>().
//
//   ICommand              – returns a plain Result
//   ICommand<TResponse>   – returns a Result<TResponse>
// ====================================================================

using MediatR;
using SharedKernel.Domain.Results;

namespace SharedKernel.Application.Abstractions;

/// <summary>
/// A command that returns a plain <see cref="Result"/>.
/// </summary>
public interface ICommand : IRequest<Result> { }

/// <summary>
/// A command that returns a typed <see cref="Result{TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The type of data returned on success.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
