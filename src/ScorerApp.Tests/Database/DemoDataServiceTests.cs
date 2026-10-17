using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScorerApp.Data;
using ScorerApp.Domain.Models;
using ScorerApp.Domain.Services;

namespace ScorerApp.Tests.Database;

/// <summary>Ukázková data pro průchod stránek (2026-10-17): dry-run nic neuloží, druhé vytvoření se odmítne, odebrání po sobě nic nenechá.</summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class DemoDataServiceTests(DatabaseTestFactory factory)
{
    [Fact]
    public async Task DryRun_Create_Remove_Roundtrip()
    {
        var admin = await factory.CreateUserAsync("demoadmin");
        var tester = await factory.CreateUserAsync("demotester");
        var testerEmail = await factory.EmailOfAsync(tester);

        using var scope = factory.Services.CreateScope();
        var demo = scope.ServiceProvider.GetRequiredService<DemoDataService>();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        async Task<int> DemoPlayers()
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            return await db.Players.CountAsync(p => p.Name.StartsWith(DemoDataService.Prefix));
        }

        try
        {
            var dry = await demo.CreateAsync(admin, dryRun: true, testUserEmail: testerEmail);
            Assert.True(dry.Ok, dry.Error);
            Assert.InRange(dry.Rows, 90, 140);
            Assert.Equal(0, await DemoPlayers());
            Assert.False(await demo.ExistsAsync());

            var created = await demo.CreateAsync(admin, testUserEmail: testerEmail);
            Assert.True(created.Ok, created.Error);
            Assert.True(created.TestUserPaired);
            Assert.Equal(dry.Rows, created.Rows);
            Assert.Equal(26, await DemoPlayers());
            Assert.Equal("Demo_AlreadyExists", (await demo.CreateAsync(admin, testUserEmail: testerEmail)).Error);

            await using (var db = await dbFactory.CreateDbContextAsync())
            {
                Assert.True(await db.Players.AnyAsync(p => p.UserId == tester && p.Name.StartsWith(DemoDataService.Prefix)));
                Assert.Equal(11, await db.Matches.CountAsync(m => m.Season.Name.StartsWith(DemoDataService.Prefix) && m.Status == MatchStatus.Played));
                // ELO se po vytvoření přepočítalo — tenisté už nemají všichni výchozích 1000.
                Assert.Contains(await db.SeasonParticipants
                    .Where(p => p.Season.Name.StartsWith(DemoDataService.Prefix) && p.Season.UseElo)
                    .Select(p => p.EloRating).ToListAsync(), r => r != 1000m);
            }
        }
        finally
        {
            await demo.RemoveAsync();
        }

        Assert.Equal(0, await DemoPlayers());
        Assert.False(await demo.ExistsAsync());
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            Assert.False(await db.Leagues.AnyAsync(l => l.Name.StartsWith(DemoDataService.Prefix)));
            Assert.False(await db.Organizations.AnyAsync(o => o.Name.StartsWith(DemoDataService.Prefix) && o.IsActive));
        }

        // Po odebrání jdou ukázková data vytvořit znovu (deaktivovaný spolek nesmí bránit).
        var again = await demo.CreateAsync(admin, testUserEmail: testerEmail);
        await demo.RemoveAsync();
        Assert.True(again.Ok, again.Error);
    }
}
