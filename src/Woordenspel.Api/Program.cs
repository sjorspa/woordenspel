using System.Text.Json.Serialization;
using Woordenspel.Api.Auth;
using Woordenspel.Api.Endpoints;
using Woordenspel.Api.Errors;
using Woordenspel.Application;
using Woordenspel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration);

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

app.MapHealthChecks("/health");
app.MapAccountEndpoints();
app.MapGameEndpoints();
app.MapWordEndpoints();
app.MapSetupEndpoints();

await app.RunAsync();

public partial class Program;
