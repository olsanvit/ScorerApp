using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

public class Car : BaseGuid
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Name { get; set; } = "";
    public string? LicensePlate { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public bool IsActive { get; set; } = true;

    public List<CarReservation> Reservations { get; set; } = new();
}
