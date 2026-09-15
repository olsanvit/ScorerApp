using SharedServices.Models.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScorerApp.Domain.Models.Clubs;

public class Invitation : BaseGuid
{
    public string Email { get; set; } = "";

    public Guid ClubId { get; set; }
    public Club Club { get; set; } = null!;

    public OrgRole Role { get; set; } = OrgRole.Member;
    public string InvitedByUserId { get; set; } = "";

    /// <summary>Náhodný token v odkazu — Guid bez pomlček, aby nešel odhadnout z pořadí záznamů.</summary>
    public string Token { get; set; } = Guid.NewGuid().ToString("N");

    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddDays(7);
    public DateTimeOffset? AcceptedAt { get; set; }

    [NotMapped] public bool IsExpired  => DateTimeOffset.UtcNow > ExpiresAt;
    [NotMapped] public bool IsAccepted => AcceptedAt.HasValue;
}
