using MercenariesAndBeasts.Infrastructure;
using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

/// <summary>
/// Oběžník — jednosměrná zpráva organizace nebo oddílu s evidencí doručení a přečtení.
/// Na rozdíl od chatu se na něj neodpovídá; typ Debt slouží k evidenci nedoplatků.
/// </summary>
public class Circular : BaseGuid
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    /// <summary>Null = celá organizace.</summary>
    public Guid? ClubId { get; set; }
    public Club? Club { get; set; }

    public string SenderUserId { get; set; } = "";
    public AppUser SenderUser { get; set; } = null!;

    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";

    public CircularType Type { get; set; } = CircularType.General;
    public CircularStatus Status { get; set; } = CircularStatus.Draft;

    public bool SendEmail { get; set; } = true;
    public bool SendNtfy { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    public List<CircularRecipient> Recipients { get; set; } = new();
}
