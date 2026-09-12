using Microsoft.EntityFrameworkCore;
using ScorerApp.Data;
using ScorerApp.Domain.Models;

namespace ScorerApp.Domain.Services;

public record PlayoffResult(bool Ok, string? Error)
{
    public static readonly PlayoffResult Success = new(true, null);
    public static PlayoffResult Fail(string error) => new(false, error);
}

/// <summary>Jedno kolo pavouka pro zobrazení.</summary>
public record BracketRound(int Round, string Name, List<PlayoffMatch> Matches);

/// <summary>
/// Generování a průběh vyřazovacího pavouka. Pozice v pavouku jsou PlayoffMatch (existují dřív,
/// než se ví, kdo je obsadí); Match se skóre vzniká až ve chvíli, kdy jsou známi oba soupeři.
/// </summary>
public class PlayoffService(IDbContextFactory<AppDbContext> dbFactory)
{
    /// <summary>
    /// Pořadí nasazení v pavouku — rekurzivní zrcadlení, aby se jednička s dvojkou potkaly
    /// nejdřív ve finále: [1,2] → [1,4,2,3] → [1,8,4,5,2,7,3,6].
    /// </summary>
    public static List<int> SeedOrder(int bracketSize)
    {
        var order = new List<int> { 1, 2 };
        while (order.Count < bracketSize)
        {
            var total = order.Count * 2 + 1;
            var next  = new List<int>(order.Count * 2);
            foreach (var seed in order)
            {
                next.Add(seed);
                next.Add(total - seed);
            }
            order = next;
        }
        return order;
    }

    public static int NextPowerOfTwo(int n)
    {
        int size = 2;
        while (size < n) size *= 2;
        return size;
    }

    public static string RoundName(int round, int totalRounds)
    {
        int remaining = 1 << (totalRounds - round + 1);   // kolik účastníků v tomto kole zbývá
        return remaining switch
        {
            2  => "Finále",
            4  => "Semifinále",
            8  => "Čtvrtfinále",
            16 => "Osmifinále",
            _  => $"{round}. kolo ({remaining} účastníků)"
        };
    }

    /// <summary>
    /// Sestaví pavouk z nasazených účastníků (pořadí = nasazení, první je jednička).
    /// Pokud účastníků není mocnina dvou, nejlepší nasazení dostanou volný los do dalšího kola.
    /// </summary>
    public async Task<PlayoffResult> GenerateBracketAsync(
        Guid seasonId, IReadOnlyList<Guid> seededParticipantIds, int? bracketSize, int moduleIndex)
    {
        var take = bracketSize.HasValue
            ? Math.Min(bracketSize.Value, seededParticipantIds.Count)
            : seededParticipantIds.Count;
        if (take < 2) return PlayoffResult.Fail("Na playoff jsou potřeba alespoň 2 účastníci.");

        var size        = NextPowerOfTwo(take);
        var totalRounds = (int)Math.Log2(size);
        var order       = SeedOrder(size);

        await using var db = await dbFactory.CreateDbContextAsync();

        var existing = await db.PlayoffMatches.Where(p => p.SeasonId == seasonId).ToListAsync();
        if (existing.Any(p => p.WinnerId is not null))
            return PlayoffResult.Fail("Pavouk už má odehrané zápasy. Nejdřív je smaž, než ho přegeneruješ.");
        if (existing.Count > 0) db.PlayoffMatches.RemoveRange(existing);

        // Nejdřív prázdné pozice pro všechna kola — pozdější kola čekají na postupující.
        var byRound = new Dictionary<int, List<PlayoffMatch>>();
        for (int round = 1; round <= totalRounds; round++)
        {
            var positions = size >> round;
            var list = new List<PlayoffMatch>();
            for (int pos = 1; pos <= positions; pos++)
            {
                var pm = new PlayoffMatch
                {
                    SeasonId        = seasonId,
                    Round           = round,
                    BracketPosition = pos
                };
                list.Add(pm);
                db.PlayoffMatches.Add(pm);
            }
            byRound[round] = list;
        }

        // První kolo: nasazení podle SeedOrder; seed mimo počet účastníků = volný los.
        for (int pos = 1; pos <= size / 2; pos++)
        {
            var seedA = order[(pos - 1) * 2];
            var seedB = order[(pos - 1) * 2 + 1];
            var pm    = byRound[1][pos - 1];

            pm.SeedA = seedA <= take ? seedA : null;
            pm.SeedB = seedB <= take ? seedB : null;
            pm.ParticipantAId = seedA <= take ? seededParticipantIds[seedA - 1] : null;
            pm.ParticipantBId = seedB <= take ? seededParticipantIds[seedB - 1] : null;
        }

        // Volné losy vyřešíme hned, ať pavouk nezačíná „zápasem“ proti nikomu.
        foreach (var pm in byRound[1])
        {
            if (pm.ParticipantAId is not null && pm.ParticipantBId is null)
                Advance(byRound, pm, pm.ParticipantAId.Value, totalRounds);
            else if (pm.ParticipantBId is not null && pm.ParticipantAId is null)
                Advance(byRound, pm, pm.ParticipantBId.Value, totalRounds);
        }

        var season = await db.Seasons.FirstOrDefaultAsync(s => s.Guid == seasonId);
        CreateMissingMatches(db, byRound, season, moduleIndex, totalRounds);

        await db.SaveChangesAsync();
        return PlayoffResult.Success;
    }

