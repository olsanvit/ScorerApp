using ScorerApp.Domain.Models;
using ScorerApp.Domain.Services;

namespace ScorerApp.Tests;

public class ScoringRulesServiceTests
{
    private readonly ScoringRulesService _svc = new();

    private static Sport FootballSport() => new Sport
    {
        Name = "Football",
        ScoringRulesJson = """{"win":3,"draw":1,"loss":0,"winOT":null,"lossOT":null,"hasDraw":true}"""
    };

    private static Sport HockeySport() => new Sport
    {
        Name = "Ice Hockey",
        ScoringRulesJson = """{"win":3,"draw":null,"loss":0,"winOT":2,"lossOT":1,"hasDraw":false}"""
    };

    [Fact]
    public void Resolve_Football_DefaultRules()
    {
        var rules = _svc.Resolve(FootballSport());
        Assert.Equal(3, rules.Win);
        Assert.Equal(1, rules.Draw);
        Assert.Equal(0, rules.Loss);
        Assert.True(rules.HasDraw);
        Assert.Null(rules.WinOT);
    }

    [Fact]
    public void Resolve_Hockey_HasOvertimeRules()
    {
        var rules = _svc.Resolve(HockeySport());
        Assert.Equal(2, rules.WinOT);
        Assert.Equal(1, rules.LossOT);
        Assert.False(rules.HasDraw);
    }

    [Fact]
    public void Resolve_LeagueOverride_UsesOverrideJson()
    {
        var sport  = FootballSport();
        var league = new League
        {
            SportId = sport.Guid,
            Sport   = sport,
            ScoringRulesOverrideJson = """{"win":2,"draw":0,"loss":0,"hasDraw":false}"""
        };
        var rules = _svc.Resolve(sport, league);
        Assert.Equal(2, rules.Win);
        Assert.Equal(0, rules.Draw);
        Assert.False(rules.HasDraw);
    }

    [Fact]
    public void Resolve_EmptyOverride_FallsBackToSport()
    {
        var sport  = FootballSport();
        var league = new League { SportId = sport.Guid, Sport = sport, ScoringRulesOverrideJson = null };
        var rules  = _svc.Resolve(sport, league);
        Assert.Equal(3, rules.Win);
    }
}
