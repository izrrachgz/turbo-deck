using Api.Logging;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Localization.Abstractions;
using Microsoft.AspNetCore.Diagnostics;
using Application.Localization.Resources;

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

        ITextProvider messages = context.RequestServices.GetRequiredService<ITextProvider>();
        var problem = new ProblemDetails
        {
            Status = status,
            Title = status switch
            {
                StatusCodes.Status400BadRequest => messages.Get(nameof(Messages.Invalid_Model)),
                StatusCodes.Status404NotFound => messages.Get(nameof(Messages.Not_Found)),
                _ => messages.Get(nameof(Messages.Server_Error))
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
