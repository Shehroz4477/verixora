// ====================================================================
// VERIXORA – SharedKernel.Application / Abstractions / ICommand.cs
// ====================================================================
// Summary:
//   Marker interface for CQRS commands.
//
//   ICommand        – a command that returns a plain Result (no data).
//   ICommand<TResponse> – a command that returns data on success.
//
//   Why two interfaces:
//     - Some commands only need to signal success/failure (e.g., DeleteUser).
//     - Other commands need to return data (e.g., RegisterUser returns
//       the new user's ID and email).
//     - The generic version ties the command to its response type,
//       making the contract explicit at compile time.
// ====================================================================

namespace SharedKernel.Application.Abstractions;

/// <summary>
/// Marker interface for commands that return no data.
/// </summary>
public interface ICommand { }

/// <summary>
/// Marker interface for commands that return a typed response.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
public interface ICommand<TResponse> { }
