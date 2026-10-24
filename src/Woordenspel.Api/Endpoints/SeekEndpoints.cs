using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Woordenspel.Api.Auth;
using Woordenspel.Application;
using Woordenspel.Contracts;

namespace Woordenspel.Api.Endpoints;

/// <summary>Uitnodigingen: een spel starten, zien wie er een tegenstander zoekt, en meedoen.</summary>
public static class SeekEndpoints
{
    public static IEndpointRouteBuilder MapSeekEndpoints(this IEndpointRouteBuilder app)
    {
        var seeks = app.MapGroup("/api/seeks").WithTags("Seeks").RequireAuthorization();
        seeks.MapPost("/", CreateSeek);
        seeks.MapGet("/", ListSeeks);
        seeks.MapPost("/{seekId:guid}/join", Join);
        seeks.MapDelete("/{seekId:guid}", Cancel);
        return app;
    }

    private static async Task<Created<SeekDto>> CreateSeek(
        CreateSeekRequest request, ClaimsPrincipal user, LobbyService lobby, CancellationToken cancellationToken)
    {
        var seek = await lobby.CreateSeekAsync(user.GetUserId(), request.Opponent, cancellationToken);
        return TypedResults.Created($"/api/seeks/{seek.Id}", seek);
    }

    private static async Task<Ok<IReadOnlyList<SeekDto>>> ListSeeks(
        ClaimsPrincipal user, LobbyService lobby, CancellationToken cancellationToken) =>
        TypedResults.Ok(await lobby.ListSeeksAsync(user.GetUserId(), cancellationToken));

    private static async Task<Created<GameDto>> Join(
        Guid seekId, ClaimsPrincipal user, LobbyService lobby, CancellationToken cancellationToken)
    {
        var game = await lobby.JoinAsync(seekId, user.GetUserId(), cancellationToken);
        return TypedResults.Created($"/api/games/{game.Id}", game);
    }

    private static async Task<NoContent> Cancel(
        Guid seekId, ClaimsPrincipal user, LobbyService lobby, CancellationToken cancellationToken)
    {
        await lobby.CancelAsync(seekId, user.GetUserId(), cancellationToken);
        return TypedResults.NoContent();
    }
}
