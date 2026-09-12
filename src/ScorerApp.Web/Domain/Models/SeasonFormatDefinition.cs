namespace ScorerApp.Domain.Models;

/// <summary>
/// Modulární formát soutěže — uložený na sezóně jako JSON (Season.FormatJson).
/// Moduly se hrají v pořadí, v jakém jsou v seznamu; další fáze se generuje až po dohrání předchozí.
/// </summary>
public class SeasonFormatDefinition
{
    public List<SeasonFormatModule> Modules { get; set; } = new();
}

/// <summary>
/// Jeden modul formátu. Parametry jsou nullable, protože každý typ modulu používá jen některé —
/// společná třída se drží kvůli jednoduché (de)serializaci celého seznamu.
/// </summary>
public class SeasonFormatModule
{
    public SeasonModuleType Type { get; set; }

    /// <summary>RoundRobin: kolikrát se každá dvojice potká (1 = jednokolově, 2 = doma i venku).</summary>
    public int? MatchesPerPair { get; set; }

    /// <summary>GroupStage: počet skupin.</summary>
    public int? GroupCount { get; set; }

    /// <summary>GroupStage: kolik účastníků z každé skupiny postupuje dál.</summary>
    public int? AdvanceCount { get; set; }

    /// <summary>Swiss: počet kol.</summary>
    public int? Rounds { get; set; }

    /// <summary>Playoff: velikost pavouka (4/8/16/32). Null = všichni dostupní účastníci.</summary>
    public int? BracketSize { get; set; }
}
