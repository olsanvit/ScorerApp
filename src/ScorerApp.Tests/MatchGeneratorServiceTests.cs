using ScorerApp.Domain.Models;
using ScorerApp.Domain.Services;

namespace ScorerApp.Tests;

public class MatchGeneratorServiceTests
{
    private readonly MatchGeneratorService _svc = new();

    private static List<SeasonParticipant> Participants(int count) =>
        Enumerable.Range(1, count).Select(_ => new SeasonParticipant()).ToList();

    private static Season RoundRobinSeason() => new Season
    {
        Format = SeasonFormat.RoundRobin,
        Status = SeasonStatus.Planning
    };

    private static Season DoubleRoundRobinSeason() => new Season
    {
        Format = SeasonFormat.DoubleRoundRobin,
        Status = SeasonStatus.Planning
    };

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 3)]
    [InlineData(4, 6)]
    [InlineData(6, 15)]
    public void Generate_RoundRobin_CorrectMatchCount(int participants, int expectedMatches)
    {
        var season  = RoundRobinSeason();
        var players = Participants(participants);
        var matches = _svc.Generate(season, players);
        Assert.Equal(expectedMatches, matches.Count);
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(4, 12)]
    [InlineData(6, 30)]
    public void Generate_DoubleRoundRobin_CorrectMatchCount(int participants, int expectedMatches)
    {
        var season  = DoubleRoundRobinSeason();
        var players = Participants(participants);
        var matches = _svc.Generate(season, players);
        Assert.Equal(expectedMatches, matches.Count);
    }

    [Fact]
    public void Generate_AllMatchesScheduled()
    {
        var matches = _svc.Generate(RoundRobinSeason(), Participants(4));
        Assert.All(matches, m => Assert.Equal(MatchStatus.Scheduled, m.Status));
    }

    [Fact]
    public void Generate_NoDuplicatePairs_RoundRobin()
    {
        var participants = Participants(5);
        var matches = _svc.Generate(RoundRobinSeason(), participants);
        var pairs = matches.Select(m => (
            Min: m.HomeParticipantId < m.AwayParticipantId ? m.HomeParticipantId : m.AwayParticipantId,
            Max: m.HomeParticipantId > m.AwayParticipantId ? m.HomeParticipantId : m.AwayParticipantId
        )).ToList();
        Assert.Equal(pairs.Count, pairs.Distinct().Count());
    }

    [Fact]
    public void Generate_DoubleRoundRobin_EachPairTwice()
    {
        var participants = Participants(3);
        var matches = _svc.Generate(DoubleRoundRobinSeason(), participants);
        // For 3 participants: 3 pairs × 2 = 6 matches
        Assert.Equal(6, matches.Count);
        // Each unordered pair appears exactly twice
        var grouped = matches.GroupBy(m =>
        {
            var ids = new[] { m.HomeParticipantId, m.AwayParticipantId }.OrderBy(x => x).ToArray();
            return (ids[0], ids[1]);
        });
        Assert.All(grouped, g => Assert.Equal(2, g.Count()));
    }
}
