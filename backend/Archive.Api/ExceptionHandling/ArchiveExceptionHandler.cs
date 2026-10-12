using Archive.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.ExceptionHandling;

internal sealed class ArchiveExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            InvalidSessionException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid session",
                Detail = "The current session could not be validated."
            },
            BadHttpRequestException requestException => new ProblemDetails
            {
                Status = requestException.StatusCode
            },
            _ => null
        };

        if (problem is null)
            return false;

        await Results.Problem(problem).ExecuteAsync(httpContext);

        return true;
    }
}
