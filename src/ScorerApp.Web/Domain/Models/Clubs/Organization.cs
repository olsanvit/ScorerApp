using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

/// <summary>Zastřešující organizace (např. TJ Sokol) — sdílí mezi oddíly členy, auta a oběžníky.</summary>
public class Organization : BaseGuid
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Club> Clubs { get; set; } = new();
    public List<OrganizationMember> Members { get; set; } = new();
    public List<Car> Cars { get; set; } = new();
}
