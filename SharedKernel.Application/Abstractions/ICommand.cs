// ====================================================================
// VERIXORA – SharedKernel.Application / Abstractions / ICommand.cs
// ====================================================================
// Summary:
//   Marker interface for all CQRS commands.
//   A command represents a write operation that changes system state
//   (create, update, delete, etc.).  Every command has exactly one
//   handler that implements ICommandHandler<TCommand>.
//
//   Why separate from IQuery:
//     - Commands mutate state; queries do not (CQS principle).
//     - Allows the MediatR pipeline to apply different behaviours
//       to commands (e.g., unit-of-work, idempotency) vs. queries
//       (e.g., caching, read-only optimisations).
//     - Architecture tests can verify that command handlers are
//       registered correctly.
//
//   Usage:
//     public record UnlockDoorCommand(Ulid SmartLockId, Ulid UserId)
//         : ICommand;
//
//   Design note:
//     ICommand does NOT carry a return type.  Handlers return
//     Result or Result<T> explicitly, keeping the contract
//     independent of the return type shape.
// ====================================================================

namespace SharedKernel.Application.Abstractions;

/// <summary>
/// Marker interface for commands (write operations).
/// </summary>
public interface ICommand
{
}
