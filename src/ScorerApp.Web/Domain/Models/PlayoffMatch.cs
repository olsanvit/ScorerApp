using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

/// <summary>
/// Pozice v pavouku. Je to vlastní entita (ne jen Match), protože zápasy pozdějších kol
/// existují dřív, než se zná, kdo je bude hrát — Match vyžaduje oba účastníky, PlayoffMatch ne.
/// Match se k pozici doplní až ve chvíli, kdy jsou známi oba soupeři.
/// </summary>
public class PlayoffMatch : BaseGuid
{
    public Guid SeasonId { get; set; }
    public Season Season { get; set; } = null!;

    /// <summary>1 = první kolo pavouka, dál se zvyšuje až k finále.</summary>
    public int Round { get; set; }

    /// <summary>Pozice v kole (1..N). Vítěz postupuje na pozici ceil(BracketPosition / 2) dalšího kola.</summary>
    public int BracketPosition { get; set; }

    /// <summary>Null, dokud není znám postupující z předchozího kola.</summary>
    public Guid? ParticipantAId { get; set; }
    public SeasonParticipant? ParticipantA { get; set; }

    public Guid? ParticipantBId { get; set; }
    public SeasonParticipant? ParticipantB { get; set; }

    /// <summary>Nasazení pro referenci ve výpisu pavouka (1 = nejlepší z tabulky).</summary>
    public int? SeedA { get; set; }
    public int? SeedB { get; set; }

    public Guid? WinnerId { get; set; }

    /// <summary>Odkaz na zápas se skóre — vznikne, až jsou známi oba účastníci.</summary>
    public Guid? MatchId { get; set; }
    public Match? Match { get; set; }
}
