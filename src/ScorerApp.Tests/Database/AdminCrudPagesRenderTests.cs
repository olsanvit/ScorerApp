using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScorerApp.Data;
using ScorerApp.Domain.Models;

namespace ScorerApp.Tests.Database;

/// <summary>
/// CRUD formuláře administrace. Do 2026-09-30 neměly test ani proklikání — jediné nepokryté místo
/// podle docs/pages/index.md. Ověřuje se, že se stránka vyrenderuje, předvyplní editovaný záznam
/// a že ji neadmin nedostane. Hledají se ASCII data, ne přeložené texty (prerender kóduje diakritiku).
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class AdminCrudPagesRenderTests(DatabaseTestFactory factory) : IAsyncLifetime
{
    private string _admin = "", _member = "", _suffix = "";
    private Guid _leagueId, _seasonId, _teamId, _playerId;

    public async Task InitializeAsync()
    {
        _admin = await factory.CreateUserAsync("acadmin");
        _member = await factory.CreateUserAsync("acmember");
        _suffix = Guid.NewGuid().ToString("N")[..6];

        using var scope = factory.Services.CreateScope();
        await using var db = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();

        var football = await db.Sports.FirstAsync(x => x.Type == SportType.Football);
        var league = new League { Name = $"Liga-{_suffix}", SportId = football.Guid };
        var season = new Season
        {
            League = league, Name = $"Sezona-{_suffix}", Year = 2033, Status = SeasonStatus.Draft
        };
        // Vytvoření závodu nabízí jen běžecké (nebo vícečlenné) sezóny ve stavu InProgress.
        var running = await db.Sports.FirstAsync(x => x.Type == SportType.Running);
        var runSeason = new Season
        {
            League = new League { Name = $"Beh-{_suffix}", SportId = running.Guid },
            Name = $"Bezecka-{_suffix}", Year = 2033, Status = SeasonStatus.InProgress
        };
        db.Add(runSeason);

        var team = new Team { Name = $"Tym-{_suffix}", ShortName = "TYM" };
        var player = new Player { Name = $"Hrac-{_suffix}" };
        db.AddRange(league, season, team, player);
        await db.SaveChangesAsync();

        (_leagueId, _seasonId, _teamId, _playerId) = (league.Guid, season.Guid, team.Guid, player.Guid);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string> GetAsync(string url, string? userId = null, bool isAdmin = true,
        HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var client = factory.ClientAs(userId ?? _admin, isAdmin);
        using var response = await client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected,
            $"GET {url} → {(int)response.StatusCode}, čekáno {(int)expected}. Začátek odpovědi: {html[..Math.Min(html.Length, 500)]}");
        return html;
    }

    [Fact]
    public async Task CreateForms_Render()
    {
        // Výběry se plní z databáze — prázdný seznam sportů nebo lig by formulář udělal nepoužitelným.
        Assert.Contains("Football", await GetAsync("/admin/leagues/create"));
        Assert.Contains($"Liga-{_suffix}", await GetAsync("/admin/seasons/create"));
        await GetAsync("/admin/players/create");
        await GetAsync("/admin/teams/create");
        Assert.Contains($"Bezecka-{_suffix}", await GetAsync("/admin/races/create"));
    }

    [Fact]
    public async Task EditForms_ArePrefilledWithRecord()
    {
        Assert.Contains($"Liga-{_suffix}", await GetAsync($"/admin/leagues/{_leagueId}/edit"));
        Assert.Contains($"Sezona-{_suffix}", await GetAsync($"/admin/seasons/{_seasonId}/edit"));
        Assert.Contains($"Tym-{_suffix}", await GetAsync($"/admin/teams/{_teamId}/edit"));
        Assert.Contains($"Hrac-{_suffix}", await GetAsync($"/admin/players/{_playerId}/edit"));
    }

    [Fact]
    public async Task AdminPages_AreForbiddenForNonAdmin()
    {
        foreach (var url in new[]
        {
            "/admin", "/admin/leagues/create", "/admin/seasons/create", "/admin/players/create",
            "/admin/races/create", $"/admin/leagues/{_leagueId}/edit", $"/admin/seasons/{_seasonId}/edit",
            $"/admin/teams/{_teamId}/edit", $"/admin/players/{_playerId}/edit"
        })
            await GetAsync(url, _member, isAdmin: false, expected: HttpStatusCode.Forbidden);
    }
}
