using Microsoft.EntityFrameworkCore;
using ScorerApp.Data;
using ScorerApp.Domain.Models;

namespace ScorerApp.Domain.Services;

public record ScheduleResult(bool Ok, int MatchCount, string? Error)
{
    public static ScheduleResult Fail(string error) => new(false, 0, error);
}

/// <summary>Stav rozehranosti formátu — podklad pro tlačítka „generovat další fázi“.</summary>
public record SeasonPhaseStatus(
    int CurrentModuleIndex,
    SeasonFormatModule? CurrentModule,
    SeasonFormatModule? NextModule,
    bool CurrentComplete,
    bool AnythingGenerated);

/// <summary>
/// Řídí rozpis sezóny podle modulárního formátu — generuje vždy jen aktuální fázi,
/// protože nasazení do dalších fází závisí na výsledcích té předchozí.
/// </summary>
public class SeasonScheduleService(
    IDbContextFactory<AppDbContext> dbFactory,
    MatchGeneratorService generator,
    SeasonFormatService formats,
    StandingsService standings,
    ScoringRulesService scoring,
    PlayoffService playoff,
    SportRatingService ratings)
{
    /// <summary>
    /// Vygeneruje první fázi sezóny a přepne ji do stavu InProgress.
    /// Existující rozpis bez výsledků se nahradí; při odehraných zápasech se operace odmítne.
    /// </summary>
    public async Task<ScheduleResult> GenerateAsync(Guid seasonId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var season = await LoadSeasonAsync(db, seasonId);
        if (season is null) return ScheduleResult.Fail("Sezóna nebyla nalezena.");
        if (season.Participants.Count < 2) return ScheduleResult.Fail("Potřebuješ alespoň 2 účastníky.");

        // Závodní sporty (běh…) nemají zápasy hlava-na-hlavu — výsledky se zadávají přes Race.
        // Uzavření registrace u nich jen přepne stav, jinak by vznikl nesmyslný round-robin
        // a sezóna by se bez něj do stavu InProgress (a tím k přidávání závodů) nedostala.
        if (season.League.Sport.MatchType == SportMatchType.MultiParticipant)
        {
            MarkInProgress(season);
            await db.SaveChangesAsync();
            return new ScheduleResult(true, 0, null);
        }

        var existing = await db.Matches.Where(m => m.SeasonId == seasonId).ToListAsync();
        if (existing.Any(m => m.Status == MatchStatus.Played))
            return ScheduleResult.Fail(
                "Sezóna už má odehrané zápasy s výsledky. Smaž je ručně, než vygeneruješ nový rozpis.");
        db.Matches.RemoveRange(existing);

        var definition = formats.Resolve(season);
        var module     = definition.Modules.FirstOrDefault();
        if (module is null) return ScheduleResult.Fail("Formát sezóny nemá žádný modul.");

        var seeded  = await SeedOrderAsync(db, season);
        var matches = BuildModuleMatches(season, module, moduleIndex: 0, seeded, startRound: 1);

        if (module.Type == SeasonModuleType.Playoff)
        {
            MarkInProgress(season);
            await db.SaveChangesAsync();

            var result = await playoff.GenerateBracketAsync(
                seasonId, seeded.Select(p => p.Guid).ToList(), module.BracketSize, moduleIndex: 0);
            if (!result.Ok) return ScheduleResult.Fail(result.Error!);

            var created = await db.Matches.CountAsync(m => m.SeasonId == seasonId);
            return new ScheduleResult(true, created, null);
        }

        db.Matches.AddRange(matches);
        MarkInProgress(season);
        await db.SaveChangesAsync();
        return new ScheduleResult(true, matches.Count, null);
    }

    /// <summary>Vygeneruje navazující fázi (další modul, nebo další kolo Swissu).</summary>
    public async Task<ScheduleResult> GenerateNextPhaseAsync(Guid seasonId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var season = await LoadSeasonAsync(db, seasonId);
        if (season is null) return ScheduleResult.Fail("Sezóna nebyla nalezena.");

        var definition = formats.Resolve(season);
        var allMatches = await LoadMatchesAsync(db, seasonId);
        var status     = BuildStatus(definition, allMatches, season);

        if (!status.CurrentComplete)
            return ScheduleResult.Fail("Aktuální fáze ještě není dohraná.");

        // Swiss generuje kolo po kole — dalším krokem nemusí být hned nový modul.
        if (status.CurrentModule?.Type == SeasonModuleType.Swiss)
        {
            var playedRounds = allMatches
                .Where(m => m.ModuleIndex == status.CurrentModuleIndex)
                .Select(m => m.Round).DefaultIfEmpty(0).Max();
            var swissRounds = status.CurrentModule.Rounds ?? 5;
            var roundsDone  = allMatches.Count(m => m.ModuleIndex == status.CurrentModuleIndex) == 0
                ? 0
                : allMatches.Where(m => m.ModuleIndex == status.CurrentModuleIndex)
                            .Select(m => m.Round).Distinct().Count();

            if (roundsDone < swissRounds)
            {
                var next = BuildSwissRound(season, allMatches, status.CurrentModuleIndex, playedRounds + 1);
                db.Matches.AddRange(next);
                await db.SaveChangesAsync();
                return new ScheduleResult(true, next.Count, null);
            }
        }

        if (status.NextModule is null)
            return ScheduleResult.Fail("Sezóna nemá žádnou další fázi.");

        var nextIndex = status.CurrentModuleIndex + 1;
        var seeded    = await SeedFromPreviousPhaseAsync(db, season, allMatches, status);
        var startRound = allMatches.Select(m => m.Round).DefaultIfEmpty(0).Max() + 1;

        if (status.NextModule.Type == SeasonModuleType.Playoff)
        {
            var result = await playoff.GenerateBracketAsync(
                seasonId, seeded.Select(p => p.Guid).ToList(), status.NextModule.BracketSize, nextIndex);
            return result.Ok
                ? new ScheduleResult(true, 0, null)
                : ScheduleResult.Fail(result.Error!);
        }

        var matches = BuildModuleMatches(season, status.NextModule, nextIndex, seeded, startRound);
        db.Matches.AddRange(matches);
        await db.SaveChangesAsync();
        return new ScheduleResult(true, matches.Count, null);
    }

    public async Task<SeasonPhaseStatus> GetPhaseStatusAsync(Guid seasonId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var season = await LoadSeasonAsync(db, seasonId);
        if (season is null)
            return new SeasonPhaseStatus(0, null, null, false, false);

        var definition = formats.Resolve(season);
        var matches    = await LoadMatchesAsync(db, seasonId);
        return BuildStatus(definition, matches, season);
    }

    /// <summary>
    /// Po uložení výsledku: přepočet sezónního ELO, posun v pavouku a aktualizace trvalého
    /// ratingu sportu. Sdílené místo pro SeasonMatches i MatchDetail, aby se logika nerozcházela.
    /// </summary>
    public async Task<PlayoffResult> AfterResultSavedAsync(Guid matchId)
    {
        var playoffResult = await playoff.ProcessResultAsync(matchId);

        await using var db = await dbFactory.CreateDbContextAsync();
        var match = await db.Matches
            .Include(m => m.Season).ThenInclude(s => s.League)
            .FirstOrDefaultAsync(m => m.Guid == matchId);
        if (match is not null)
            await ratings.RecomputeSportAsync(match.Season.League.SportId);

        return playoffResult;
    }

    private static void MarkInProgress(Season season)
    {
        if (!season.Status.AllowsParticipantChanges()) return;
        season.Status    = SeasonStatus.InProgress;
        season.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static Task<Season?> LoadSeasonAsync(AppDbContext db, Guid seasonId) =>
        db.Seasons
            .Include(s => s.League).ThenInclude(l => l.Sport)
            .Include(s => s.Participants).ThenInclude(p => p.Player)
            .Include(s => s.Participants).ThenInclude(p => p.Team)
            .FirstOrDefaultAsync(s => s.Guid == seasonId);

    private static async Task<List<Match>> LoadMatchesAsync(AppDbContext db, Guid seasonId) =>
        await db.Matches.AsNoTracking().Where(m => m.SeasonId == seasonId).ToListAsync();

    private List<Match> BuildModuleMatches(
        Season season, SeasonFormatModule module, int moduleIndex,
        List<SeasonParticipant> seeded, int startRound) => module.Type switch
    {
        SeasonModuleType.RoundRobin => generator.GenerateRoundRobin(
            season, seeded, module.MatchesPerPair ?? 1, moduleIndex, startRound),

        SeasonModuleType.GroupStage => generator.GenerateGroupStage(
            season, seeded, module.GroupCount ?? 2, module.MatchesPerPair ?? 1, moduleIndex, startRound),

        SeasonModuleType.Swiss => generator.GenerateSwissRound(
            season, seeded, new HashSet<(Guid, Guid)>(), startRound, moduleIndex),

        _ => []   // Playoff se generuje přes PlayoffService
    };

    private List<Match> BuildSwissRound(Season season, List<Match> allMatches, int moduleIndex, int round)
    {
        var rules = scoring.Resolve(season.League.Sport, season.League);
        var played = allMatches.Where(m => m.ModuleIndex == moduleIndex && m.Status == MatchStatus.Played).ToList();
        var table  = standings.Calculate(season, played, rules);

        var alreadyPlayed = allMatches
            .Where(m => m.ModuleIndex == moduleIndex)
            .Select(m => MatchGeneratorService.PairKey(m.HomeParticipantId, m.AwayParticipantId))
            .ToHashSet();

        var byStanding = table.Select(r => r.Participant).ToList();
        return generator.GenerateSwissRound(season, byStanding, alreadyPlayed, round, moduleIndex);
    }

    /// <summary>Nasazení pro první fázi — podle trvalého ratingu sportu, aby se favorité nepotkali hned.</summary>
    private async Task<List<SeasonParticipant>> SeedOrderAsync(AppDbContext db, Season season)
    {
        var sportId = season.League.SportId;
        var rating  = await db.SportRatings
            .Where(r => r.SportId == sportId)
            .ToDictionaryAsync(r => r.PlayerId ?? r.TeamId ?? Guid.Empty, r => r.Rating);

        return season.Participants
            .OrderByDescending(p => rating.TryGetValue(p.PlayerId ?? p.TeamId ?? Guid.Empty, out var r)
                ? r
                : EloService.StartElo)
            .ThenBy(p => p.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Nasazení do další fáze podle výsledků té předchozí. U skupin postupuje AdvanceCount
    /// z každé skupiny a řadí se „všichni první, pak všichni druzí“, aby se vítězové skupin
    /// potkali co nejpozději.
    /// </summary>
    private async Task<List<SeasonParticipant>> SeedFromPreviousPhaseAsync(
        AppDbContext db, Season season, List<Match> allMatches, SeasonPhaseStatus status)
    {
        var rules  = scoring.Resolve(season.League.Sport, season.League);
        var played = allMatches
            .Where(m => m.ModuleIndex == status.CurrentModuleIndex && m.Status == MatchStatus.Played)
            .ToList();

        if (status.CurrentModule?.Type == SeasonModuleType.GroupStage)
        {
            var advance   = status.CurrentModule.AdvanceCount ?? 2;
            var byGroup   = allMatches
                .Where(m => m.ModuleIndex == status.CurrentModuleIndex && m.GroupIndex.HasValue)
                .GroupBy(m => m.GroupIndex!.Value)
                .OrderBy(g => g.Key);

            var perGroup = new List<List<SeasonParticipant>>();
            foreach (var group in byGroup)
            {
                var ids = group.SelectMany(m => new[] { m.HomeParticipantId, m.AwayParticipantId }).ToHashSet();
                var members = season.Participants.Where(p => ids.Contains(p.Guid)).ToList();
                var table = standings.Calculate(season,
                    played.Where(m => m.GroupIndex == group.Key).ToList(), rules, members);
                perGroup.Add(table.Take(advance).Select(r => r.Participant).ToList());
            }

            var seeded = new List<SeasonParticipant>();
            for (int position = 0; position < advance; position++)
                foreach (var group in perGroup)
                    if (position < group.Count) seeded.Add(group[position]);
            return seeded;
        }

        var overall = standings.Calculate(season, played, rules);
        return overall.Select(r => r.Participant).ToList();
    }

    private static SeasonPhaseStatus BuildStatus(
        SeasonFormatDefinition definition, List<Match> matches, Season season)
    {
        var anything = matches.Count > 0;
        var current  = anything ? matches.Max(m => m.ModuleIndex) : 0;
        var module   = definition.Modules.ElementAtOrDefault(current);
        var next     = definition.Modules.ElementAtOrDefault(current + 1);

        var moduleMatches = matches.Where(m => m.ModuleIndex == current).ToList();
        var complete = anything
                       && moduleMatches.Count > 0
                       && moduleMatches.All(m => m.Status is MatchStatus.Played or MatchStatus.Cancelled);

        // Swiss je dohraný, až když proběhl nastavený počet kol.
        if (complete && module?.Type == SeasonModuleType.Swiss)
        {
            var roundsDone = moduleMatches.Select(m => m.Round).Distinct().Count();
            if (roundsDone < (module.Rounds ?? 5))
                return new SeasonPhaseStatus(current, module, next, true, anything);
        }

        return new SeasonPhaseStatus(current, module, next, complete, anything);
    }
}
