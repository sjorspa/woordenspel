using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Woordenspel.Application;
using Woordenspel.Domain;

namespace Woordenspel.Api.Errors;

/// <summary>Vertaalt applicatie- en domeinfouten naar ProblemDetails met een stabiele <c>code</c>.</summary>
internal sealed class GameExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code) = exception switch
        {
            GameNotFoundException => (StatusCodes.Status404NotFound, "GameNotFound"),
            UserNotFoundException => (StatusCodes.Status404NotFound, "UserNotFound"),
            SeekNotFoundException => (StatusCodes.Status404NotFound, "SeekNotFound"),
            SeekUnavailableException => (StatusCodes.Status409Conflict, "SeekUnavailable"),
            UsernameTakenException => (StatusCodes.Status409Conflict, "UsernameTaken"),
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "AuthenticationFailed"),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "ConcurrencyConflict"),
            DomainException { Code: DomainErrors.PlayerNotInGame } e => (StatusCodes.Status404NotFound, e.Code),
            DomainException { Code: DomainErrors.SeekNotForYou } e => (StatusCodes.Status403Forbidden, e.Code),
            DomainException { Code: DomainErrors.NotYourTurn or DomainErrors.GameFinished } e => (StatusCodes.Status409Conflict, e.Code),
            DomainException e => (StatusCodes.Status422UnprocessableEntity, e.Code),
            _ => (0, null),
        };

        if (code is null)
        {
            return false;
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = exception.Message,
            Extensions = { ["code"] = code },
        };

        if (exception is InvalidWordsException invalidWords)
        {
            problem.Extensions["invalidWords"] = invalidWords.InvalidWords;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
