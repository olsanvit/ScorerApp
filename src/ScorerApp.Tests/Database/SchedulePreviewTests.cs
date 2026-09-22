using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScorerApp.Data;
using ScorerApp.Domain.Models;
using ScorerApp.Domain.Services;

namespace ScorerApp.Tests.Database;

/// <summary>
/// Náhled na generovací stránce („Generovat N zápasů“) musí sedět s tím, co GenerateAsync opravdu vytvoří.
/// Dřív se počítal ze starého enumu, takže u skupin, Swissu a playoff ukazoval jiné číslo, než kolik zápasů vzniklo.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class SchedulePreviewTests(DatabaseTestFactory factory)
{
    public static TheoryData<string, SeasonFormat, int> Formats => new()
    {
        { "", SeasonFormat.RoundRobin, 5 },
        { "", SeasonFormat.DoubleRoundRobin, 4 },
        { """{"modules":[{"type":"GroupStage","groupCount":2}]}""", SeasonFormat.Custom, 8 },
        { """{"modules":[{"type":"Swiss","rounds":3}]}""", SeasonFormat.Custom, 5 },
        { """{"modules":[{"type":"Playoff"}]}""", SeasonFormat.Custom, 6 },
        { """{"modules":[{"type":"Playoff","bracketSize":4}]}""", SeasonFormat.Custom, 7 },
    };

    [Theory]
    [MemberData(nameof(Formats))]
    public async Task Preview_MatchesWhatGenerateCreates(string formatJson, SeasonFormat format, int participants)
    {
        using var scope = factory.Services.CreateScope();
        var schedule = scope.ServiceProvider.GetRequiredService<SeasonScheduleService>();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        Guid seasonId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var football = await db.Sports.FirstAsync(x => x.Type == SportType.Football);
            var s = Guid.NewGuid().ToString("N")[..6];
            var season = new Season
            {
                League = new League { Name = $"League-{s}", SportId = football.Guid },
                Name = $"Season-{s}", Year = 2032, Status = SeasonStatus.Registration,
                Format = format, FormatJson = string.IsNullOrEmpty(formatJson) ? null : formatJson
            };
            for (var i = 1; i <= participants; i++)
                season.Participants.Add(new SeasonParticipant { Player = new Player { Name = $"P{i}-{s}" } });
            db.Seasons.Add(season);
            await db.SaveChangesAsync();
            seasonId = season.Guid;
        }

        int preview;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var season = await db.Seasons.Include(x => x.League).ThenInclude(l => l.Sport)
                .Include(x => x.Participants).FirstAsync(x => x.Guid == seasonId);
            preview = schedule.PreviewFirstPhaseCount(season);
        }

        var result = await schedule.GenerateAsync(seasonId);
        Assert.True(result.Ok, result.Error);
        Assert.True(preview > 0);
        Assert.Equal(result.MatchCount, preview);
    }
}
