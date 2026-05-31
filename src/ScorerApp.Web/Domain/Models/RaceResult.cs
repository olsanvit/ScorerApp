using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class RaceResult : BaseGuid
{
    public Guid RaceId { get; set; }
    public Race Race { get; set; } = null!;
    public Guid ParticipantId { get; set; }
    public SeasonParticipant Participant { get; set; } = null!;
    public int? Position { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public bool Dnf { get; set; }
    public string? Notes { get; set; }
}
