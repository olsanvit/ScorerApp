using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class Race : BaseGuid
{
    public Guid SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public string Name { get; set; } = "";
    public decimal? Distance { get; set; }
    public string? DistanceUnit { get; set; }
    public DateOnly? RaceDate { get; set; }
    public string? Notes { get; set; }

    public List<RaceResult> RaceResults { get; set; } = new();
}
