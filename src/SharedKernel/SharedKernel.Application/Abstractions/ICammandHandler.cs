// ====================================================================
// VERIXORA – SharedKernel.Application / Abstractions / ICommandHandler.cs
// ====================================================================
// Summary:
//   Contracts for CQRS command handlers.
//   Now extend MediatR’s IRequestHandler<TRequest, TResponse> so that
//   handlers are automatically discovered and registered by AddMediatR.
//
//   ICommandHandler<TCommand>            – handles ICommand, returns Result
//   ICommandHandler<TCommand, TResponse> – handles ICommand<TResponse>, returns Result<TResponse>
// ====================================================================

using MediatR;
using SharedKernel.Domain.Results;

namespace SharedKernel.Application.Abstractions;

/// <summary>
/// Handles a command that returns a plain <see cref="Result"/>.
/// </summary>
public interface ICommandHandler<TCommand>
    : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{ }

/// <summary>
/// Handles a command that returns a typed <see cref="Result{TResponse}"/>.
/// </summary>
public interface ICommandHandler<TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{ }
