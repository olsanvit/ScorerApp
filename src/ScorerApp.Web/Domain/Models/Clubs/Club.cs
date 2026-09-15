using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

/// <summary>Oddíl/klub. Do soutěží posílá své týmy, případně hráče, kteří za něj nastupují.</summary>
public class Club : BaseGuid
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Name { get; set; } = "";
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Kód pro vstup do oddílu přes /join. Regenerací přestanou staré kódy fungovat.</summary>
    public string JoinCode { get; set; } = NewJoinCode();

    public List<ClubMember> Members { get; set; } = new();
    public List<Team> Teams { get; set; } = new();
    public List<ClubThread> Threads { get; set; } = new();
    public List<Invitation> Invitations { get; set; } = new();

    public static string NewJoinCode() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
}
