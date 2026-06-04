// ====================================================================
// VERIXORA – SharedKernel.Application / Abstractions / ICommandHandler.cs
// ====================================================================
// Summary:
//   Contract for a handler that processes a single type of command.
//   Each command has exactly one handler.  The handler returns a
//   Result (or Result<T>) to signal success or domain failure.
//
//   Why not use MediatR's IRequestHandler directly everywhere:
//     - Wrapping it behind our own interface lets us add shared
//       constraints (e.g., all commands return Result types).
//     - Architecture tests can verify that every command handler
//       implements this interface, not just IRequestHandler.
//     - Future changes to the handler contract (e.g., adding
//       cancellation token policy) can be done here without
//       touching every module.
//
//   Usage:
//     public class UnlockDoorHandler
//         : ICommandHandler<UnlockDoorCommand>
//     {
//         public async Task<Result> Handle(
//             UnlockDoorCommand command,
//             CancellationToken cancellationToken)
//         { ... }
//     }
// ====================================================================

namespace SharedKernel.Application.Abstractions;

/// <summary>
/// Handles a command of type <typeparamref name="TCommand"/> and
/// returns a <see cref="Result"/>.
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
