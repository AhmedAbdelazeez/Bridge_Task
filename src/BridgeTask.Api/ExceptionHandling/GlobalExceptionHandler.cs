using BridgeTask.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BridgeTask.Api.ExceptionHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // The client has gone away; there is nobody to send a response to and nothing worth logging as an error.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var (statusCode, title, detail) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Resource conflict", exception.Message),
            BusinessValidationException => (StatusCodes.Status400BadRequest, "Business rule violation", exception.Message),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "Bad request", "The request could not be processed."),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error", "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            }
        });
    }
}
