using Microsoft.EntityFrameworkCore;
using ScorerApp.Data;
using ScorerApp.Domain.Models;
using ScorerApp.Domain.Models.Clubs;

namespace ScorerApp.Domain.Services;

public record DemoResult(bool Ok, int Rows, string? Error, bool TestUserPaired = false);

/// <summary>
/// Ukázková data pro průchod stránek na prázdné produkci (rozhodnutí Vítka 2026-10-17): spolek s oddílem,
/// tenisová a fotbalová liga, závod, chat, oběžník a auto — kolem 110 řádků. Vše má v názvu předponu
/// <see cref="Prefix"/>, aby šlo poznat a odebrat. Testovací účet se stane členem a spáruje se s hráčem,
/// jinak by chat, oběžníky ani „moje zápasy“ nešlo projít.
/// </summary>
public class DemoDataService(IDbContextFactory<AppDbContext> dbFactory, MatchGeneratorService generator,
    SeasonScheduleService schedule, SportRatingService ratings)
{
    public const string Prefix = "[Demo] ";
    public const string TestUserEmail = "olsansky575@gmail.com";

    public async Task<bool> ExistsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Organizations.AnyAsync(o => o.Name.StartsWith(Prefix) && o.IsActive);
    }

    /// <param name="adminUserId">Odesílatel oběžníku a autor chatu — za testovací účet se nic nepíše.</param>
    /// <param name="dryRun">Vše se zapíše v transakci a vrátí zpět; výsledek jen řekne, kolik řádků by vzniklo.</param>
    public async Task<DemoResult> CreateAsync(string adminUserId, bool dryRun = false, string testUserEmail = TestUserEmail)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.Organizations.AnyAsync(o => o.Name.StartsWith(Prefix) && o.IsActive))
            return new DemoResult(false, 0, "Demo_AlreadyExists");

        var sports = await db.Sports.ToDictionaryAsync(s => s.Type);
        if (!sports.TryGetValue(SportType.Tennis, out var tennis) ||
            !sports.TryGetValue(SportType.Football, out var football) ||
            !sports.TryGetValue(SportType.Running, out var running))
            return new DemoResult(false, 0, "Demo_SportsMissing");

        var normalized = testUserEmail.ToUpperInvariant();
        var testUserId = await db.Users.Where(u => u.NormalizedEmail == normalized)
            .Select(u => u.Id).FirstOrDefaultAsync();

        await using var tx = await db.Database.BeginTransactionAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = 0;
        T Add<T>(T entity) where T : class { db.Add(entity); rows++; return entity; }

        // ── Spolek ───────────────────────────────────────────────────────────────
        var org = Add(new Organization { Name = Prefix + "Ukázkový spolek", Description = "Ukázková data pro průchod aplikací." });
        var club = Add(new Club { Organization = org, Name = Prefix + "Ukázkový oddíl", ShortName = "DEMO" });
        Add(new Car { Organization = org, Name = Prefix + "Klubová dodávka", Brand = "Škoda", Model = "Octavia Combi", Year = 2021 });

        // ── Tenis: 6 hráčů, každý s každým, ELO ──────────────────────────────────
        string[] tennisNames = ["Vít O. (test)", "Petra Nováková", "Jan Dvořák", "Lucie Černá", "Tomáš Veselý", "Klára Horáková"];
        var tennisPlayers = tennisNames.Select(n => Add(new Player { Name = Prefix + n })).ToList();
        if (testUserId is not null) tennisPlayers[0].UserId = testUserId;
        foreach (var p in tennisPlayers) Add(new ClubMember { Club = club, Player = p });

        var tennisSeason = Add(new Season
        {
            League = Add(new League { Name = Prefix + "Tenisová liga", SportId = tennis.Guid }),
            Name = Prefix + "Sezóna 2026", Year = today.Year, Format = SeasonFormat.RoundRobin,
            Status = SeasonStatus.InProgress, UseElo = true, StartDate = today.AddDays(-30)
        });
        var tennisParts = tennisPlayers.Select(p => Add(new SeasonParticipant { Season = tennisSeason, Player = p, Club = club })).ToList();
        var tennisMatches = generator.Generate(tennisSeason, tennisParts);
        (int, int)[] tennisScores = [(2, 0), (2, 1), (1, 2), (2, 0), (0, 2), (2, 1), (2, 1), (1, 2)];
        for (var i = 0; i < tennisMatches.Count; i++)
        {
            var m = tennisMatches[i];
            m.MatchDate = today.AddDays(-28 + i * 3);
            if (i < tennisScores.Length) Play(m, tennisScores[i]);
            Add(m);
        }

        // ── Fotbal: 4 týmy po 5 hráčích, góly a karty ────────────────────────────
        string[] teamNames = ["Sokol", "Slavoj", "Spartak", "Dynamo"];
        string[] firstNames = ["Adam", "Ondřej", "Martin", "Filip", "David"];
        var teamParts = new List<SeasonParticipant>();
        var footballSeason = Add(new Season
        {
            League = Add(new League { Name = Prefix + "Fotbalová liga", SportId = football.Guid }),
            Name = Prefix + "Podzim 2026", Year = today.Year, Format = SeasonFormat.RoundRobin,
            Status = SeasonStatus.InProgress, StartDate = today.AddDays(-21)
        });
        var scorers = new Dictionary<SeasonParticipant, List<Player>>();
        foreach (var name in teamNames)
        {
            var team = Add(new Team { Name = Prefix + name, ShortName = name[..3].ToUpperInvariant(), Club = club });
            var players = firstNames.Select(f => Add(new Player { Name = $"{Prefix}{f} ({name})" })).ToList();
            foreach (var p in players) Add(new TeamPlayer { Team = team, Player = p, JoinedDate = today.AddDays(-60) });
            var part = Add(new SeasonParticipant { Season = footballSeason, Team = team, Club = club });
            teamParts.Add(part);
            scorers[part] = players;
        }
        var footballMatches = generator.Generate(footballSeason, teamParts);
        (int, int)[] footballScores = [(2, 1), (0, 0), (3, 2)];
        for (var i = 0; i < footballMatches.Count; i++)
        {
            var m = footballMatches[i];
            m.MatchDate = today.AddDays(-20 + i * 7);
            if (i < footballScores.Length)
            {
                Play(m, footballScores[i]);
                var home = teamParts.First(p => p.Guid == m.HomeParticipantId);
                var away = teamParts.First(p => p.Guid == m.AwayParticipantId);
                for (var g = 0; g < footballScores[i].Item1; g++)
                    Add(new MatchEvent { Match = m, Type = MatchEventType.Goal, Minute = 12 + g * 25, Player = scorers[home][g] });
                for (var g = 0; g < footballScores[i].Item2; g++)
                    Add(new MatchEvent { Match = m, Type = MatchEventType.Goal, Minute = 20 + g * 30, Player = scorers[away][g] });
                Add(new MatchEvent { Match = m, Type = MatchEventType.YellowCard, Minute = 55, Player = scorers[away][4] });
            }
            Add(m);
        }

        // ── Běh: závod tenistů na 5 km ───────────────────────────────────────────
        var runSeason = Add(new Season
        {
            League = Add(new League { Name = Prefix + "Běžecký pohár", SportId = running.Guid }),
            Name = Prefix + "Pohár 2026", Year = today.Year, Format = SeasonFormat.RoundRobin,
            Status = SeasonStatus.InProgress, StartDate = today.AddDays(-14)
        });
        var race = Add(new Race { Season = runSeason, Name = Prefix + "Podzimní pětka", Distance = 5, DistanceUnit = "km", RaceDate = today.AddDays(-7) });
        for (var i = 0; i < tennisPlayers.Count; i++)
        {
            var part = Add(new SeasonParticipant { Season = runSeason, Player = tennisPlayers[i], Club = club });
            Add(new RaceResult { Race = race, Participant = part, Position = i + 1, FinishTime = TimeSpan.FromSeconds(1150 + i * 47) });
        }

        // ── Chat a oběžník (jen když existuje testovací účet — jinak by je nikdo nečetl) ──
        if (testUserId is not null)
        {
            Add(new OrganizationMember { Organization = org, UserId = testUserId, Role = OrgRole.Member, DisplayName = "Vít O. (test)" });
            var thread = Add(new ClubThread { Club = club, Title = Prefix + "Organizace turnaje", CreatedByUserId = adminUserId });
            foreach (var body in new[] { "Ahoj všichni, v sobotu hrajeme od 9:00.", "Kurty jsou zamluvené do 14:00.", "Výsledky zadávejte prosím hned po zápase." })
                Add(new ChatMessage { Thread = thread, SenderUserId = adminUserId, Body = body });

            var circular = Add(new Circular
            {
                Organization = org, Club = club, SenderUserId = adminUserId, Subject = Prefix + "Podzimní soustředění",
                Body = "Soustředění proběhne poslední víkend v říjnu. Přihlášky do pátku.",
                Status = CircularStatus.Sent, SendEmail = false, SentAt = DateTimeOffset.UtcNow
            });
            Add(new CircularRecipient { Circular = circular, UserId = testUserId, EmailStatus = DeliveryStatus.Sent });
        }

        await db.SaveChangesAsync();
        if (dryRun)
        {
            await tx.RollbackAsync();
            return new DemoResult(true, rows, null, testUserId is not null);
        }
        await tx.CommitAsync();

        // ELO tenisové sezóny a žebříčky sportů — služba přehraje historii sama (přes svůj DbContext, proto až po commitu).
        var lastTennis = tennisMatches.Last(m => m.Status == MatchStatus.Played);
        await schedule.AfterResultSavedAsync(lastTennis.Guid);
        await schedule.AfterResultSavedAsync(footballMatches.First(m => m.Status == MatchStatus.Played).Guid);
        return new DemoResult(true, rows, null, testUserId is not null);
    }

    /// <summary>
    /// Odebere ukázková data. Stránky soft-smazané záznamy většinou nefiltrují, proto se sportovní a chatová data
    /// mažou fyzicky (ExecuteDelete obchází AuditInterceptor) — jen to, co má předponu <see cref="Prefix"/>.
    /// Spolek, oddíl, auto a členství se podle pravidla modulu Kluby jen deaktivují (rozhodnutí Vítka 2026-10-17).
    /// </summary>
    public async Task<int> RemoveAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        var n = 0;

        n += await db.ChatMessageReads.Where(r => r.Message.Thread.Title.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.ChatMessages.Where(m => m.Thread.Title.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.ThreadParticipants.Where(t => t.Thread.Title.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.ClubThreads.Where(t => t.Title.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.CircularRecipients.Where(r => r.Circular.Subject.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.Circulars.Where(c => c.Subject.StartsWith(Prefix)).ExecuteDeleteAsync();

        n += await db.MatchEvents.Where(e => e.Match.Season.Name.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.MatchSets.Where(e => e.Match.Season.Name.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.PlayoffMatches.Where(p => p.Season.Name.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.Matches.Where(m => m.Season.Name.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.RaceResults.Where(r => r.Race.Season.Name.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.Races.Where(r => r.Season.Name.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.SeasonParticipants.Where(p => p.Season.Name.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.Seasons.Where(s => s.Name.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.Leagues.Where(l => l.Name.StartsWith(Prefix)).ExecuteDeleteAsync();

        n += await db.SportRatings.Where(r => (r.PlayerId != null && db.Players.Any(p => p.Guid == r.PlayerId && p.Name.StartsWith(Prefix)))
                                           || (r.TeamId != null && db.Teams.Any(t => t.Guid == r.TeamId && t.Name.StartsWith(Prefix))))
            .ExecuteDeleteAsync();
        n += await db.TeamPlayers.Where(tp => tp.Team.Name.StartsWith(Prefix) || tp.Player.Name.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.ClubMembers.Where(m => m.Player.Name.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.Teams.Where(t => t.Name.StartsWith(Prefix)).ExecuteDeleteAsync();
        n += await db.Players.Where(p => p.Name.StartsWith(Prefix)).ExecuteDeleteAsync();

        n += await db.Cars.Where(c => c.Name.StartsWith(Prefix) && c.IsActive).ExecuteUpdateAsync(u => u.SetProperty(c => c.IsActive, false));
        n += await db.Clubs.Where(c => c.Name.StartsWith(Prefix) && c.IsActive).ExecuteUpdateAsync(u => u.SetProperty(c => c.IsActive, false));
        n += await db.OrganizationMembers.Where(m => m.Organization.Name.StartsWith(Prefix) && m.IsActive).ExecuteUpdateAsync(u => u.SetProperty(m => m.IsActive, false));
        n += await db.Organizations.Where(o => o.Name.StartsWith(Prefix) && o.IsActive).ExecuteUpdateAsync(u => u.SetProperty(o => o.IsActive, false));

        await tx.CommitAsync();
        await ratings.RecomputeAllAsync();
        return n;
    }

    private static void Play(Match m, (int home, int away) score)
    {
        m.HomeScore = score.home;
        m.AwayScore = score.away;
        m.Status = MatchStatus.Played;
    }
}