    /// <summary>
    /// Zpracuje výsledek playoff zápasu — určí vítěze a posune ho do dalšího kola.
    /// Volá se po uložení skóre; pro ligové zápasy se nic nestane.
    /// </summary>
    public async Task<PlayoffResult> ProcessResultAsync(Guid matchId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var pm = await db.PlayoffMatches.FirstOrDefaultAsync(p => p.MatchId == matchId);
        if (pm is null) return PlayoffResult.Success;   // běžný ligový zápas

        var match = await db.Matches.FirstOrDefaultAsync(m => m.Guid == matchId);
        if (match is null || match.Status != MatchStatus.Played) return PlayoffResult.Success;

        var winnerId = DetermineWinner(match, pm);
        if (winnerId is null)
            return PlayoffResult.Fail(
                "V playoff musí být vítěz — u remízy doplň penaltové skóre v detailu zápasu.");

        if (pm.WinnerId == winnerId) return PlayoffResult.Success;   // beze změny

        var all = await db.PlayoffMatches.Where(p => p.SeasonId == pm.SeasonId).ToListAsync();
        var byRound = all.GroupBy(p => p.Round).ToDictionary(g => g.Key, g => g.OrderBy(p => p.BracketPosition).ToList());
        var totalRounds = byRound.Keys.Max();

        // Oprava výsledku po odehraném dalším kole by rozbila pavouk — radši odmítneme.
        var next = NextSlot(byRound, pm, totalRounds);
        if (pm.WinnerId is not null && next is not null)
        {
            var nextMatch = next.Value.Slot.MatchId is Guid nid
                ? await db.Matches.FirstOrDefaultAsync(m => m.Guid == nid)
                : null;
            if (nextMatch?.Status == MatchStatus.Played)
                return PlayoffResult.Fail(
                    "Navazující zápas už je odehraný — nejdřív smaž jeho výsledek, pak oprav tento.");
        }

        pm.WinnerId  = winnerId;
        pm.UpdatedAt = DateTimeOffset.UtcNow;
        Advance(byRound, pm, winnerId.Value, totalRounds);

        var season = await db.Seasons.FirstOrDefaultAsync(s => s.Guid == pm.SeasonId);
        CreateMissingMatches(db, byRound, season, match.ModuleIndex, totalRounds);

        // Finále rozhodnuto → sezóna je dohraná.
        if (pm.Round == totalRounds && season is not null && season.Status != SeasonStatus.Completed)
        {
            season.Status    = SeasonStatus.Completed;
            season.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();
        return PlayoffResult.Success;
    }

    public async Task<List<BracketRound>> GetBracketAsync(Guid seasonId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var all = await db.PlayoffMatches
            .Where(p => p.SeasonId == seasonId)
            .Include(p => p.ParticipantA).ThenInclude(p => p!.Player)
            .Include(p => p.ParticipantA).ThenInclude(p => p!.Team)
            .Include(p => p.ParticipantB).ThenInclude(p => p!.Player)
            .Include(p => p.ParticipantB).ThenInclude(p => p!.Team)
            .Include(p => p.Match)
            .OrderBy(p => p.Round).ThenBy(p => p.BracketPosition)
            .ToListAsync();

        if (all.Count == 0) return [];

        var totalRounds = all.Max(p => p.Round);
        return all.GroupBy(p => p.Round)
            .OrderBy(g => g.Key)
            .Select(g => new BracketRound(g.Key, RoundName(g.Key, totalRounds), g.ToList()))
            .ToList();
    }

    public async Task<bool> HasBracketAsync(Guid seasonId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.PlayoffMatches.AnyAsync(p => p.SeasonId == seasonId);
    }

    /// <summary>Vítěz zápasu; u remízy rozhodnou penalty, jinak null (pavouk nemůže pokračovat).</summary>
    private static Guid? DetermineWinner(Match match, PlayoffMatch pm)
    {
        int home = match.HomeScore ?? 0, away = match.AwayScore ?? 0;
        if (home != away)
            return home > away ? match.HomeParticipantId : match.AwayParticipantId;

        int ph = match.HomePenaltyScore ?? 0, pa = match.AwayPenaltyScore ?? 0;
        if (ph != pa)
            return ph > pa ? match.HomeParticipantId : match.AwayParticipantId;

        return null;
    }

    /// <summary>Pozice v dalším kole, kam postupuje vítěz — a jestli obsadí slot A, nebo B.</summary>
    private static (PlayoffMatch Slot, bool IsSlotA)? NextSlot(
        IReadOnlyDictionary<int, List<PlayoffMatch>> byRound, PlayoffMatch pm, int totalRounds)
    {
        if (pm.Round >= totalRounds) return null;
        var nextPosition = (pm.BracketPosition + 1) / 2;
        var slot = byRound[pm.Round + 1].FirstOrDefault(p => p.BracketPosition == nextPosition);
        return slot is null ? null : (slot, pm.BracketPosition % 2 == 1);
    }

    private static void Advance(
        IReadOnlyDictionary<int, List<PlayoffMatch>> byRound, PlayoffMatch pm, Guid winnerId, int totalRounds)
    {
        pm.WinnerId = winnerId;

        var next = NextSlot(byRound, pm, totalRounds);
        if (next is null) return;

        var (slot, isSlotA) = next.Value;
        if (isSlotA) slot.ParticipantAId = winnerId;
        else         slot.ParticipantBId = winnerId;
        slot.UpdatedAt = DateTimeOffset.UtcNow;

        // Volný los v prvním kole může postoupit rovnou přes víc kol, pokud protistrana chybí.
        if (slot.ParticipantAId is not null && slot.ParticipantBId is null && pm.Round + 1 < totalRounds
            && byRound[pm.Round].Where(p => (p.BracketPosition + 1) / 2 == slot.BracketPosition)
                                .All(p => p.WinnerId is not null || p.ParticipantAId is null && p.ParticipantBId is null))
        {
            // Druhá strana dvojice nemá účastníka vůbec → postup bez zápasu.
            Advance(byRound, slot, slot.ParticipantAId.Value, totalRounds);
        }
    }

    /// <summary>Pro pozice, kde jsou známi oba účastníci a chybí zápas, založí Match se skóre.</summary>
    private static void CreateMissingMatches(
        AppDbContext db, IReadOnlyDictionary<int, List<PlayoffMatch>> byRound,
        Season? season, int moduleIndex, int totalRounds)
    {
        if (season is null) return;

        foreach (var (round, positions) in byRound.OrderBy(kv => kv.Key))
        {
            foreach (var pm in positions)
            {
                if (pm.MatchId is not null) continue;
                if (pm.ParticipantAId is null || pm.ParticipantBId is null) continue;

                var match = new Match
                {
                    SeasonId          = season.Guid,
                    Round             = round,
                    Stage             = MatchStage.Playoff,
                    ModuleIndex       = moduleIndex,
                    HomeParticipantId = pm.ParticipantAId.Value,
                    AwayParticipantId = pm.ParticipantBId.Value,
                    Status            = MatchStatus.Scheduled
                };
                db.Matches.Add(match);
                pm.MatchId   = match.Guid;
                pm.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}
