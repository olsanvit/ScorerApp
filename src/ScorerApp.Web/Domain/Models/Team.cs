using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class Team : BaseGuid
{
    public string Name { get; set; } = "";
    public string? ShortName { get; set; }
    public string? Color { get; set; }

    /// <summary>Klub, za který tým hraje. Volitelné — samostatné partičky bez klubu zůstávají možné.</summary>
    public Guid? ClubId { get; set; }
    public ScorerApp.Domain.Models.Clubs.Club? Club { get; set; }

    public List<TeamPlayer> TeamPlayers { get; set; } = new();
    public List<SeasonParticipant> SeasonParticipants { get; set; } = new();
}
