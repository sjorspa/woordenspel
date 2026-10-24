using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Woordenspel.Api.Auth;
using Woordenspel.Api.Contracts;
using Woordenspel.Application;

namespace Woordenspel.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var account = app.MapGroup("/api/account").WithTags("Account");
        account.MapPost("/register", Register);
        account.MapPost("/login", Login);
        account.MapGet("/me", Me).RequireAuthorization();
        return app;
    }

    private static async Task<Created<AuthResponse>> Register(
        RegisterRequest request, AccountService accounts, JwtTokenIssuer tokens, CancellationToken cancellationToken)
    {
        var user = await accounts.RegisterAsync(request.Username ?? string.Empty, request.Password ?? string.Empty, cancellationToken);
        return TypedResults.Created("/api/account/me", tokens.Issue(user));
    }

    private static async Task<Ok<AuthResponse>> Login(
        LoginRequest request, AccountService accounts, JwtTokenIssuer tokens, CancellationToken cancellationToken)
    {
        var user = await accounts.AuthenticateAsync(request.Username ?? string.Empty, request.Password ?? string.Empty, cancellationToken);
        return TypedResults.Ok(tokens.Issue(user));
    }

    private static Ok<UserDto> Me(ClaimsPrincipal user) =>
        TypedResults.Ok(new UserDto(user.GetUserId(), user.Identity?.Name ?? string.Empty));
}
