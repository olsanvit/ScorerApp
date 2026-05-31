using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class League : BaseGuid
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public Guid SportId { get; set; }
    public Sport Sport { get; set; } = null!;
    public string? ScoringRulesOverrideJson { get; set; }

    public List<Season> Seasons { get; set; } = new();
}
