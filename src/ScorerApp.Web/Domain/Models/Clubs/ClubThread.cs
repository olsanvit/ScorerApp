using MercenariesAndBeasts.Infrastructure;
using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

public class ClubThread : BaseGuid
{
    public Guid ClubId { get; set; }
    public Club Club { get; set; } = null!;

    public string Title { get; set; } = "";
    public ThreadType ThreadType { get; set; } = ThreadType.General;

    public string CreatedByUserId { get; set; } = "";
    public AppUser CreatedByUser { get; set; } = null!;

    public bool IsArchived { get; set; }

    public List<ChatMessage> Messages { get; set; } = new();
    public List<ThreadParticipant> Participants { get; set; } = new();
}
