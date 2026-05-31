using ScorerApp.Domain.Models;

namespace ScorerApp.Domain.Services;

public class StandingsRow
{
    public SeasonParticipant Participant { get; set; } = null!;
    public string DisplayName { get; set; } = "";
    public int Position { get; set; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int WonOT { get; set; }
    public int LostOT { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDiff => GoalsFor - GoalsAgainst;
    public int Points { get; set; }
    public decimal EloRating { get; set; }
}

public class StandingsService
{
    private readonly ScoringRulesService _scoring;

    public StandingsService(ScoringRulesService scoring) => _scoring = scoring;

    public List<StandingsRow> Calculate(
        Season season,
        IReadOnlyList<Match> playedMatches,
        ScoringRules rules)
    {
        var rows = season.Participants
            .Select(p => new StandingsRow
            {
                Participant = p,
                DisplayName = p.DisplayName,
                EloRating   = p.EloRating
            })
            .ToDictionary(r => r.Participant.Guid);

        foreach (var m in playedMatches.Where(m => m.Status == MatchStatus.Played
                                                   && m.HomeScore.HasValue && m.AwayScore.HasValue))
        {
            if (!rows.TryGetValue(m.HomeParticipantId, out var home)) continue;
            if (!rows.TryGetValue(m.AwayParticipantId, out var away)) continue;

            home.Played++; away.Played++;
            home.GoalsFor      += m.HomeScore!.Value; home.GoalsAgainst  += m.AwayScore!.Value;
            away.GoalsFor      += m.AwayScore!.Value; away.GoalsAgainst  += m.HomeScore!.Value;

            bool wasOT = m.ExtraTime == true || m.Penalties == true;

            if (m.HomeScore > m.AwayScore)
            {
                if (wasOT) { home.WonOT++; away.LostOT++; home.Points += rules.WinOT ?? rules.Win; away.Points += rules.LossOT ?? rules.Loss; }
                else        { home.Won++;   away.Lost++;   home.Points += rules.Win; away.Points += rules.Loss; }
            }
            else if (m.HomeScore < m.AwayScore)
            {
                if (wasOT) { away.WonOT++; home.LostOT++; away.Points += rules.WinOT ?? rules.Win; home.Points += rules.LossOT ?? rules.Loss; }
                else        { away.Won++;   home.Lost++;   away.Points += rules.Win; home.Points += rules.Loss; }
            }
            else if (rules.HasDraw)
            {
                home.Drawn++; away.Drawn++;
                home.Points += rules.Draw ?? 1; away.Points += rules.Draw ?? 1;
            }
        }

        var sorted = rows.Values
            .OrderByDescending(r => r.Points)
            .ThenByDescending(r => r.GoalDiff)
            .ThenByDescending(r => r.GoalsFor)
            .ToList();

        for (int i = 0; i < sorted.Count; i++) sorted[i].Position = i + 1;
        return sorted;
    }
}
