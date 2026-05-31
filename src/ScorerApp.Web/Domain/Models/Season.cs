using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class Season : BaseGuid
{
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;
    public string Name { get; set; } = "";
    public int Year { get; set; }
    public SeasonFormat Format { get; set; }
    public SeasonStatus Status { get; set; }
    public bool UseElo { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public List<SeasonParticipant> Participants { get; set; } = new();
    public List<Match> Matches { get; set; } = new();
    public List<Race> Races { get; set; } = new();
}
