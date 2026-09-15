using MercenariesAndBeasts.Infrastructure;
using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

/// <summary>Explicitní účastník vlákna — pro soukromá vlákna; oddílová vlákna vidí všichni členové.</summary>
public class ThreadParticipant : BaseGuid
{
    public Guid ThreadId { get; set; }
    public ClubThread Thread { get; set; } = null!;

    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
}
