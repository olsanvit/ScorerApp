namespace ScorerApp.Domain.Services;

public class EloService
{
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
}
