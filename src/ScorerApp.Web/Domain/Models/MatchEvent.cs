using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class MatchEvent : BaseGuid
{
    public Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;
    public int? Minute { get; set; }
    public MatchEventType Type { get; set; }
    public Guid? PlayerId { get; set; }
    public Player? Player { get; set; }
    public int? Value { get; set; }
    public string? Notes { get; set; }
}
