using ScorerApp.Domain.Models;
using ScorerApp.Domain.Services;

namespace ScorerApp.Tests;

public class PlayoffAndFormatTests
{
    private readonly MatchGeneratorService _generator = new();
    private readonly SeasonFormatService _formats = new();

    private static List<SeasonParticipant> Participants(int count) =>
        Enumerable.Range(1, count).Select(_ => new SeasonParticipant()).ToList();

    private static Season Season(SeasonFormat format = SeasonFormat.RoundRobin) =>
        new() { Format = format, Status = SeasonStatus.Draft };

    // ── Nasazení do pavouka ───────────────────────────────────────────────────

    [Fact]
    public void SeedOrder_PutsTopSeedsInOppositeHalves()
    {
        Assert.Equal([1, 4, 2, 3], PlayoffService.SeedOrder(4));
        Assert.Equal([1, 8, 4, 5, 2, 7, 3, 6], PlayoffService.SeedOrder(8));
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(5, 8)]
    [InlineData(8, 8)]
    [InlineData(9, 16)]
    public void NextPowerOfTwo_RoundsUp(int input, int expected) =>
        Assert.Equal(expected, PlayoffService.NextPowerOfTwo(input));

    [Theory]
    [InlineData(3, 3, "Finále")]
    [InlineData(2, 3, "Semifinále")]
    [InlineData(1, 3, "Čtvrtfinále")]
    public void RoundName_UsesCzechNames(int round, int totalRounds, string expected) =>
        Assert.Equal(expected, PlayoffService.RoundName(round, totalRounds));

    // ── Skupinová fáze ────────────────────────────────────────────────────────

    [Fact]
    public void GenerateGroupStage_SplitsEvenlyAndTagsGroups()
    {
        var matches = _generator.GenerateGroupStage(Season(), Participants(8), groupCount: 2);

        // 2 skupiny po 4 → v každé 6 zápasů
        Assert.Equal(12, matches.Count);
        Assert.All(matches, m => Assert.Equal(MatchStage.Group, m.Stage));
        Assert.Equal([1, 2], matches.Select(m => m.GroupIndex!.Value).Distinct().Order());
        Assert.All(matches.GroupBy(m => m.GroupIndex), g => Assert.Equal(6, g.Count()));
    }

    [Fact]
    public void GenerateGroupStage_SnakeSeedingKeepsTopSeedsApart()
    {
        var participants = Participants(4);
        var matches = _generator.GenerateGroupStage(Season(), participants, groupCount: 2);

        // Had: 1. a 4. nasazený do skupiny A, 2. a 3. do skupiny B — jedničky se nepotkají.
        var groupOfFirst  = matches.First(m => m.HomeParticipantId == participants[0].Guid
                                            || m.AwayParticipantId == participants[0].Guid).GroupIndex;
        var groupOfSecond = matches.First(m => m.HomeParticipantId == participants[1].Guid
                                            || m.AwayParticipantId == participants[1].Guid).GroupIndex;
        Assert.NotEqual(groupOfFirst, groupOfSecond);
    }

    // ── Swiss ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateSwissRound_PairsEveryoneOnce()
    {
        var participants = Participants(6);
        var played = new HashSet<(Guid, Guid)>();

        var round = _generator.GenerateSwissRound(Season(), participants, played, round: 1);

        Assert.Equal(3, round.Count);
        Assert.All(round, m => Assert.Equal(MatchStage.Swiss, m.Stage));
        var ids = round.SelectMany(m => new[] { m.HomeParticipantId, m.AwayParticipantId }).ToList();
        Assert.Equal(6, ids.Distinct().Count());
    }

    [Fact]
    public void GenerateSwissRound_AvoidsRematches()
    {
        var participants = Participants(4);
        var played = new HashSet<(Guid, Guid)>();

        var first  = _generator.GenerateSwissRound(Season(), participants, played, round: 1);
        var second = _generator.GenerateSwissRound(Season(), participants, played, round: 2);

        var firstPairs  = first.Select(m => MatchGeneratorService.PairKey(m.HomeParticipantId, m.AwayParticipantId)).ToHashSet();
        var secondPairs = second.Select(m => MatchGeneratorService.PairKey(m.HomeParticipantId, m.AwayParticipantId)).ToHashSet();

        Assert.Empty(firstPairs.Intersect(secondPairs));
    }

    [Fact]
    public void GenerateSwissRound_OddCountLeavesOnePlayerOut()
    {
        var round = _generator.GenerateSwissRound(Season(), Participants(5), new HashSet<(Guid, Guid)>(), round: 1);
        Assert.Equal(2, round.Count);   // pátý má volno
    }

    // ── Modulární formát ──────────────────────────────────────────────────────

