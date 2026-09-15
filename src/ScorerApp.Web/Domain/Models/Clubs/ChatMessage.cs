using MercenariesAndBeasts.Infrastructure;
using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

public class ChatMessage : BaseGuid
{
    public Guid ThreadId { get; set; }
    public ClubThread Thread { get; set; } = null!;

    public string SenderUserId { get; set; } = "";
    public AppUser SenderUser { get; set; } = null!;

    public string Body { get; set; } = "";
    public DateTimeOffset? EditedAt { get; set; }

    public List<ChatMessageRead> Reads { get; set; } = new();
}
