using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models;

public class Season : BaseGuid
{
    public Guid LeagueId { get; set; }
    public League League { get; set; } = null!;
    public string Name { get; set; } = "";
    public int Year { get; set; }
    /// <summary>Původní jednoduchý formát. Zůstává kvůli existujícím sezónám — nový modulární
    /// formát je ve FormatJson a má přednost (viz SeasonFormatService).</summary>
    public SeasonFormat Format { get; set; }

    /// <summary>Modulární formát soutěže jako JSON (SeasonFormatDefinition). Null = odvodit z Format.</summary>
    public string? FormatJson { get; set; }
    public SeasonStatus Status { get; set; }
    public bool UseElo { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public List<SeasonParticipant> Participants { get; set; } = new();
    public List<Match> Matches { get; set; } = new();
    public List<Race> Races { get; set; } = new();
    public List<PlayoffMatch> PlayoffMatches { get; set; } = new();
}
