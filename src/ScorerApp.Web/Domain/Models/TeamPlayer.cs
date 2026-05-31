using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class TeamPlayer : BaseGuid
{
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public string? Position { get; set; }
    public DateOnly? JoinedDate { get; set; }
    public DateOnly? LeftDate { get; set; }
}
