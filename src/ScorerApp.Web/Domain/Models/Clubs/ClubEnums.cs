namespace ScorerApp.Domain.Models.Clubs;

/// <summary>
/// Role účtu v organizaci. Hodnoty jsou explicitní a vzestupné, protože oprávnění se
/// porovnávají přes <c>&gt;=</c> (správce organizace smí všechno, co správce oddílu).
/// </summary>
public enum OrgRole
{
    Member      = 0,
    ClubManager = 1,
    OrgAdmin    = 2
}

/// <summary>Typ vlákna určuje prioritu notifikací — Debt se doručuje vždy, bez ohledu na preference.</summary>
public enum ThreadType
{
    General,
    Announcement,
    Debt,
    Event
}

public enum CircularType
{
    General,
    Reminder,
    Debt,
    Event
}

public enum CircularStatus
{
    Draft,
    Sent,
    Cancelled
}

public enum DeliveryStatus
{
    Pending,
    Sent,
    Failed,
    Read
}

public enum ReservationStatus
{
    Pending,
    Approved,
    Rejected,
    Completed,
    Cancelled
}

public enum NotifyMinPriority
{
    All,
    High,
    Urgent
}
