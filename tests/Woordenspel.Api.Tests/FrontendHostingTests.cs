using System.Net;

namespace Woordenspel.Api.Tests;

public class FrontendHostingTests(WoordenspelApiFactory factory) : IClassFixture<WoordenspelApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/")]
    [InlineData("/games/123")]
    [InlineData("/login")]
    public async Task Frontend_pages_serve_the_blazor_app(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("_framework/blazor.webassembly", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_api_routes_are_not_found()
    {
        var response = await _client.GetAsync("/api/bestaat-niet");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
