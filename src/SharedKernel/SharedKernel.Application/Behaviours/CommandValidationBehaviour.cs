// ====================================================================
// VERIXORA – SharedKernel.Application / Behaviours / CommandValidationBehaviour.cs
// ====================================================================
// Summary:
//   MediatR pipeline behaviour that validates commands (ICommand)
//   using FluentValidation before the handler is invoked.
//
//   Why separate from queries:
//     - Commands always return a plain Result (no value).
//       This lets us call Result.Failure() directly – no reflection.
//     - The generic constraint `where TCommand : ICommand` gives
//       compile‑time safety.
//     - Aligns with the CQRS separation already present in the
//       abstractions.
// ====================================================================

using FluentValidation;
using MediatR;
using SharedKernel.Application.Abstractions;
using SharedKernel.Domain.Results;

namespace SharedKernel.Application.Behaviours;

public class CommandValidationBehaviour<TCommand, TResult>
    : IPipelineBehavior<TCommand, Result>
    where TCommand : ICommand
{
    private readonly IEnumerable<IValidator<TCommand>> _validators;

    public CommandValidationBehaviour(IEnumerable<IValidator<TCommand>> validators)
    {
        _validators = validators;
    }

    public async Task<Result> Handle(
        TCommand request,
        RequestHandlerDelegate<Result> next,
        CancellationToken cancellationToken)
    {
        // If no validators are registered, skip straight to the handler.
        if (_validators is null || !_validators.Any())
            return await next();

        var context = new ValidationContext<TCommand>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Any())
            .SelectMany(r => r.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        if (failures.Any())
        {
            var errorMessage = string.Join("; ", failures);
            return Result.Failure(errorMessage);   // compile‑time call, no reflection
        }

        return await next();
    }
}
