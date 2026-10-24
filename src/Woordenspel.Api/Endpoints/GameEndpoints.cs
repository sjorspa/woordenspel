using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Woordenspel.Api.Auth;
using Woordenspel.Contracts;
using Woordenspel.Application;

namespace Woordenspel.Api.Endpoints;

/// <summary>Alle acties gelden voor de ingelogde gebruiker.</summary>
public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var games = app.MapGroup("/api/games").WithTags("Games").RequireAuthorization();
        games.MapPost("/", CreateGame);
        games.MapGet("/", ListGames);
        games.MapGet("/{gameId:guid}", GetGame);
        games.MapPost("/{gameId:guid}/preview", Preview);
        games.MapPost("/{gameId:guid}/place", Place);
        games.MapPost("/{gameId:guid}/swap", Swap);
        games.MapPost("/{gameId:guid}/pass", Pass);
        games.MapPost("/{gameId:guid}/resign", Resign);
        return app;
    }

    private static async Task<Created<GameDto>> CreateGame(
        CreateGameRequest request, ClaimsPrincipal user, GameService service, CancellationToken cancellationToken)
    {
        var game = await service.CreateGameAsync(user.GetUserId(), request.Opponent ?? string.Empty, cancellationToken);
        return TypedResults.Created($"/api/games/{game.Id}", game);
    }

    private static async Task<Ok<IReadOnlyList<GameSummaryDto>>> ListGames(
        ClaimsPrincipal user, GameService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListGamesAsync(user.GetUserId(), cancellationToken));

    private static async Task<Ok<GameDto>> GetGame(
        Guid gameId, ClaimsPrincipal user, GameService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetGameAsync(gameId, user.GetUserId(), cancellationToken));

    private static async Task<Ok<PreviewDto>> Preview(
        Guid gameId, PlaceTilesRequest request, ClaimsPrincipal user, GameService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.PreviewPlacementAsync(gameId, user.GetUserId(), request.Tiles ?? [], cancellationToken));

    private static async Task<Ok<MoveResultDto>> Place(
        Guid gameId, PlaceTilesRequest request, ClaimsPrincipal user, GameService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.PlaceTilesAsync(gameId, user.GetUserId(), request.Tiles ?? [], cancellationToken));

    private static async Task<Ok<MoveResultDto>> Swap(
        Guid gameId, SwapTilesRequest request, ClaimsPrincipal user, GameService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SwapTilesAsync(gameId, user.GetUserId(), request.Letters ?? [], cancellationToken));

    private static async Task<Ok<MoveResultDto>> Pass(
        Guid gameId, ClaimsPrincipal user, GameService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.PassAsync(gameId, user.GetUserId(), cancellationToken));

    private static async Task<Ok<MoveResultDto>> Resign(
        Guid gameId, ClaimsPrincipal user, GameService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ResignAsync(gameId, user.GetUserId(), cancellationToken));
}
