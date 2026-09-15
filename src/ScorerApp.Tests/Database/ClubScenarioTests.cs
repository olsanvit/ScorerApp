using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScorerApp.Data;
using ScorerApp.Domain.Models;
using ScorerApp.Domain.Models.Clubs;
using ScorerApp.Domain.Services.Clubs;

namespace ScorerApp.Tests.Database;

/// <summary>
/// Scénáře klubového modulu nad skutečnou databází. Každý test si založí vlastní organizaci
/// a účty, aby na sobě testy nezávisely a šly pouštět v libovolném pořadí.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class ClubScenarioTests(DatabaseTestFactory factory)
{
    /// <summary>Admin = zakladatel organizace (OrgAdmin), Manager = správce oddílu, Member = vstoupil kódem.</summary>
    private sealed record World(string Admin, string Manager, string Member, string Outsider, Guid OrgId, Guid ClubId, string JoinCode);

    private static T Get<T>(IServiceScope scope) where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

    private async Task<World> CreateWorldAsync()
    {
        var admin = await factory.CreateUserAsync("admin");
        var manager = await factory.CreateUserAsync("manager");
        var member = await factory.CreateUserAsync("member");
        var outsider = await factory.CreateUserAsync("outsider");

        using var scope = factory.Services.CreateScope();
        var clubs = Get<ClubService>(scope);
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var org = await clubs.CreateOrganizationAsync($"Org {suffix}", null, admin, isSiteAdmin: true);
        // Oddíl zakládá správce organizace bez admin práv aplikace — ověřuje, že zakladatel dostal OrgAdmin.
        var club = await clubs.CreateClubAsync(org.Guid, $"Oddil {suffix}", "ODD", null, admin, isSiteAdmin: false);
        await clubs.AddOrganizationMemberByEmailAsync(org.Guid, await factory.EmailOfAsync(manager), OrgRole.ClubManager, null, admin, false);
        await Get<InvitationService>(scope).JoinByCodeAsync(club.JoinCode, member);

        return new World(admin, manager, member, outsider, org.Guid, club.Guid, club.JoinCode);
    }

    [Fact]
    public async Task Migrations_ApplyOnCleanTestDatabase()
    {
        using var scope = factory.Services.CreateScope();
        await using var db = await Get<IDbContextFactory<AppDbContext>>(scope).CreateDbContextAsync();

        // Pojistka, že testy neběží nad vývojovou DB — jinak by do ní zapisovaly testovací data.
        Assert.Equal(DatabaseTestFactory.TestDatabase, db.Database.GetDbConnection().Database);

        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.Contains(applied, m => m.EndsWith("_ModularFormatsPlayoffRanking"));
        Assert.Contains(applied, m => m.EndsWith("_AddClubsModule"));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task Organization_OnlySiteAdminCreates_AndCreatorBecomesOrgAdmin()
    {
        var creator = await factory.CreateUserAsync("creator");
        var other = await factory.CreateUserAsync("other");
        using var scope = factory.Services.CreateScope();
        var clubs = Get<ClubService>(scope);
        var access = Get<ClubAccessService>(scope);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            clubs.CreateOrganizationAsync("Nepovolena", null, other, isSiteAdmin: false));

        var org = await clubs.CreateOrganizationAsync($"Org {Guid.NewGuid():N}", null, creator, isSiteAdmin: true);
        Assert.Equal(OrgRole.OrgAdmin, await access.GetOrgRoleAsync(org.Guid, creator));
        Assert.True(await access.CanManageOrganizationAsync(org.Guid, creator, isSiteAdmin: false));
    }

    [Fact]
    public async Task ClubManager_ManagesClubButNotOrganization()
    {
        var w = await CreateWorldAsync();
        using var scope = factory.Services.CreateScope();
        var access = Get<ClubAccessService>(scope);
        var clubs = Get<ClubService>(scope);
        var invitations = Get<InvitationService>(scope);

        Assert.True(await access.CanManageClubAsync(w.ClubId, w.Manager, false));
        Assert.False(await access.CanManageOrganizationAsync(w.OrgId, w.Manager, false));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            clubs.CreateClubAsync(w.OrgId, "Dalsi oddil", null, null, w.Manager, false));
        // Správce oddílu nesmí přes pozvánku rozdat vyšší roli, než sám má.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            invitations.CreateInvitationAsync("boss@test.local", w.ClubId, OrgRole.OrgAdmin, w.Manager, false));

        var invitation = await invitations.CreateInvitationAsync("hrac@test.local", w.ClubId, OrgRole.Member, w.Manager, false);
        Assert.Equal(32, invitation.Token.Length);
    }

    [Fact]
    public async Task Outsider_CannotManageOrReadProtectedClubData()
    {
        var w = await CreateWorldAsync();
        using var scope = factory.Services.CreateScope();
        var clubs = Get<ClubService>(scope);
        var chat = Get<ChatService>(scope);
        var cars = Get<CarReservationService>(scope);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            clubs.UpdateClubAsync(w.ClubId, "Prevzato", null, null, true, w.Outsider, false));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            clubs.AddPlayerByNameAsync(w.ClubId, "Vetrelec", w.Outsider, false));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => cars.GetCarsAsync(w.OrgId, w.Outsider, false));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => cars.GetReservationsAsync(w.OrgId, w.Outsider, false));

        var thread = await chat.CreateThreadAsync(w.ClubId, "Interni", ThreadType.General, w.Manager, false);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => chat.GetMessagesAsync(thread.Guid, w.Outsider, false));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => chat.SendMessageAsync(thread.Guid, w.Outsider, false, "ahoj"));
    }

    [Fact]
    public async Task JoinByCode_AddsLinkedPlayerToRoster_AndRegenerationInvalidatesOldCode()
    {
        var w = await CreateWorldAsync();
        using var scope = factory.Services.CreateScope();
        var clubs = Get<ClubService>(scope);
        var invitations = Get<InvitationService>(scope);
        var access = Get<ClubAccessService>(scope);

        var detail = await clubs.GetClubDetailAsync(w.ClubId);
        Assert.Contains(detail!.Roster, m => m.Player.UserId == w.Member);
        Assert.Equal(OrgRole.Member, await access.GetOrgRoleAsync(w.OrgId, w.Member));
        Assert.True(await access.IsClubParticipantAsync(w.ClubId, w.Member, false));

        Assert.Null(await invitations.JoinByCodeAsync("NEPLATNY", w.Outsider));

        var newCode = await clubs.RegenerateJoinCodeAsync(w.ClubId, w.Manager, false);
        Assert.NotEqual(w.JoinCode, newCode);
        Assert.Null(await invitations.JoinByCodeAsync(w.JoinCode, w.Outsider));
        // Kód se zadává ručně — malá písmena musí projít.
        Assert.NotNull(await invitations.JoinByCodeAsync(newCode.ToLowerInvariant(), w.Outsider));
    }

    [Fact]
    public async Task Invitation_AcceptedOnlyByInvitedEmail_AndNeverDowngradesRole()
    {
        var w = await CreateWorldAsync();
        using var scope = factory.Services.CreateScope();
        var invitations = Get<InvitationService>(scope);
        var access = Get<ClubAccessService>(scope);

        var outsiderEmail = await factory.EmailOfAsync(w.Outsider);
        var invite = await invitations.CreateInvitationAsync(outsiderEmail.ToUpperInvariant(), w.ClubId, OrgRole.Member, w.Manager, false);

        Assert.Equal(InvitationAcceptResult.EmailMismatch, await invitations.AcceptAsync(invite.Token, w.Member));
        Assert.Equal(InvitationAcceptResult.Accepted, await invitations.AcceptAsync(invite.Token, w.Outsider));
        Assert.Equal(InvitationAcceptResult.NotFound, await invitations.AcceptAsync(invite.Token, w.Outsider));
        Assert.True(await access.IsClubParticipantAsync(w.ClubId, w.Outsider, false));

        var downgrade = await invitations.CreateInvitationAsync(await factory.EmailOfAsync(w.Manager), w.ClubId, OrgRole.Member, w.Admin, false);
        Assert.Equal(InvitationAcceptResult.Accepted, await invitations.AcceptAsync(downgrade.Token, w.Manager));
        Assert.Equal(OrgRole.ClubManager, await access.GetOrgRoleAsync(w.OrgId, w.Manager));
    }

    [Fact]
    public async Task Roster_ReusesPlayerByName_AndReactivatesRemovedMember()
    {
        var w = await CreateWorldAsync();
        using var scope = factory.Services.CreateScope();
        var clubs = Get<ClubService>(scope);
        var name = $"Hrac {Guid.NewGuid():N}"[..14];

        Assert.False(await clubs.AddPlayerByNameAsync(w.ClubId, name, w.Manager, false));
        Assert.True(await clubs.AddPlayerByNameAsync(w.ClubId, name.ToUpperInvariant(), w.Manager, false));

        var entry = Assert.Single((await clubs.GetClubDetailAsync(w.ClubId))!.Roster, m => m.Player.Name == name);

        await clubs.RemoveFromRosterAsync(entry.Guid, w.Manager, false);
        Assert.DoesNotContain((await clubs.GetClubDetailAsync(w.ClubId))!.Roster, m => m.PlayerId == entry.PlayerId);

        await clubs.AddPlayerToRosterAsync(w.ClubId, entry.PlayerId, w.Manager, false);
        var again = Assert.Single((await clubs.GetClubDetailAsync(w.ClubId))!.Roster, m => m.PlayerId == entry.PlayerId);
        // Stejný záznam znovu aktivní, ne nový řádek — unikátní index (ClubId, PlayerId) by jinak nedovolil.
        Assert.Equal(entry.Guid, again.Guid);
    }

    [Fact]
    public async Task LastOrgAdmin_CannotBeDemotedOrDeactivated()
    {
        var w = await CreateWorldAsync();
        using var scope = factory.Services.CreateScope();
        var clubs = Get<ClubService>(scope);

        var members = await clubs.GetOrganizationMembersAsync(w.OrgId, w.Admin, false);
        var admin = members.Single(m => m.UserId == w.Admin);
        var manager = members.Single(m => m.UserId == w.Manager);

        await Assert.ThrowsAsync<InvalidOperationException>(() => clubs.ChangeRoleAsync(admin.Guid, OrgRole.Member, w.Admin, false));
        await Assert.ThrowsAsync<InvalidOperationException>(() => clubs.SetOrganizationMemberActiveAsync(admin.Guid, false, w.Admin, false));

        await clubs.ChangeRoleAsync(manager.Guid, OrgRole.OrgAdmin, w.Admin, false);
        await clubs.ChangeRoleAsync(admin.Guid, OrgRole.Member, w.Admin, false);
    }

    [Fact]
    public async Task Chat_DeliversLive_TracksUnread_AndRespectsRoles()
    {
        var w = await CreateWorldAsync();
        using var scope = factory.Services.CreateScope();
        var chat = Get<ChatService>(scope);
        var broadcaster = factory.Services.GetRequiredService<ClubChatBroadcaster>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            chat.CreateThreadAsync(w.ClubId, "Od clena", ThreadType.General, w.Member, false));
        var thread = await chat.CreateThreadAsync(w.ClubId, "Trenink", ThreadType.Event, w.Manager, false);

        var delivered = new TaskCompletionSource<ChatMessageDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (broadcaster.Subscribe(thread.Guid, m => { delivered.TrySetResult(m); return Task.CompletedTask; }))
        {
            await chat.SendMessageAsync(thread.Guid, w.Member, false, "  Prijdu v 18:00  ");
            var live = await delivered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal("Prijdu v 18:00", live.Body);
            Assert.Equal(w.Member, live.SenderUserId);
        }

        Assert.Equal(1, (await chat.GetUnreadCountsAsync(w.Manager, false)).GetValueOrDefault(thread.Guid));
        // Vlastní zpráva se odesílateli nepočítá jako nepřečtená.
        Assert.Equal(0, (await chat.GetUnreadCountsAsync(w.Member, false)).GetValueOrDefault(thread.Guid));
        await chat.MarkThreadReadAsync(thread.Guid, w.Manager);
        Assert.Equal(0, (await chat.GetUnreadCountsAsync(w.Manager, false)).GetValueOrDefault(thread.Guid));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => chat.ArchiveThreadAsync(thread.Guid, w.Member, false));
        await chat.ArchiveThreadAsync(thread.Guid, w.Manager, false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => chat.SendMessageAsync(thread.Guid, w.Member, false, "pozde"));
    }

    [Fact]
    public async Task Circular_ReachesClubAccounts_AndHonoursVisibility()
    {
        var w = await CreateWorldAsync();
        using var scope = factory.Services.CreateScope();
        var circulars = Get<CircularService>(scope);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            circulars.SendAsync(w.OrgId, null, w.Manager, false, "Vsem", "text", CircularType.General, false, false));

        var (id, recipients) = await circulars.SendAsync(
            w.OrgId, w.ClubId, w.Manager, false, "Prispevky", "Zaplatte do patku", CircularType.Debt, sendEmail: false, sendNtfy: false);

        var detail = await circulars.GetCircularAsync(id, w.Member, false);
        var ids = detail.Circular!.Recipients.Select(r => r.UserId).ToHashSet();
        Assert.Equal(recipients, ids.Count);
        // Účty oddílu = hráči ze soupisky s účtem + správci (vč. správce organizace); outsider ne.
        Assert.Contains(w.Member, ids);
        Assert.Contains(w.Manager, ids);
        Assert.Contains(w.Admin, ids);
        Assert.DoesNotContain(w.Outsider, ids);

        Assert.True((await circulars.GetCircularAsync(id, w.Outsider, false)).Forbidden);
        Assert.Contains(await circulars.GetCircularsAsync(w.OrgId, null, w.Member, false), c => c.Guid == id);
        Assert.Empty(await circulars.GetCircularsAsync(w.OrgId, null, w.Outsider, false));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => circulars.GetDebtSummariesAsync(w.OrgId, w.Member, false));
        await circulars.MarkReadAsync(id, w.Member);
        var debt = Assert.Single(await circulars.GetDebtSummariesAsync(w.OrgId, w.Manager, false), d => d.Id == id);
        Assert.Equal(1, debt.ReadCount);
        Assert.Equal(recipients - 1, debt.UnreadCount);
    }

    [Fact]
    public async Task CarReservation_BlocksOverlapIncludingEndDay_AndRechecksOnRestore()
    {
        var w = await CreateWorldAsync();
        using var scope = factory.Services.CreateScope();
        var cars = Get<CarReservationService>(scope);
        static DateOnly Day(int day) => new(2030, 5, day);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            cars.SaveCarAsync(new Car { OrganizationId = w.OrgId, Name = "Cizi" }, w.Manager, false));
        var car = await cars.SaveCarAsync(new Car { OrganizationId = w.OrgId, Name = "Octavia" }, w.Admin, false);

        var first = await cars.CreateReservationAsync(car.Guid, w.Member, false, Day(10), Day(12), "turnaj", 1000m);
        // Auto vrácené 12. se nesmí 12. znovu půjčit — oba krajní dny jsou obsazené.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cars.CreateReservationAsync(car.Guid, w.Manager, false, Day(12), Day(14), null, null));
        await cars.CreateReservationAsync(car.Guid, w.Manager, false, Day(13), Day(14), null, null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            cars.CreateReservationAsync(car.Guid, w.Member, false, Day(20), Day(19), null, null));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            cars.CreateReservationAsync(car.Guid, w.Outsider, false, Day(25), Day(26), null, null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            cars.UpdateStatusAsync(first.Guid, ReservationStatus.Approved, w.Member, false));
        await cars.UpdateStatusAsync(first.Guid, ReservationStatus.Cancelled, w.Member, false);
        await cars.CreateReservationAsync(car.Guid, w.Manager, false, Day(11), Day(11), null, null);
        // Obnovení zrušené rezervace musí znovu ověřit termín — mezitím ho někdo zabral.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cars.UpdateStatusAsync(first.Guid, ReservationStatus.Pending, w.Admin, false));

        await Assert.ThrowsAsync<ArgumentException>(() => cars.CompleteAsync(first.Guid, 900m, w.Member, false));
    }

    [Fact]
    public async Task CarReservation_ConcurrentOverlappingRequests_OnlyOneWins()
    {
        var w = await CreateWorldAsync();
        Guid carId;
        using (var setup = factory.Services.CreateScope())
            carId = (await Get<CarReservationService>(setup)
                .SaveCarAsync(new Car { OrganizationId = w.OrgId, Name = "Transit" }, w.Admin, false)).Guid;

        async Task<bool> TryReserveAsync(string userId)
        {
            using var scope = factory.Services.CreateScope();
            try
            {
                await Get<CarReservationService>(scope).CreateReservationAsync(
                    carId, userId, false, new DateOnly(2031, 1, 5), new DateOnly(2031, 1, 7), null, null);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 6)
            .Select(i => TryReserveAsync(i % 2 == 0 ? w.Member : w.Manager)));

        Assert.Equal(1, results.Count(ok => ok));
        using var verify = factory.Services.CreateScope();
        await using var db = await Get<IDbContextFactory<AppDbContext>>(verify).CreateDbContextAsync();
        Assert.Equal(1, await db.CarReservations.CountAsync(r => r.CarId == carId));
    }

    [Fact]
    public async Task ClubDetail_ListsSeasonsWhereItsTeamPlayed()
    {
        var w = await CreateWorldAsync();
        using var scope = factory.Services.CreateScope();
        var clubs = Get<ClubService>(scope);
        await using var db = await Get<IDbContextFactory<AppDbContext>>(scope).CreateDbContextAsync();

        var football = await db.Sports.FirstAsync(s => s.Type == SportType.Football);
        var league = new League { Name = $"Liga {Guid.NewGuid():N}"[..13], SportId = football.Guid };
        var season = new Season { League = league, Name = "Podzim", Year = 2030, Status = SeasonStatus.Draft };
        var team = new Team { Name = $"Tym {Guid.NewGuid():N}"[..12] };
        db.AddRange(league, season, team);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => clubs.AssignTeamAsync(w.ClubId, team.Guid, w.Member, false));
        await clubs.AssignTeamAsync(w.ClubId, team.Guid, w.Manager, false);

        db.SeasonParticipants.Add(new SeasonParticipant { SeasonId = season.Guid, TeamId = team.Guid, ClubId = w.ClubId });
        await db.SaveChangesAsync();

        var detail = await clubs.GetClubDetailAsync(w.ClubId);
        Assert.Contains(detail!.Teams, t => t.Guid == team.Guid);
        var row = Assert.Single(detail.Seasons, s => s.SeasonId == season.Guid);
        Assert.Equal(team.Name, row.ParticipantName);
    }

    [Fact]
    public async Task Invitation_CancelInvalidatesLink_AndResendReplacesToken()
    {
        var w = await CreateWorldAsync();
        using var scope = factory.Services.CreateScope();
        var invitations = Get<InvitationService>(scope);
        var email = await factory.EmailOfAsync(w.Outsider);

        var cancelled = await invitations.CreateInvitationAsync(email, w.ClubId, OrgRole.Member, w.Manager, false);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => invitations.CancelAsync(cancelled.Guid, w.Member, false));
        await invitations.CancelAsync(cancelled.Guid, w.Manager, false);
        Assert.Null(await invitations.GetValidByTokenAsync(cancelled.Token));
        Assert.Equal(InvitationAcceptResult.NotFound, await invitations.AcceptAsync(cancelled.Token, w.Outsider));
        Assert.DoesNotContain(await invitations.GetPendingForClubAsync(w.ClubId), i => i.Guid == cancelled.Guid);

        var original = await invitations.CreateInvitationAsync(email, w.ClubId, OrgRole.Member, w.Manager, false);
        var originalToken = original.Token;
        var resent = await invitations.ResendAsync(original.Guid, w.Manager, false);

        Assert.NotEqual(originalToken, resent.Token);
        // Po znovuodeslání nesmí zůstat platné dva odkazy.
        Assert.Null(await invitations.GetValidByTokenAsync(originalToken));
        Assert.Equal(InvitationAcceptResult.Accepted, await invitations.AcceptAsync(resent.Token, w.Outsider));
        await Assert.ThrowsAsync<InvalidOperationException>(() => invitations.ResendAsync(original.Guid, w.Manager, false));
    }

    [Fact]
    public async Task NotificationPreference_DefaultsMatchDispatcher_AndSaveUpdatesSingleRow()
    {
        var w = await CreateWorldAsync();
        using var scope = factory.Services.CreateScope();
        var chat = Get<ChatService>(scope);

        // Neuložená výchozí preference se musí chovat stejně jako žádná — jinak by UI ukazovalo jiný stav, než platí.
        var defaults = await chat.GetPreferenceAsync(w.ClubId, w.Member, false);
        foreach (var type in Enum.GetValues<ThreadType>())
            Assert.Equal(ChatNotificationDispatcher.Channels(type, null), ChatNotificationDispatcher.Channels(type, defaults));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            chat.SavePreferenceAsync(w.ClubId, w.Outsider, false, true, true, NotifyMinPriority.All));

        await chat.SavePreferenceAsync(w.ClubId, w.Member, false, false, true, NotifyMinPriority.All);
        await chat.SavePreferenceAsync(w.ClubId, w.Member, false, false, true, NotifyMinPriority.Urgent);

        var saved = await chat.GetPreferenceAsync(w.ClubId, w.Member, false);
        Assert.False(saved.EmailEnabled);
        Assert.True(saved.NtfyEnabled);
        Assert.Equal(NotifyMinPriority.Urgent, saved.MinPriority);

        await using var db = await Get<IDbContextFactory<AppDbContext>>(scope).CreateDbContextAsync();
        Assert.Equal(1, await db.NotificationPreferences.CountAsync(p => p.ClubId == w.ClubId && p.UserId == w.Member));
    }
}
