using MercenariesAndBeasts.Infrastructure;
using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

/// <summary>
/// Rodič (účet) propojený s hráčem na soupisce. Vazba je na <see cref="Player"/>, ne na účet dítěte —
/// děti na soupisce účet většinou nemají a právě za ně rodič dostává oddílové oběžníky.
/// OrganizationId omezuje propojení na jednu organizaci: hráč může hrát i jinde.
/// </summary>
public class FamilyLink : BaseGuid
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string ParentUserId { get; set; } = "";
    public AppUser ParentUser { get; set; } = null!;

    public Guid ChildPlayerId { get; set; }
    public Player ChildPlayer { get; set; } = null!;
}
