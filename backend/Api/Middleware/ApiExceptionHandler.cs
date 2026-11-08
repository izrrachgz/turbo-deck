using FluentValidation;
using Api.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware;

internal sealed class ApiExceptionHandler(IHostEnvironment environment, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        int status = exception switch
        {
            ValidationException or InvalidDataException => StatusCodes.Status400BadRequest,
            FileNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.UnhandledException(exception, context.TraceIdentifier);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = status switch
            {
                StatusCodes.Status400BadRequest => "The request is invalid.",
                StatusCodes.Status404NotFound => "The requested resource was not found.",
                _ => "The request could not be completed."
            },
            Detail = environment.IsDevelopment() ? exception.Message : null,
            Instance = context.Request.Path
        };
        problem.Extensions["errorCode"] = status == StatusCodes.Status400BadRequest ? "validation_failed" : "request_failed";
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
