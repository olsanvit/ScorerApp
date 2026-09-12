using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class Player : BaseGuid
{
    public string Name { get; set; } = "";
    public string? Nickname { get; set; }
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>E-mail hráče — podle něj se hráč páruje s přihlášeným účtem (stránka /profile).</summary>
    public string? Email { get; set; }

    /// <summary>Id spárovaného účtu (AppUser). Vyplní se automaticky při shodě e-mailu.</summary>
    public string? UserId { get; set; }

    public List<TeamPlayer> TeamPlayers { get; set; } = new();
    public List<SeasonParticipant> SeasonParticipants { get; set; } = new();
}
