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
        Status = SeasonStatus.Draft
    };

    private static Season DoubleRoundRobinSeason() => new Season
    {
        Format = SeasonFormat.DoubleRoundRobin,
        Status = SeasonStatus.Draft
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

    // Kola musí odpovídat skutečnému okružnímu rozpisu — dřív dostal každý zápas vlastní
    // číslo kola a seskupení po kolech v SeasonMatches bylo k ničemu.
    [Theory]
    [InlineData(4, 3, 2)]
    [InlineData(6, 5, 3)]
    [InlineData(8, 7, 4)]
    public void Generate_RoundRobin_EvenCount_RoundsHaveAllParticipants(int participants, int expectedRounds, int matchesPerRound)
    {
        var matches = _svc.Generate(RoundRobinSeason(), Participants(participants));
        var rounds  = matches.GroupBy(m => m.Round).ToList();

        Assert.Equal(expectedRounds, rounds.Count);
        Assert.All(rounds, r => Assert.Equal(matchesPerRound, r.Count()));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    public void Generate_RoundRobin_OddCount_OneByePerRound(int participants)
    {
        var matches = _svc.Generate(RoundRobinSeason(), Participants(participants));
        var rounds  = matches.GroupBy(m => m.Round).ToList();

        // Lichý počet: n kol, v každém má jeden účastník volno → (n-1)/2 zápasů.
        Assert.Equal(participants, rounds.Count);
        Assert.All(rounds, r => Assert.Equal((participants - 1) / 2, r.Count()));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public void Generate_NobodyPlaysTwiceInOneRound(int participants)
    {
        var matches = _svc.Generate(DoubleRoundRobinSeason(), Participants(participants));
        foreach (var round in matches.GroupBy(m => m.Round))
        {
            var ids = round.SelectMany(m => new[] { m.HomeParticipantId, m.AwayParticipantId }).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }
    }

    [Fact]
    public void Generate_DoubleRoundRobin_SecondHalfMirrorsFirstWithSwappedHome()
    {
        var matches = _svc.Generate(DoubleRoundRobinSeason(), Participants(4));
        var firstHalf  = matches.Where(m => m.Round <= 3).ToList();
        var secondHalf = matches.Where(m => m.Round > 3).ToList();

        Assert.Equal(6, firstHalf.Count);
        Assert.Equal(6, secondHalf.Count);
        // Každý zápas první poloviny má odvetu s prohozeným domácím prostředím.
        Assert.All(firstHalf, m => Assert.Contains(secondHalf, r =>
            r.HomeParticipantId == m.AwayParticipantId && r.AwayParticipantId == m.HomeParticipantId));
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
