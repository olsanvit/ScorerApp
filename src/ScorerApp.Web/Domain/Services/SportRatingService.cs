using Microsoft.EntityFrameworkCore;
using ScorerApp.Data;
using ScorerApp.Domain.Models;

namespace ScorerApp.Domain.Services;

/// <summary>Celkový rating hráče přes sporty — vážený průměr podle počtu odehraných zápasů.</summary>
public record OverallRating(decimal Rating, int Games, IReadOnlyList<SportRating> PerSport);

/// <summary>
/// Trvalý rating per sport. Počítá se vždy přehráním celé historie sportu od výchozích 1000 —
/// stejně jako sezónní ELO. Inkrementální update by po opravě starého výsledku dal jiné číslo
/// než plný přepočet a ranking by se rozešel s realitou.
/// </summary>
public class SportRatingService(IDbContextFactory<AppDbContext> dbFactory, EloService elo)
{
    public async Task RecomputeSportAsync(Guid sportId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var matches = await db.Matches
            .AsNoTracking()
            .Where(m => m.Status == MatchStatus.Played
                        && m.HomeScore != null && m.AwayScore != null
                        && m.Season.League.SportId == sportId)
            .OrderBy(m => m.Season.Year)
            .ThenBy(m => m.Season.CreatedAt)
            .ThenBy(m => m.ModuleIndex)
            .ThenBy(m => m.Round)
            .ThenBy(m => m.MatchDate)
            .ThenBy(m => m.CreatedAt)
            .Select(m => new
            {
                m.HomeParticipantId,
                m.AwayParticipantId,
                HomeScore = m.HomeScore!.Value,
                AwayScore = m.AwayScore!.Value
            })
            .ToListAsync();

        // Účastník sezóny → skutečná entita (hráč nebo tým), na které rating visí.
        var participantOwner = await db.SeasonParticipants
            .AsNoTracking()
            .Where(p => p.Season.League.SportId == sportId)
            .Select(p => new { p.Guid, p.PlayerId, p.TeamId })
            .ToDictionaryAsync(p => p.Guid, p => (p.PlayerId, p.TeamId));

        var state = new Dictionary<(Guid? PlayerId, Guid? TeamId), RatingState>();

        RatingState StateFor(Guid participantId)
        {
            var key = participantOwner.TryGetValue(participantId, out var owner) ? owner : (null, null);
            if (!state.TryGetValue(key, out var s))
            {
                s = new RatingState { Rating = EloService.StartElo };
                state[key] = s;
            }
            return s;
        }

        foreach (var m in matches)
        {
            if (!participantOwner.ContainsKey(m.HomeParticipantId) ||
                !participantOwner.ContainsKey(m.AwayParticipantId))
                continue;

            var home = StateFor(m.HomeParticipantId);
            var away = StateFor(m.AwayParticipantId);

            double homeResult = m.HomeScore > m.AwayScore ? 1.0
                              : m.HomeScore < m.AwayScore ? 0.0
                              : 0.5;

            var (newHome, newAway) = elo.Calculate(home.Rating, away.Rating, homeResult);
            home.Rating = newHome;
            away.Rating = newAway;

            home.Games++; away.Games++;
            if (homeResult == 1.0)      { home.Wins++;  away.Losses++; }
            else if (homeResult == 0.0) { home.Losses++; away.Wins++;  }
            else                        { home.Draws++; away.Draws++;  }
        }

        var existing = await db.SportRatings.Where(r => r.SportId == sportId).ToListAsync();
        var now = DateTimeOffset.UtcNow;

        foreach (var ((playerId, teamId), s) in state)
        {
            if (playerId is null && teamId is null) continue;

            var row = existing.FirstOrDefault(r => r.PlayerId == playerId && r.TeamId == teamId);
            if (row is null)
            {
                row = new SportRating { SportId = sportId, PlayerId = playerId, TeamId = teamId };
                db.SportRatings.Add(row);
            }

            row.Rating    = s.Rating;
            row.Games     = s.Games;
            row.Wins      = s.Wins;
            row.Draws     = s.Draws;
            row.Losses    = s.Losses;
            row.UpdatedAt = now;
        }

        await db.SaveChangesAsync();
    }

    public async Task RecomputeAllAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var sportIds = await db.Sports.Select(s => s.Guid).ToListAsync();
        foreach (var id in sportIds)
            await RecomputeSportAsync(id);
    }

    /// <summary>
    /// Celkový rating hráče = vážený průměr přes sporty (rating × počet her / celkem her).
    /// Prostý průměr by šel nafouknout jedním turnajem v novém sportu.
    /// </summary>
    public async Task<OverallRating> GetOverallAsync(Guid playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var rows = await db.SportRatings
            .Include(r => r.Sport)
            .Where(r => r.PlayerId == playerId)
            .OrderByDescending(r => r.Games)
            .ToListAsync();

        var games = rows.Sum(r => r.Games);
        var rating = games > 0
            ? rows.Sum(r => r.Rating * r.Games) / games
            : EloService.StartElo;

        return new OverallRating(rating, games, rows);
    }

    private sealed class RatingState
    {
        public decimal Rating { get; set; }
        public int Games { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
    }
}
