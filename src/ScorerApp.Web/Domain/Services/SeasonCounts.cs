using Microsoft.EntityFrameworkCore;
using ScorerApp.Data;
using ScorerApp.Domain.Models;

namespace ScorerApp.Domain.Services;

/// <summary>Počty do přehledových tabulek; struct, aby sezóna bez záznamu měla rovnou nuly.</summary>
public readonly record struct SeasonCount(int Participants, int Matches, int Played);

/// <summary>
/// Počty účastníků a zápasů sezón spočítané v DB (GROUP BY). Přehledy dřív přes Include tahaly všechny
/// zápasy všech sezón jen kvůli číslu v tabulce — s rostoucí historií by se stránky neúměrně zpomalovaly.
/// </summary>
public static class SeasonCounts
{
    public static async Task<Dictionary<Guid, SeasonCount>> LoadAsync(AppDbContext db, ICollection<Guid> seasonIds)
    {
        if (seasonIds.Count == 0) return new();

        var participants = await db.SeasonParticipants
            .Where(p => seasonIds.Contains(p.SeasonId))
            .GroupBy(p => p.SeasonId)
            .Select(g => new { SeasonId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SeasonId, x => x.Count);

        var matches = await db.Matches
            .Where(m => seasonIds.Contains(m.SeasonId))
            .GroupBy(m => m.SeasonId)
            .Select(g => new { SeasonId = g.Key, All = g.Count(), Played = g.Count(m => m.Status == MatchStatus.Played) })
            .ToDictionaryAsync(x => x.SeasonId, x => (x.All, x.Played));

        return seasonIds.Distinct().ToDictionary(id => id, id =>
        {
            var (all, played) = matches.GetValueOrDefault(id);
            return new SeasonCount(participants.GetValueOrDefault(id), all, played);
        });
    }
}
