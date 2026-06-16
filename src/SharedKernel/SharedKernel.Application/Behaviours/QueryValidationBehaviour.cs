// ====================================================================
// VERIXORA – SharedKernel.Application / Behaviours / QueryValidationBehaviour.cs
// ====================================================================
// Summary:
//   MediatR pipeline behaviour that validates queries (IQuery<TResponse>)
//   using FluentValidation before the handler is invoked.
//
//   Why separate from commands:
//     - Queries return Result<TResponse>.  The generic signature
//       gives us the concrete type, so we can call
//       Result<TResponse>.Failure() directly – no reflection.
//     - The constraint `where TQuery : IQuery<TResponse>` ensures
//       only genuine queries are validated.
// ====================================================================

using FluentValidation;
using MediatR;
using SharedKernel.Application.Abstractions;
using SharedKernel.Domain.Results;

namespace SharedKernel.Application.Behaviours;

public class QueryValidationBehaviour<TQuery, TResponse>
    : IPipelineBehavior<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
    private readonly IEnumerable<IValidator<TQuery>> _validators;

    public QueryValidationBehaviour(IEnumerable<IValidator<TQuery>> validators)
    {
        _validators = validators;
    }

    public async Task<Result<TResponse>> Handle(
        TQuery request,
        RequestHandlerDelegate<Result<TResponse>> next,
        CancellationToken cancellationToken)
    {
        if (_validators is null || !_validators.Any())
            return await next();

        var context = new ValidationContext<TQuery>(request);
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
            return Result<TResponse>.Failure(errorMessage);   // compile‑time call
        }

        return await next();
    }
}
