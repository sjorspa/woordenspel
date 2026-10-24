using System.Text.Json.Serialization;
using Woordenspel.Api.Auth;
using Woordenspel.Api.Endpoints;
using Woordenspel.Api.Errors;
using Woordenspel.Api.Realtime;
using Woordenspel.Application;
using Woordenspel.Contracts;
using Woordenspel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddRealtime(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GameExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// De Blazor-frontend.
app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapAccountEndpoints();
app.MapGameEndpoints();
app.MapWordEndpoints();
app.MapSetupEndpoints();
app.MapHub<GameHub>(GameHubContract.Path);

// Onbekende API-routes geven een 404; al het andere is een pagina van de frontend.
app.MapFallback("/api/{**path}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound));
app.MapFallbackToFile("index.html");

await app.RunAsync();

public partial class Program;
