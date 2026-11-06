using System.Diagnostics;
using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Features.Employees;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            EmployeePreconditionException { IsMissing: true } => (StatusCodes.Status428PreconditionRequired, "Precondition Required"),
            EmployeePreconditionException => (StatusCodes.Status412PreconditionFailed, "Precondition Failed"),
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (StatusCodes.Status412PreconditionFailed, "Precondition Failed"),
            ArgumentException or BadHttpRequestException => (StatusCodes.Status400BadRequest, "Bad Request"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Operation Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        // Expected client errors (4xx) are warnings without stack traces; only real faults are errors.
        if (statusCode >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Exception occurred while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning("Request rejected with {StatusCode} for {Method} {Path}: {ExceptionType}",
                statusCode, httpContext.Request.Method, httpContext.Request.Path, exception.GetType().Name);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "Terjadi kesalahan internal pada server. Silakan hubungi administrator dengan menyertakan traceId."
                : exception.Message,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestampUtc"] = DateTime.UtcNow;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
