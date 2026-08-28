using ScorerApp.Domain.Models;

namespace ScorerApp.Domain.Services;

public class EloService
{
    public const decimal StartElo = 1000m;

    // AUDIT:FIXED|byl: K=32 hardcoded; nyní parametr s defaultem
    /// <summary>Returns (newHome, newAway). homeResult: 1=win, 0.5=draw, 0=loss.</summary>
    public (decimal newHome, decimal newAway) Calculate(decimal homeElo, decimal awayElo, double homeResult, double k = 32)
    {
        double h = (double)homeElo;
        double a = (double)awayElo;
        double expectedHome = 1.0 / (1.0 + Math.Pow(10, (a - h) / 400.0));
        double expectedAway = 1.0 - expectedHome;
        double awayResult   = 1.0 - homeResult;

        return (
            (decimal)(h + k * (homeResult  - expectedHome)),
            (decimal)(a + k * (awayResult  - expectedAway))
        );
    }

    /// <summary>
    /// Přepočítá EloRating všech účastníků od <see cref="StartElo"/> přehráním odehraných zápasů
    /// v zadaném pořadí. Deterministické — nezávisí na pořadí zadávání výsledků, jen na historii.
    /// Mutuje entity <paramref name="participants"/> (EloRating + UpdatedAt); uložení řeší volající.
    /// </summary>
    public void RecomputeSeason(
        IReadOnlyList<SeasonParticipant> participants,
        IReadOnlyList<Match> playedMatchesInOrder,
        double k = 32)
    {
        var now = DateTimeOffset.UtcNow;
        var byId = participants.ToDictionary(p => p.Guid);
        foreach (var p in participants)
        {
            p.EloRating = StartElo;
            p.UpdatedAt = now;
        }

        foreach (var m in playedMatchesInOrder)
        {
            if (!byId.TryGetValue(m.HomeParticipantId, out var home) ||
                !byId.TryGetValue(m.AwayParticipantId, out var away))
                continue;

            var h = m.HomeScore ?? 0;
            var a = m.AwayScore ?? 0;
            double homeResult = h > a ? 1.0 : h < a ? 0.0 : 0.5;

            var (newHome, newAway) = Calculate(home.EloRating, away.EloRating, homeResult, k);
            home.EloRating = newHome;
            away.EloRating = newAway;
        }
    }
}
