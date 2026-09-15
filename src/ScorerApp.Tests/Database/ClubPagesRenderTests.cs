using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScorerApp.Data;
using ScorerApp.Domain.Models;
using ScorerApp.Domain.Models.Clubs;
using ScorerApp.Domain.Services.Clubs;

namespace ScorerApp.Tests.Database;

/// <summary>
/// Stránky modulu Kluby vyrenderované přihlášenému uživateli (prerender InteractiveServer vrací HTML
/// s načtenými daty). Značky jsou data s ASCII názvy, ne přeložené texty — prerender kóduje diakritiku
/// (í → &amp;#xED;) a kontrola textu by na ní selhala; zároveň testy nezávisí na jazyku.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class ClubPagesRenderTests(DatabaseTestFactory factory) : IAsyncLifetime
{
    private sealed record PageWorld(
        string Admin, string Manager, string Member, string Outsider,
        Guid OrgId, string OrgName, Guid ClubId, string ClubName, string JoinCode,
        string ThreadTitle, Guid CircularId, string CircularSubject,
        string CarName, string InviteToken, Guid SeasonId);

    private PageWorld _w = null!;

    public async Task InitializeAsync()
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var admin = await factory.CreateUserAsync("padmin");
        var manager = await factory.CreateUserAsync("pmanager");
        var member = await factory.CreateUserAsync("pmember");
        var outsider = await factory.CreateUserAsync("poutsider");

        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var clubs = sp.GetRequiredService<ClubService>();
        var invitations = sp.GetRequiredService<InvitationService>();
        var cars = sp.GetRequiredService<CarReservationService>();

        var org = await clubs.CreateOrganizationAsync($"Org-{s}", null, admin, isSiteAdmin: true);
        var club = await clubs.CreateClubAsync(org.Guid, $"Club-{s}", "CLB", null, admin, false);
        await clubs.AddOrganizationMemberByEmailAsync(org.Guid, await factory.EmailOfAsync(manager), OrgRole.ClubManager, null, admin, false);
        await invitations.JoinByCodeAsync(club.JoinCode, member);

        var thread = await sp.GetRequiredService<ChatService>()
            .CreateThreadAsync(club.Guid, $"Thread-{s}", ThreadType.General, manager, false);
        var (circularId, _) = await sp.GetRequiredService<CircularService>().SendAsync(
            org.Guid, club.Guid, manager, false, $"Subject-{s}", $"Body-{s}", CircularType.Debt, false, false);
        var car = await cars.SaveCarAsync(new Car { OrganizationId = org.Guid, Name = $"Car-{s}" }, admin, false);
        await cars.CreateReservationAsync(car.Guid, member, false, new DateOnly(2032, 3, 1), new DateOnly(2032, 3, 2), $"Purpose-{s}", null);
        var invite = await invitations.CreateInvitationAsync(await factory.EmailOfAsync(outsider), club.Guid, OrgRole.Member, manager, false);

        await using var db = await sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync();
        var football = await db.Sports.FirstAsync(x => x.Type == SportType.Football);
        var league = new League { Name = $"League-{s}", SportId = football.Guid };
        var season = new Season { League = league, Name = $"Season-{s}", Year = 2032, Status = SeasonStatus.Draft };
        db.AddRange(league, season);
        await db.SaveChangesAsync();

        _w = new PageWorld(admin, manager, member, outsider,
            org.Guid, org.Name, club.Guid, club.Name, club.JoinCode,
            thread.Title, circularId, $"Subject-{s}", car.Name, invite.Token, season.Guid);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string> GetAsync(string url, string? userId, bool isAdmin = false, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var client = factory.ClientAs(userId, isAdmin);
        using var response = await client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected,
            $"GET {url} → {(int)response.StatusCode}, čekáno {(int)expected}. Začátek odpovědi: {html[..Math.Min(html.Length, 500)]}");
        return html;
    }

    [Fact]
    public async Task AnonymousRequest_IsChallenged()
        => await GetAsync("/clubs", userId: null, expected: HttpStatusCode.Unauthorized);

    [Fact]
    public async Task ClubsListAndDetail_RenderForMember_WithoutManagementPanels()
    {
        Assert.Contains(_w.ClubName, await GetAsync("/clubs", _w.Member));

        var detail = await GetAsync($"/clubs/{_w.ClubId}", _w.Member);
        Assert.Contains(_w.ClubName, detail);
        Assert.DoesNotContain(_w.JoinCode, detail);
    }

    [Fact]
    public async Task ClubDetail_ShowsJoinCodeAndPendingInvitation_ToManager()
    {
        var detail = await GetAsync($"/clubs/{_w.ClubId}", _w.Manager);
        Assert.Contains(_w.JoinCode, detail);
        Assert.Contains("@test.local", detail);
    }

    [Fact]
    public async Task ClubEdit_IsForbiddenForMember_AndPrefilledForManager()
    {
        Assert.Contains("alert-danger", await GetAsync($"/clubs/{_w.ClubId}/edit", _w.Member));

        var html = await GetAsync($"/clubs/{_w.ClubId}/edit", _w.Manager);
        Assert.DoesNotContain("alert-danger", html);
        Assert.Contains(_w.ClubName, html);
    }

    [Fact]
    public async Task ClubCreate_OffersOrganizationToOrgAdmin()
        => Assert.Contains(_w.OrgName, await GetAsync($"/clubs/create?organizationId={_w.OrgId}", _w.Admin));

    [Fact]
    public async Task Organizations_RenderForMember_AndForbidOutsider()
    {
        Assert.Contains(_w.OrgName, await GetAsync("/organizations", _w.Member));
        Assert.Contains(_w.ClubName, await GetAsync($"/organizations/{_w.OrgId}", _w.Member));
        Assert.Contains("alert-danger", await GetAsync($"/organizations/{_w.OrgId}", _w.Outsider));
    }

    [Fact]
    public async Task Chat_ListsThreadForMember()
        => Assert.Contains(_w.ThreadTitle, await GetAsync("/chat", _w.Member));

    [Fact]
    public async Task Circulars_ListDetailSendAndDebts_RespectRoles()
    {
        Assert.Contains(_w.CircularSubject, await GetAsync($"/circulars?organizationId={_w.OrgId}", _w.Member));
        Assert.Contains("Body-", await GetAsync($"/circulars/{_w.CircularId}", _w.Member));
        Assert.Contains("alert-danger", await GetAsync($"/circulars/{_w.CircularId}", _w.Outsider));
        Assert.Contains("alert-danger", await GetAsync($"/circulars/debts?organizationId={_w.OrgId}", _w.Member));
        Assert.Contains(_w.CircularSubject, await GetAsync($"/circulars/debts?organizationId={_w.OrgId}", _w.Manager));
        Assert.Contains("<textarea", await GetAsync($"/circulars/send?organizationId={_w.OrgId}", _w.Manager));
    }

    [Fact]
    public async Task CarsAndReservations_RenderForMember_AndForbidOutsider()
    {
        Assert.Contains(_w.CarName, await GetAsync($"/cars?organizationId={_w.OrgId}", _w.Member));

        var reservations = await GetAsync($"/cars/reservations?organizationId={_w.OrgId}", _w.Member);
        Assert.Contains(_w.CarName, reservations);
        Assert.Contains("Purpose-", reservations);

        Assert.Contains("alert-danger", await GetAsync($"/cars?organizationId={_w.OrgId}", _w.Outsider));
    }

    [Fact]
    public async Task JoinAndAcceptInvite_Render()
    {
        Assert.Contains("maxlength=\"16\"", await GetAsync("/join", _w.Outsider));
        Assert.Contains(_w.ClubName, await GetAsync($"/accept-invite?token={_w.InviteToken}", _w.Outsider));
    }

    [Fact]
    public async Task AdminPages_OfferClubs_AndForbidNonAdmin()
    {
        Assert.Contains(_w.ClubName, await GetAsync("/admin/teams/create", _w.Admin, isAdmin: true));
        Assert.Contains(_w.ClubName, await GetAsync($"/admin/seasons/{_w.SeasonId}/participants", _w.Admin, isAdmin: true));
        await GetAsync("/admin/teams/create", _w.Member, expected: HttpStatusCode.Forbidden);
    }
}