    [Fact]
    public void Resolve_FallsBackToLegacyEnumWhenJsonMissing()
    {
        var definition = _formats.Resolve(Season(SeasonFormat.DoubleRoundRobin));

        var module = Assert.Single(definition.Modules);
        Assert.Equal(SeasonModuleType.RoundRobin, module.Type);
        Assert.Equal(2, module.MatchesPerPair);
    }

    [Fact]
    public void Resolve_FallsBackWhenJsonIsCorrupted()
    {
        var season = Season(SeasonFormat.RoundRobin);
        season.FormatJson = "{ tohle není JSON";

        var definition = _formats.Resolve(season);

        Assert.Single(definition.Modules);
        Assert.Equal(SeasonModuleType.RoundRobin, definition.Modules[0].Type);
    }

    [Fact]
    public void SerializeAndResolve_RoundTripsModules()
    {
        var original = new SeasonFormatDefinition
        {
            Modules =
            [
                new SeasonFormatModule { Type = SeasonModuleType.GroupStage, GroupCount = 4, AdvanceCount = 2 },
                new SeasonFormatModule { Type = SeasonModuleType.Playoff, BracketSize = 8 }
            ]
        };

        var season = Season();
        season.FormatJson = _formats.Serialize(original);
        var restored = _formats.Resolve(season);

        Assert.Equal(2, restored.Modules.Count);
        Assert.Equal(SeasonModuleType.GroupStage, restored.Modules[0].Type);
        Assert.Equal(4, restored.Modules[0].GroupCount);
        Assert.Equal(SeasonModuleType.Playoff, restored.Modules[1].Type);
        Assert.Equal(8, restored.Modules[1].BracketSize);
    }

    [Fact]
    public void Describe_ProducesReadablePreview()
    {
        var definition = new SeasonFormatDefinition
        {
            Modules =
            [
                new SeasonFormatModule { Type = SeasonModuleType.RoundRobin, MatchesPerPair = 1 },
                new SeasonFormatModule { Type = SeasonModuleType.Playoff, BracketSize = 4 }
            ]
        };

        Assert.Equal("tabulka (každý s každým) → playoff top 4", _formats.Describe(definition));
    }

    [Fact]
    public void GenerateRoundRobin_TwoLegsMirrorHomeAndAway()
    {
        var participants = Participants(4);
        var matches = _generator.GenerateRoundRobin(Season(), participants, matchesPerPair: 2);

        Assert.Equal(12, matches.Count);
        var firstLeg  = matches.Where(m => m.Round <= 3).ToList();
        var secondLeg = matches.Where(m => m.Round > 3).ToList();
        Assert.All(firstLeg, m => Assert.Contains(secondLeg, r =>
            r.HomeParticipantId == m.AwayParticipantId && r.AwayParticipantId == m.HomeParticipantId));
    }

    // ── Pořadí přehrávání ─────────────────────────────────────────────────────

    /// <summary>
    /// ELO se skládá sekvenčně, takže stejná sada zápasů v jiném pořadí dá jiná čísla.
    /// Proto se zápasy musí řadit podle (ModuleIndex, Round, MatchDate) — playoff čísluje
    /// kola znovu od 1 a při řazení jen podle Round by se přehrálo před koncem ligy.
    /// </summary>
    [Fact]
    public void RecomputeSeason_DependsOnMatchOrder()
    {
        var elo = new EloService();

        static (List<SeasonParticipant> Participants, List<Match> Matches) Fixture()
        {
            var a = new SeasonParticipant();
            var b = new SeasonParticipant();
            var c = new SeasonParticipant();

            Match Win(SeasonParticipant home, SeasonParticipant away, int round, int moduleIndex) => new()
            {
                HomeParticipantId = home.Guid,
                AwayParticipantId = away.Guid,
                HomeScore = 1,
                AwayScore = 0,
                Round = round,
                ModuleIndex = moduleIndex,
                Status = MatchStatus.Played
            };

            // Liga (modul 0, kola 1–2) a na ni navazující playoff (modul 1, kolo 1).
            return ([a, b, c],
            [
                Win(a, b, round: 1, moduleIndex: 0),
                Win(b, c, round: 2, moduleIndex: 0),
                Win(c, a, round: 1, moduleIndex: 1)
            ]);
        }

        var correct = Fixture();
        elo.RecomputeSeason(correct.Participants,
            correct.Matches.OrderBy(m => m.ModuleIndex).ThenBy(m => m.Round).ToList());

        var wrong = Fixture();
        elo.RecomputeSeason(wrong.Participants,
            wrong.Matches.OrderBy(m => m.Round).ToList());   // playoff by se dostalo na začátek

        Assert.NotEqual(
            correct.Participants.Select(p => p.EloRating),
            wrong.Participants.Select(p => p.EloRating));
    }
}
