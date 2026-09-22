using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScorerApp.Data;
using ScorerApp.Domain.Models;
using ScorerApp.Domain.Services;

namespace ScorerApp.Tests.Database;

/// <summary>
/// Sportovní stránky nad sezónou s rozpisem a odehraným zápasem. Render testy klubů mají jen prázdnou
/// sezónu — tabulky, zápasy a počty by se tam nerozbily viditelně. Kontrolují se ASCII data, ne texty.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class SportPagesRenderTests(DatabaseTestFactory factory) : IAsyncLifetime
{
    private string _admin = "", _suffix = "";
    private Guid _leagueId, _seasonId, _playedMatchId, _playerId;

    public async Task InitializeAsync()
    {
        _admin = await factory.CreateUserAsync("sadmin");
        _suffix = Guid.NewGuid().ToString("N")[..6];

        using var scope = factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var schedule = scope.ServiceProvider.GetRequiredService<SeasonScheduleService>();

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var football = await db.Sports.FirstAsync(x => x.Type == SportType.Football);
            var season = new Season
            {
                League = new League { Name = $"League-{_suffix}", SportId = football.Guid },
                Name = $"Season-{_suffix}", Year = 2032, Status = SeasonStatus.Registration,
                Format = SeasonFormat.RoundRobin
            };
            foreach (var n in new[] { "A", "B", "C", "D" })
                season.Participants.Add(new SeasonParticipant { Player = new Player { Name = $"Hrac-{n}-{_suffix}" } });
            db.Seasons.Add(season);
            await db.SaveChangesAsync();
            (_leagueId, _seasonId) = (season.LeagueId, season.Guid);
            _playerId = season.Participants[0].PlayerId!.Value;
        }

        var generated = await schedule.GenerateAsync(_seasonId);
        Assert.True(generated.Ok, generated.Error);

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var match = await db.Matches.Where(m => m.SeasonId == _seasonId).OrderBy(m => m.Round).FirstAsync();
            (match.HomeScore, match.AwayScore, match.Status) = (2, 1, MatchStatus.Played);
            await db.SaveChangesAsync();
            _playedMatchId = match.Guid;
        }
        await schedule.AfterResultSavedAsync(_playedMatchId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string> GetAsync(string url)
    {
        using var client = factory.ClientAs(_admin, isAdmin: true);
        using var response = await client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"GET {url} → {(int)response.StatusCode}. Začátek odpovědi: {html[..Math.Min(html.Length, 500)]}");
        return html;
    }

    [Fact]
    public async Task SeasonPages_ShowStandingsMatchesAndCounts()
    {
        // Round robin 4 hráčů = 6 zápasů, 1 odehraný — počty jdou z SeasonCounts, ne z Include.
        Assert.Contains("1 / 6", await GetAsync("/seasons"));
        Assert.Contains($"Season-{_suffix}", await GetAsync($"/leagues/{_leagueId}"));
        Assert.Contains($"Season-{_suffix}", await GetAsync("/"));

        Assert.Contains($"Hrac-A-{_suffix}", await GetAsync($"/seasons/{_seasonId}"));
        Assert.Contains($"Hrac-D-{_suffix}", await GetAsync($"/seasons/{_seasonId}/matches"));
        Assert.Contains($"Hrac-", await GetAsync($"/matches/{_playedMatchId}"));
        Assert.Contains($"Hrac-A-{_suffix}", await GetAsync($"/players/{_playerId}"));
        await GetAsync($"/admin/seasons/{_seasonId}/generate");
        await GetAsync("/rankings");
    }

    /// <summary>
    /// Klíče pro přihlašovací cookie musí být v DB — v kontejneru by se po jeho novém vytvoření ztratily
    /// a všichni by se odhlásili. Protect() vynutí vytvoření klíče přes nakonfigurované úložiště.
    /// </summary>
    [Fact]
    public async Task DataProtectionKeys_ArePersistedInDatabase()
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("test").Protect("x");

        await using var db = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();
        Assert.True(await db.DataProtectionKeys.AnyAsync());
    }

    [Fact]
    public async Task SeasonCounts_MatchIncludeSemantics_IncludingSoftDelete()
    {
        using var scope = factory.Services.CreateScope();
        await using var db = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();

        var before = (await SeasonCounts.LoadAsync(db, [_seasonId]))[_seasonId];
        Assert.Equal(new SeasonCount(4, 6, 1), before);

        // Smazaný (soft delete) zápas nesmí být v počtu — Include ho díky globálnímu filtru taky nevracel.
        var unplayed = await db.Matches.FirstAsync(m => m.SeasonId == _seasonId && m.Status != MatchStatus.Played);
        db.Matches.Remove(unplayed);
        await db.SaveChangesAsync();

        var empty = Guid.NewGuid();
        var after = await SeasonCounts.LoadAsync(db, [_seasonId, empty]);
        Assert.Equal(new SeasonCount(4, 5, 1), after[_seasonId]);
        Assert.Equal(default, after[empty]);
    }
}
