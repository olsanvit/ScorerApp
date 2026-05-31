using ScorerApp.Domain.Services;

namespace ScorerApp.Tests;

public class EloServiceTests
{
    private readonly EloService _svc = new();

    [Fact]
    public void Calculate_HomeWin_HomeRatingIncreases()
    {
        var (newHome, newAway) = _svc.Calculate(1000m, 1000m, homeResult: 1.0);
        Assert.True(newHome > 1000m);
        Assert.True(newAway < 1000m);
    }

    [Fact]
    public void Calculate_AwayWin_AwayRatingIncreases()
    {
        var (newHome, newAway) = _svc.Calculate(1000m, 1000m, homeResult: 0.0);
        Assert.True(newHome < 1000m);
        Assert.True(newAway > 1000m);
    }

    [Fact]
    public void Calculate_Draw_BothRatingsNearlyUnchanged()
    {
        var (newHome, newAway) = _svc.Calculate(1000m, 1000m, homeResult: 0.5);
        Assert.Equal(1000m, newHome, precision: 1);
        Assert.Equal(1000m, newAway, precision: 1);
    }

    [Fact]
    public void Calculate_SumIsConserved()
    {
        var (newHome, newAway) = _svc.Calculate(1200m, 800m, homeResult: 1.0);
        Assert.Equal(2000m, Math.Round(newHome + newAway, 4));
    }

    [Fact]
    public void Calculate_Upset_LargerRatingChange()
    {
        // Slabší hráč (800) porazí silnějšího (1200) → větší ELO zisk
        var (newHome, newAway) = _svc.Calculate(800m, 1200m, homeResult: 1.0);
        var gainedByWeak  = newHome - 800m;
        var (newHome2, _) = _svc.Calculate(1200m, 800m, homeResult: 1.0);
        var gainedByStrong = newHome2 - 1200m;
        Assert.True(gainedByWeak > gainedByStrong);
    }
}
