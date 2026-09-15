using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

public class ChatMessageRead : BaseGuid
{
    public Guid MessageId { get; set; }
    public ChatMessage Message { get; set; } = null!;

    public string UserId { get; set; } = "";
    public DateTimeOffset ReadAt { get; set; } = DateTimeOffset.UtcNow;
}
