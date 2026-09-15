using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

public class NotificationPreference : BaseGuid
{
    public string UserId { get; set; } = "";

    public Guid ClubId { get; set; }
    public Club Club { get; set; } = null!;

    public bool EmailEnabled { get; set; } = true;
    public bool NtfyEnabled { get; set; }
    public NotifyMinPriority MinPriority { get; set; } = NotifyMinPriority.High;
}
