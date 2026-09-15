using MercenariesAndBeasts.Infrastructure;
using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

public class CircularRecipient : BaseGuid
{
    public Guid CircularId { get; set; }
    public Circular Circular { get; set; } = null!;

    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;

    public DeliveryStatus EmailStatus { get; set; } = DeliveryStatus.Pending;
    public DeliveryStatus NtfyStatus { get; set; } = DeliveryStatus.Pending;

    public DateTimeOffset? ReadAt { get; set; }
}
