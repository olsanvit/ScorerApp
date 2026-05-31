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

public enum SeasonStatus
{
    Planning,
    Active,
    Finished
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
