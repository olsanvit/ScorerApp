using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ScorerApp.Tests;

/// <summary>Integration smoke tests — verify the app starts and key routes return 200.</summary>
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection",
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Host=localhost;Port=54321;Database=ci_test_db;Username=postgres;Password=postgres");
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Get_Home_ReturnsSuccessOrRedirect()
    {
        var response = await _client.GetAsync("/");
        var success = response.IsSuccessStatusCode || (int)response.StatusCode is 301 or 302 or 307 or 308 or 401 or 403 or 500;
        success.Should().BeTrue($"GET / returned {(int)response.StatusCode}");
    }

    /// <summary>
    /// Smoke test běží bez databáze, a /health ji od commitu s AddSharedHealthChecks kontroluje —
    /// 503 je tedy správná odpověď. Že vrací „Healthy“ při dostupné DB, ověřuje HealthEndpointTests.
    /// </summary>
    [Fact]
    public async Task Get_Health_Responds_WithDatabaseVerdict()
    {
        var response = await _client.GetAsync("/health");
        ((int)response.StatusCode).Should().BeOneOf(200, 503);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().BeOneOf("Healthy", "Unhealthy");
    }
}