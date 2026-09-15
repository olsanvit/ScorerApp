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

    /// <summary>
    /// Klub, za který účastník v této sezóně nastupuje. Uložené u účastníka, ne odvozené
    /// z hráče — hráč může přestoupit a historická sezóna musí ukazovat klub, za který tehdy hrál.
    /// </summary>
    public Guid? ClubId { get; set; }
    public ScorerApp.Domain.Models.Clubs.Club? Club { get; set; }

    public decimal EloRating { get; set; } = 1000m;

    [NotMapped]
    public string DisplayName => Player?.Name ?? Team?.Name ?? "—";
}
