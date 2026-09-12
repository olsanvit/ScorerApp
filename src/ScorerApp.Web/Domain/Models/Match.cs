using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class Match : BaseGuid
{
    public Guid SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public int Round { get; set; }

    /// <summary>Fáze soutěže — odděluje ligové zápasy od skupin a playoff (tabulka je počítá různě).</summary>
    public MatchStage Stage { get; set; } = MatchStage.League;

    /// <summary>Index modulu formátu, který zápas vygeneroval — kvůli víc fázím za sebou.</summary>
    public int ModuleIndex { get; set; }

    /// <summary>Číslo skupiny (1..N) u skupinové fáze; jinak null.</summary>
    public int? GroupIndex { get; set; }

    public Guid HomeParticipantId { get; set; }
    public SeasonParticipant HomeParticipant { get; set; } = null!;

    public Guid AwayParticipantId { get; set; }
    public SeasonParticipant AwayParticipant { get; set; } = null!;

    public DateOnly? MatchDate { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;

    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public bool? ExtraTime { get; set; }
    public bool? Penalties { get; set; }
    public int? HomePenaltyScore { get; set; }
    public int? AwayPenaltyScore { get; set; }
    public string? Notes { get; set; }

    public List<MatchEvent> MatchEvents { get; set; } = new();
    public List<MatchSet> MatchSets { get; set; } = new();
}
