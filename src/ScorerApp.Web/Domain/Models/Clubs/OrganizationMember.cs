using MercenariesAndBeasts.Infrastructure;
using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

/// <summary>
/// Účet s rolí v organizaci — nese OPRÁVNĚNÍ (kdo smí spravovat oddíl, psát oběžníky).
/// Soupiska oddílu je naproti tomu <see cref="ClubMember"/> nad hráči, kteří účet mít nemusí.
/// </summary>
public class OrganizationMember : BaseGuid
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;

    public OrgRole Role { get; set; } = OrgRole.Member;
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
