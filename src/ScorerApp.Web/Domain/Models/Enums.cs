namespace ScorerApp.Domain.Models;

public enum SportType
{
    Football,
    IceHockey,
    Basketball,
    Tennis,
    Darts,
    Padel,
    Cards,
    Running,
    Other
}

public enum SportMatchType
{
    HeadToHead,
    MultiParticipant
}

public enum ParticipantKind
{
    Team,
    Individual
}

public enum SeasonFormat
{
    RoundRobin,
    DoubleRoundRobin,
    Playoff,
    GroupsAndKnockout,
    Custom
}

/// <summary>
/// Životní cyklus sezóny: Draft → Registration → InProgress → Completed.
/// Číselné hodnoty jsou explicitní a záměrně mimo logické pořadí — sloupec v DB
/// je int a původní stavy (Planning=0, Active=1, Finished=2) musí zůstat platné,
/// jinak by se existující sezóny po nasazení posunuly do špatného stavu.
/// Pro řazení a zobrazení používej <see cref="SeasonStatusExtensions.SortOrder"/>.
/// </summary>
public enum SeasonStatus
{
    /// <summary>Sezóna založena, registrace ještě neotevřena.</summary>
    Draft = 0,
    /// <summary>Přidávají se účastníci, zápasy ještě nejsou vygenerované.</summary>
    Registration = 3,
    /// <summary>Rozpis vygenerován, zapisují se výsledky.</summary>
    InProgress = 1,
    /// <summary>Vše odehráno.</summary>
    Completed = 2
}

public static class SeasonStatusExtensions
{
    public static int SortOrder(this SeasonStatus s) => s switch
    {
        SeasonStatus.Draft        => 0,
        SeasonStatus.Registration => 1,
        SeasonStatus.InProgress   => 2,
        SeasonStatus.Completed    => 3,
        _                         => 9
    };

    public static string Label(this SeasonStatus s) => s switch
    {
        SeasonStatus.Draft        => "Návrh",
        SeasonStatus.Registration => "Registrace",
        SeasonStatus.InProgress   => "Probíhá",
        SeasonStatus.Completed    => "Dokončena",
        _                         => s.ToString()
    };

    public static string Badge(this SeasonStatus s) => s switch
    {
        SeasonStatus.Draft        => "bg-secondary",
        SeasonStatus.Registration => "bg-info text-dark",
        SeasonStatus.InProgress   => "bg-success",
        SeasonStatus.Completed    => "bg-dark",
        _                         => "bg-secondary"
    };

    /// <summary>Účastníky lze přidávat/odebírat jen před vygenerováním rozpisu.</summary>
    public static bool AllowsParticipantChanges(this SeasonStatus s) =>
        s is SeasonStatus.Draft or SeasonStatus.Registration;
}

/// <summary>
/// Fáze soutěže, ve které se zápas hraje. Do tabulky pořadí se počítají jen League a Group —
/// Swiss má vlastní průběžnou tabulku a Playoff se do tabulky nepočítá vůbec.
/// Hodnoty jsou explicitní: existující zápasy mají v DB 0 = League.
/// </summary>
public enum MatchStage
{
    League  = 0,
    Group   = 1,
    Swiss   = 2,
    Playoff = 3
}

/// <summary>Modul, ze kterého se skládá formát sezóny (viz SeasonFormatDefinition).</summary>
public enum SeasonModuleType
{
    RoundRobin,
    GroupStage,
    Swiss,
    Playoff
}

public enum MatchStatus
{
    Scheduled,
    Played,
    Cancelled,
    Postponed
}

public enum MatchEventType
{
    Goal,
    Assist,
    YellowCard,
    RedCard,
    PenaltyGoal,
    OwnGoal,
    CleanSheet,
    PlusMinus,
    PenaltyMissed,
    Checkout180,
    HighCheckout,
    LegWon,
    Ace,
    DoubleFault,
    SetResult,
    Other
}
