using SharedServices.Models.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScorerApp.Domain.Models;

public class SeasonParticipant : BaseGuid
{
    public Guid SeasonId { get; set; }
    public Season Season { get; set; } = null!;

    public Guid? PlayerId { get; set; }
    public Player? Player { get; set; }

    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }

    public decimal EloRating { get; set; } = 1000m;

    [NotMapped]
    public string DisplayName => Player?.Name ?? Team?.Name ?? "—";
}
