using SharedServices.Models.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScorerApp.Domain.Models;

/// <summary>
/// Trvalý rating hráče (nebo týmu) v jednom sportu. Sezónní ELO na SeasonParticipant končí
/// se sezónou — tohle je průběžný rating přes všechny sezóny daného sportu a slouží
/// i jako nasazení do pavouka a podklad pro celkový vážený průměr na profilu.
/// </summary>
public class SportRating : BaseGuid
{
    public Guid SportId { get; set; }
    public Sport Sport { get; set; } = null!;

    /// <summary>Vyplněn buď PlayerId, nebo TeamId — podle toho, kdo v daném sportu soutěží.</summary>
    public Guid? PlayerId { get; set; }
    public Player? Player { get; set; }

    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }

    public decimal Rating { get; set; } = 1000m;

    /// <summary>Počet odehraných zápasů — váha tohoto sportu v celkovém průměru.</summary>
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }

    [NotMapped]
    public string DisplayName => Player?.Name ?? Team?.Name ?? "—";
}
