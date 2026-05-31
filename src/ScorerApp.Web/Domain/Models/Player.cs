using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class Player : BaseGuid
{
    public string Name { get; set; } = "";
    public string? Nickname { get; set; }
    public DateOnly? DateOfBirth { get; set; }

    public List<TeamPlayer> TeamPlayers { get; set; } = new();
    public List<SeasonParticipant> SeasonParticipants { get; set; } = new();
}
