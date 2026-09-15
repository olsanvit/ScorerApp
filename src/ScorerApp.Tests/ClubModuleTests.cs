using ScorerApp.Domain.Models.Clubs;
using ScorerApp.Domain.Services.Clubs;

namespace ScorerApp.Tests;

public class ClubModuleTests
{
    // ── Rezervace aut: překryv celých dnů ─────────────────────────────────────

    private static DateOnly D(int day) => new(2026, 9, day);

    [Theory]
    [InlineData(10, 12, 12, 14, true)]   // auto vrácené v pátek nejde od pátku znovu půjčit
    [InlineData(10, 12, 13, 14, false)]  // navazující den je volný
    [InlineData(10, 20, 12, 14, true)]   // rezervace uvnitř jiné
    [InlineData(12, 14, 10, 20, true)]   // opačné pořadí argumentů
    [InlineData(10, 10, 10, 10, true)]   // jednodenní na stejný den
    [InlineData(15, 16, 10, 14, false)]  // úplně po
    public void Overlaps_TreatsBothEndDaysAsBooked(int aFrom, int aTo, int bFrom, int bTo, bool expected) =>
        Assert.Equal(expected, CarReservation.Overlaps(D(aFrom), D(aTo), D(bFrom), D(bTo)));

    [Fact]
    public void Days_CountsBothEndDays() =>
        Assert.Equal(3, new CarReservation { DateFrom = D(10), DateTo = D(12) }.Days);

    [Theory]
    [InlineData(ReservationStatus.Pending, true)]
    [InlineData(ReservationStatus.Approved, true)]
    [InlineData(ReservationStatus.Completed, true)]
    [InlineData(ReservationStatus.Rejected, false)]
    [InlineData(ReservationStatus.Cancelled, false)]
    public void BlocksCar_ReleasesTermOnlyForRejectedAndCancelled(ReservationStatus status, bool expected) =>
        Assert.Equal(expected, new CarReservation { Status = status }.BlocksCar);

    [Fact]
    public void KmDriven_IsNullUntilBothReadingsExist()
    {
        Assert.Null(new CarReservation { KmAtStart = 1000m }.KmDriven);
        Assert.Equal(250m, new CarReservation { KmAtStart = 1000m, KmAtEnd = 1250m }.KmDriven);
    }

    // ── Notifikace chatu: priorita a kanály ───────────────────────────────────

    [Theory]
    [InlineData(ThreadType.Debt, NotifyMinPriority.Urgent)]
    [InlineData(ThreadType.Announcement, NotifyMinPriority.High)]
    [InlineData(ThreadType.Event, NotifyMinPriority.High)]
    [InlineData(ThreadType.General, NotifyMinPriority.All)]
    public void PriorityOf_MapsThreadTypes(ThreadType type, NotifyMinPriority expected) =>
        Assert.Equal(expected, ChatNotificationDispatcher.PriorityOf(type));

    [Fact]
    public void Channels_DebtAlwaysEmailsEvenWhenUserDisabledEmail()
    {
        var pref = new NotificationPreference { EmailEnabled = false, NtfyEnabled = false, MinPriority = NotifyMinPriority.Urgent };
        Assert.Equal((true, false), ChatNotificationDispatcher.Channels(ThreadType.Debt, pref));
    }

    [Fact]
    public void Channels_WithoutPreferenceEmailsOnlyImportantThreads()
    {
        Assert.Equal((false, false), ChatNotificationDispatcher.Channels(ThreadType.General, null));
        Assert.Equal((true, false), ChatNotificationDispatcher.Channels(ThreadType.Announcement, null));
    }

    [Fact]
    public void Channels_RespectsMinPriority()
    {
        var urgentOnly = new NotificationPreference { EmailEnabled = true, NtfyEnabled = true, MinPriority = NotifyMinPriority.Urgent };
        Assert.Equal((false, false), ChatNotificationDispatcher.Channels(ThreadType.Event, urgentOnly));

        var everything = new NotificationPreference { EmailEnabled = false, NtfyEnabled = true, MinPriority = NotifyMinPriority.All };
        Assert.Equal((false, true), ChatNotificationDispatcher.Channels(ThreadType.General, everything));
    }

    // ── Drobnosti ─────────────────────────────────────────────────────────────

    [Fact]
    public void UserTopic_UsesCompactPrefixAndSurvivesShortIds()
    {
        Assert.Equal("scorerapp-1a2b3c4d", ClubNotificationService.UserTopic("1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d"));
        Assert.Equal("scorerapp-abc", ClubNotificationService.UserTopic("abc"));
    }

    [Fact]
    public void NewJoinCode_IsEightUppercaseHexCharactersAndRandom()
    {
        var a = Club.NewJoinCode();
        var b = Club.NewJoinCode();

        Assert.Matches("^[0-9A-F]{8}$", a);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void OrgRole_IsOrderedSoThatComparisonsGrantHigherRoles()
    {
        // ClubAccessService porovnává role přes >= — pořadí hodnot je součást kontraktu.
        Assert.True(OrgRole.OrgAdmin > OrgRole.ClubManager);
        Assert.True(OrgRole.ClubManager > OrgRole.Member);
    }

    [Fact]
    public void Invitation_DefaultsToSevenDayUnguessableToken()
    {
        var inv = new Invitation();

        Assert.Matches("^[0-9a-f]{32}$", inv.Token);
        Assert.InRange(inv.ExpiresAt - DateTimeOffset.UtcNow, TimeSpan.FromDays(6.99), TimeSpan.FromDays(7.01));
        Assert.False(inv.IsExpired);
        Assert.False(inv.IsAccepted);
    }
}
