// ====================================================================
// VERIXORA – SharedKernel.Application / Abstractions / ICommandHandler.cs
// ====================================================================
// Summary:
//   Contracts for CQRS command handlers.
//
//   ICommandHandler<TCommand>            – handles a command that
//     returns a plain Result.
//   ICommandHandler<TCommand, TResponse> – handles a command that
//     returns a Result<TResponse> with data.
//
//   Why two interfaces:
//     - Matches the two ICommand variants.
//     - Handlers that return data don't need to cast or use object.
//     - The generic constraint ensures type safety: a command
//       implementing ICommand<TResponse> can only be handled by
//       ICommandHandler<TCommand, TResponse>.
// ====================================================================

using SharedKernel.Domain.Results;

namespace SharedKernel.Application.Abstractions;

/// <summary>
/// Handles a command that returns no data.
/// </summary>
/// <typeparam name="TCommand">The command type (must implement <see cref="ICommand"/>).</typeparam>
public interface ICommandHandler<TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Executes the command and returns a Result indicating success or failure.
    /// </summary>
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Handles a command that returns a typed response.
/// </summary>
/// <typeparam name="TCommand">The command type (must implement <see cref="ICommand{TResponse}"/>).</typeparam>
/// <typeparam name="TResponse">The type of data returned on success.</typeparam>
public interface ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Executes the command and returns a <see cref="Result{TResponse}"/>.
    /// </summary>
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
