// ====================================================================
// VERIXORA – SharedKernel.Application / Behaviours / LoggingBehaviour.cs
// ====================================================================
// Summary:
//   A MediatR pipeline behaviour that logs every incoming command
//   or query before and after the handler executes.
//
//   Why:
//     - Provides a centralised audit trail of all application
//       operations without cluttering individual handlers.
//     - Logs the request type, handler name, and execution time.
//     - Structured logging (Serilog) automatically captures
//       TenantId, UserId, and CorrelationId via enrichers.
//
//   Design:
//     - The behaviour is generic over IRequest<TResponse> so it
//       applies to every command and query.
//     - Uses Microsoft.Extensions.Logging.ILogger for
//       framework‑agnostic structured logging.
// ====================================================================

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Application.Behaviours;

/// <summary>
/// Logs the execution of every MediatR request.
/// </summary>
/// <typeparam name="TRequest">The command or query type.</typeparam>
/// <typeparam name="TResponse">The Result type returned by the handler.</typeparam>
public class LoggingBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs the request, invokes the handler, and logs the outcome.
    /// </summary>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var handlerName = typeof(LoggingBehaviour<TRequest, TResponse>).Name;

        _logger.LogInformation(
            "Handling {RequestName} in {HandlerName}",
            requestName, handlerName);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();

            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds}ms",
                requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Error handling {RequestName} after {ElapsedMilliseconds}ms",
                requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
