using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class MatchSet : BaseGuid
{
    public Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;
    public int SetNumber { get; set; }
    public int HomeGames { get; set; }
    public int AwayGames { get; set; }
    public int? TiebreakHome { get; set; }
    public int? TiebreakAway { get; set; }
}
