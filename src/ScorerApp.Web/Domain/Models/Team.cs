using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class Team : BaseGuid
{
    public string Name { get; set; } = "";
    public string? ShortName { get; set; }
    public string? Color { get; set; }

    public List<TeamPlayer> TeamPlayers { get; set; } = new();
    public List<SeasonParticipant> SeasonParticipants { get; set; } = new();
}
