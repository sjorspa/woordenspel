using Microsoft.AspNetCore.Http.HttpResults;
using Woordenspel.Application;
using Woordenspel.Contracts;

namespace Woordenspel.Api.Endpoints;

public static class InfoEndpoints
{
    public static IEndpointRouteBuilder MapWordEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/words/{word}", CheckWord).WithTags("Words");
        return app;
    }

    public static IEndpointRouteBuilder MapSetupEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/setup", GetSetup).WithTags("Setup");
        return app;
    }

    private static Ok<WordCheckDto> CheckWord(string word, GameService service) => TypedResults.Ok(service.CheckWord(word));

    private static Ok<SetupDto> GetSetup(GameService service) => TypedResults.Ok(service.GetSetup());
}
