using MercenariesAndBeasts.Infrastructure;
using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

/// <summary>Rodič spravuje účet dítěte v rámci organizace (vidí a píše za něj).</summary>
public class FamilyLink : BaseGuid
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string ParentUserId { get; set; } = "";
    public AppUser ParentUser { get; set; } = null!;

    public string ChildUserId { get; set; } = "";
    public AppUser ChildUser { get; set; } = null!;
}
