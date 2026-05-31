using ScorerApp.Domain.Models;

namespace ScorerApp.Domain.Services;

public class MatchGeneratorService
{
    /// <summary>Generates Round Robin or Double Round Robin matches for a season.</summary>
    public List<Match> Generate(Season season, List<SeasonParticipant> participants)
    {
        var matches = new List<Match>();
        var pairs   = GetPairs(participants);

        int round = 1;
        foreach (var (home, away) in pairs)
        {
            matches.Add(new Match
            {
                SeasonId          = season.Guid,
                Round             = round++,
                HomeParticipantId = home.Guid,
                AwayParticipantId = away.Guid,
                Status            = MatchStatus.Scheduled
            });
        }

        if (season.Format == SeasonFormat.DoubleRoundRobin)
        {
            foreach (var (home, away) in pairs)
            {
                matches.Add(new Match
                {
                    SeasonId          = season.Guid,
                    Round             = round++,
                    HomeParticipantId = away.Guid,   // swapped
                    AwayParticipantId = home.Guid,
                    Status            = MatchStatus.Scheduled
                });
            }
        }

        return matches;
    }

    private static IEnumerable<(SeasonParticipant, SeasonParticipant)> GetPairs(
        List<SeasonParticipant> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
            for (int j = i + 1; j < list.Count; j++)
                yield return (list[i], list[j]);
    }
}
