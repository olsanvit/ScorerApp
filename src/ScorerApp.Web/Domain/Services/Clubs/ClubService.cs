using Microsoft.EntityFrameworkCore;
using ScorerApp.Data;
using ScorerApp.Domain.Models;
using ScorerApp.Domain.Models.Clubs;

namespace ScorerApp.Domain.Services.Clubs;

public record ClubListRow(Guid Id, string Name, string? ShortName, string OrganizationName, bool IsActive, int Members, int Teams);

public record ClubSeasonRow(Guid SeasonId, string SeasonName, string LeagueName, SeasonStatus Status, string ParticipantName);

public record ClubDetail(Club Club, List<ClubMember> Roster, List<Team> Teams, List<ClubSeasonRow> Seasons);

public record FamilyLinkRow(Guid Id, Guid PlayerId, string ParentName);

/// <summary>IsParent = uživatel v oddílu není, jen je rodičem hráče ze soupisky.</summary>
public record MyClubRow(Guid Id, string Name, string OrganizationName, bool IsParent);

/// <summary>
/// Organizace, oddíly, soupiska a týmy. Nic se tu nemaže — ScorerApp převádí Remove() na soft delete,
/// při kterém se DB kaskáda nespustí, takže smazaný oddíl by nechal aktivní členy i vlákna.
/// Místo toho se deaktivuje přes IsActive, což je navíc vratné.
/// </summary>
public class ClubService(IDbContextFactory<AppDbContext> dbFactory, ClubAccessService access)
{
    // ── Organizace ────────────────────────────────────────────────────────────

    public async Task<List<Organization>> GetOrganizationsForUserAsync(string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.Organizations.AsNoTracking();
        if (!isSiteAdmin)
        {
            q = q.Where(o => o.IsActive
                && (db.OrganizationMembers.Any(m => m.OrganizationId == o.Guid && m.UserId == userId && m.IsActive)
                    || db.ClubMembers.Any(m => m.IsActive && m.Club.OrganizationId == o.Guid && m.Player.UserId == userId)));
        }
        return await q.OrderBy(o => o.Name).ToListAsync();
    }

    public async Task<Organization?> GetOrganizationAsync(Guid organizationId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Guid == organizationId);
    }

    /// <summary>Organizaci zakládá jen admin aplikace (spec: SuperAdmin); zakladatel se stává jejím správcem.</summary>
    public async Task<Organization> CreateOrganizationAsync(string name, string? description, string creatorUserId, bool isSiteAdmin)
    {
        if (!isSiteAdmin)
            throw new UnauthorizedAccessException("Organizaci smí založit jen administrátor.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Zadej název organizace.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var org = new Organization { Name = name.Trim(), Description = Clean(description) };
        db.Organizations.Add(org);
        await ClubMembership.EnsureOrganizationMemberAsync(db, org.Guid, creatorUserId, OrgRole.OrgAdmin);
        await db.SaveChangesAsync();
        return org;
    }

    public async Task UpdateOrganizationAsync(
        Guid organizationId, string name, string? description, bool isActive, string userId, bool isSiteAdmin)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Zadej název organizace.");
        if (!await access.CanManageOrganizationAsync(organizationId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Organizaci upravuje jen její správce.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Guid == organizationId)
            ?? throw new InvalidOperationException("Organizace neexistuje.");
        org.Name = name.Trim();
        org.Description = Clean(description);
        org.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    public async Task<List<OrganizationMember>> GetOrganizationMembersAsync(Guid organizationId, string userId, bool isSiteAdmin)
    {
        if (!await access.CanManageOrganizationAsync(organizationId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Členy organizace spravuje jen její správce.");

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.OrganizationId == organizationId)
            .OrderByDescending(m => m.IsActive).ThenByDescending(m => m.Role)
            .ThenBy(m => m.DisplayName ?? m.User.UserName)
            .ToListAsync();
    }

    /// <summary>Přidá existující účet podle e-mailu. Neexistující účet je potřeba pozvat — ne založit naslepo.</summary>
    public async Task AddOrganizationMemberByEmailAsync(
        Guid organizationId, string email, OrgRole role, string? displayName, string userId, bool isSiteAdmin)
    {
        if (!await access.CanManageOrganizationAsync(organizationId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Členy organizace spravuje jen její správce.");

        var normalized = email?.Trim().ToUpperInvariant() ?? "";
        await using var db = await dbFactory.CreateDbContextAsync();
        var account = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized)
            ?? throw new InvalidOperationException("Účet s tímto e-mailem neexistuje — pošli pozvánku z detailu oddílu.");

        await ClubMembership.EnsureOrganizationMemberAsync(db, organizationId, account.Id, role, Clean(displayName));
        await db.SaveChangesAsync();
    }

    public async Task ChangeRoleAsync(Guid memberId, OrgRole role, string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var member = await db.OrganizationMembers.FirstOrDefaultAsync(m => m.Guid == memberId)
            ?? throw new InvalidOperationException("Člen neexistuje.");
        if (!await access.CanManageOrganizationAsync(member.OrganizationId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Role mění jen správce organizace.");
        if (member.Role == role) return;

        if (member.Role == OrgRole.OrgAdmin && role != OrgRole.OrgAdmin)
            await EnsureNotLastAdminAsync(db, member);

        member.Role = role;
        await db.SaveChangesAsync();
    }

    public async Task SetOrganizationMemberActiveAsync(Guid memberId, bool isActive, string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var member = await db.OrganizationMembers.FirstOrDefaultAsync(m => m.Guid == memberId)
            ?? throw new InvalidOperationException("Člen neexistuje.");
        if (!await access.CanManageOrganizationAsync(member.OrganizationId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Členy spravuje jen správce organizace.");
        if (member.IsActive == isActive) return;

        if (!isActive && member.Role == OrgRole.OrgAdmin)
            await EnsureNotLastAdminAsync(db, member);

        member.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    /// <summary>Organizace bez aktivního správce by šla spravovat už jen přes admina aplikace.</summary>
    private static async Task EnsureNotLastAdminAsync(AppDbContext db, OrganizationMember member)
    {
        var otherAdmins = await db.OrganizationMembers.CountAsync(m =>
            m.OrganizationId == member.OrganizationId && m.Guid != member.Guid
            && m.IsActive && m.Role == OrgRole.OrgAdmin);
        if (otherAdmins == 0)
            throw new InvalidOperationException("Organizace musí mít aspoň jednoho aktivního správce.");
    }

    // ── Oddíly ────────────────────────────────────────────────────────────────

    public async Task<List<ClubListRow>> GetClubsAsync(Guid? organizationId, bool includeInactive)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.Clubs.AsNoTracking().Where(c => includeInactive || c.IsActive);
        if (organizationId.HasValue) q = q.Where(c => c.OrganizationId == organizationId);

        return await q
            .OrderBy(c => c.Organization.Name).ThenBy(c => c.Name)
            .Select(c => new ClubListRow(
                c.Guid, c.Name, c.ShortName, c.Organization.Name, c.IsActive,
                c.Members.Count(m => m.IsActive),
                c.Teams.Count))
            .ToListAsync();
    }

    public async Task<ClubDetail?> GetClubDetailAsync(Guid clubId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var club = await db.Clubs.AsNoTracking()
            .Include(c => c.Organization)
            .FirstOrDefaultAsync(c => c.Guid == clubId);
        if (club is null) return null;

        var roster = await db.ClubMembers.AsNoTracking()
            .Include(m => m.Player)
            .Where(m => m.ClubId == clubId && m.IsActive)
            .OrderBy(m => m.Player.Name)
            .ToListAsync();

        var teams = await db.Teams.AsNoTracking()
            .Where(t => t.ClubId == clubId)
            .OrderBy(t => t.Name)
            .ToListAsync();

        // Sezóny, kde oddíl nastoupil: přímo přes ClubId účastníka, nebo přes svůj tým.
        var teamIds = teams.Select(t => t.Guid).ToList();
        var seasons = await db.SeasonParticipants.AsNoTracking()
            .Where(p => p.ClubId == clubId || (p.TeamId != null && teamIds.Contains(p.TeamId.Value)))
            .OrderByDescending(p => p.Season.Year).ThenBy(p => p.Season.Name)
            .Select(p => new ClubSeasonRow(
                p.SeasonId, p.Season.Name, p.Season.League.Name, p.Season.Status,
                p.Team != null ? p.Team.Name : p.Player != null ? p.Player.Name : "—"))
            .ToListAsync();

        return new ClubDetail(club, roster, teams, seasons);
    }

    public async Task<Club> CreateClubAsync(
        Guid organizationId, string name, string? shortName, string? description, string userId, bool isSiteAdmin)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Zadej název oddílu.");
        if (!await access.CanManageOrganizationAsync(organizationId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Oddíl zakládá jen správce organizace.");

        await using var db = await dbFactory.CreateDbContextAsync();
        await EnsureUniqueClubNameAsync(db, organizationId, name, exceptId: null);

        var club = new Club
        {
            OrganizationId = organizationId,
            Name           = name.Trim(),
            ShortName      = Clean(shortName),
            Description    = Clean(description)
        };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();
        return club;
    }

    public async Task UpdateClubAsync(
        Guid clubId, string name, string? shortName, string? description, bool isActive, string userId, bool isSiteAdmin)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Zadej název oddílu.");
        if (!await access.CanManageClubAsync(clubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Oddíl upravuje jen jeho správce.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Guid == clubId)
            ?? throw new InvalidOperationException("Oddíl neexistuje.");
        await EnsureUniqueClubNameAsync(db, club.OrganizationId, name, exceptId: clubId);

        club.Name        = name.Trim();
        club.ShortName   = Clean(shortName);
        club.Description = Clean(description);
        club.IsActive    = isActive;
        await db.SaveChangesAsync();
    }

    public async Task<string> RegenerateJoinCodeAsync(Guid clubId, string userId, bool isSiteAdmin)
    {
        if (!await access.CanManageClubAsync(clubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Kód skupiny mění jen správce oddílu.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Guid == clubId)
            ?? throw new InvalidOperationException("Oddíl neexistuje.");
        club.JoinCode = Club.NewJoinCode();
        await db.SaveChangesAsync();
        return club.JoinCode;
    }

    /// <summary>Unikátní index to hlídá taky, ale výjimka z Postgresu by uživateli nic neřekla.</summary>
    private static async Task EnsureUniqueClubNameAsync(AppDbContext db, Guid organizationId, string name, Guid? exceptId)
    {
        var lower = name.Trim().ToLower();
        if (await db.Clubs.AnyAsync(c => c.OrganizationId == organizationId && c.Name.ToLower() == lower
                                         && (exceptId == null || c.Guid != exceptId)))
            throw new InvalidOperationException("Oddíl s tímto názvem už v organizaci existuje.");
    }

    // ── Soupiska ──────────────────────────────────────────────────────────────

    public async Task<List<Player>> GetPlayersNotInRosterAsync(Guid clubId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Players.AsNoTracking()
            .Where(p => !db.ClubMembers.Any(m => m.ClubId == clubId && m.PlayerId == p.Guid && m.IsActive))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task AddPlayerToRosterAsync(Guid clubId, Guid playerId, string userId, bool isSiteAdmin)
    {
        await EnsureCanManageClubAsync(clubId, userId, isSiteAdmin);

        await using var db = await dbFactory.CreateDbContextAsync();
        if (!await db.Players.AnyAsync(p => p.Guid == playerId))
            throw new InvalidOperationException("Hráč neexistuje.");
        await ClubMembership.EnsureClubMemberAsync(db, clubId, playerId);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Přidá hráče jménem. Existující hráč se stejným jménem se použije znovu (stejně jako
    /// registrace do sezóny), aby soupiska a sezóny nevytvářely duplicitní záznamy téhož člověka.
    /// </summary>
    public async Task<bool> AddPlayerByNameAsync(Guid clubId, string name, string userId, bool isSiteAdmin)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Zadej jméno hráče.");
        await EnsureCanManageClubAsync(clubId, userId, isSiteAdmin);

        await using var db = await dbFactory.CreateDbContextAsync();
        var trimmed = name.Trim();
        var lower = trimmed.ToLower();
        var player = await db.Players.FirstOrDefaultAsync(p => p.Name.ToLower() == lower);
        var reused = player is not null;
        if (player is null)
        {
            player = new Player { Name = trimmed };
            db.Players.Add(player);
        }

        await ClubMembership.EnsureClubMemberAsync(db, clubId, player.Guid);
        await db.SaveChangesAsync();
        return reused;
    }

    public async Task RemoveFromRosterAsync(Guid memberId, string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var member = await db.ClubMembers.FirstOrDefaultAsync(m => m.Guid == memberId)
            ?? throw new InvalidOperationException("Člen soupisky neexistuje.");
        await EnsureCanManageClubAsync(member.ClubId, userId, isSiteAdmin);

        member.IsActive = false;
        await db.SaveChangesAsync();
    }

    public async Task UpdateRosterPositionAsync(Guid memberId, string? position, string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var member = await db.ClubMembers.FirstOrDefaultAsync(m => m.Guid == memberId)
            ?? throw new InvalidOperationException("Člen soupisky neexistuje.");
        await EnsureCanManageClubAsync(member.ClubId, userId, isSiteAdmin);

        member.Position = Clean(position);
        await db.SaveChangesAsync();
    }

    // ── Rodiče ────────────────────────────────────────────────────────────────

    public async Task<List<FamilyLinkRow>> GetParentsForClubAsync(Guid clubId, string userId, bool isSiteAdmin)
    {
        await EnsureCanManageClubAsync(clubId, userId, isSiteAdmin);

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.FamilyLinks.AsNoTracking()
            .Where(f => db.ClubMembers.Any(m => m.ClubId == clubId && m.IsActive && m.PlayerId == f.ChildPlayerId
                                                && m.Club.OrganizationId == f.OrganizationId))
            .OrderBy(f => f.ParentUser.UserName)
            .Select(f => new FamilyLinkRow(f.Guid, f.ChildPlayerId, f.ParentUser.UserName ?? f.ParentUser.Email ?? f.ParentUserId))
            .ToListAsync();
    }

    /// <summary>
    /// Propojí existující účet rodiče s hráčem soupisky. Rodič se stává členem organizace (roli nesnižuje),
    /// jinak by se k oběžníkům v aplikaci nedostal — seznam organizací i oběžníků vychází z členství.
    /// </summary>
    public async Task LinkParentByEmailAsync(Guid clubId, Guid playerId, string email, string userId, bool isSiteAdmin)
    {
        await EnsureCanManageClubAsync(clubId, userId, isSiteAdmin);

        var normalized = email?.Trim().ToUpperInvariant() ?? "";
        await using var db = await dbFactory.CreateDbContextAsync();
        var member = await db.ClubMembers.Include(m => m.Club).Include(m => m.Player)
            .FirstOrDefaultAsync(m => m.ClubId == clubId && m.PlayerId == playerId && m.IsActive)
            ?? throw new InvalidOperationException("Hráč není na soupisce oddílu.");
        var parent = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized)
            ?? throw new InvalidOperationException("Účet s tímto e-mailem neexistuje — rodič si ho musí nejdřív založit.");
        if (member.Player.UserId == parent.Id)
            throw new InvalidOperationException("Hráč nemůže být rodičem sám sobě.");

        var organizationId = member.Club.OrganizationId;
        if (await db.FamilyLinks.AnyAsync(f => f.OrganizationId == organizationId
                                              && f.ParentUserId == parent.Id && f.ChildPlayerId == playerId))
            return;

        await ClubMembership.EnsureOrganizationMemberAsync(db, organizationId, parent.Id, OrgRole.Member);
        db.FamilyLinks.Add(new FamilyLink { OrganizationId = organizationId, ParentUserId = parent.Id, ChildPlayerId = playerId });
        await db.SaveChangesAsync();
    }

    /// <summary>Členství v organizaci zůstává — rodič v ní může být i z jiného důvodu; odebere ho správce organizace.</summary>
    public async Task UnlinkParentAsync(Guid linkId, string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var link = await db.FamilyLinks.FirstOrDefaultAsync(f => f.Guid == linkId)
            ?? throw new InvalidOperationException("Propojení neexistuje.");
        if (!isSiteAdmin && !(await access.GetOrgRoleAsync(link.OrganizationId, userId) >= OrgRole.ClubManager))
            throw new UnauthorizedAccessException("Rodiče spravuje jen správce oddílu.");

        db.FamilyLinks.Remove(link);
        await db.SaveChangesAsync();
    }

    // ── Týmy ──────────────────────────────────────────────────────────────────

    public async Task<List<Team>> GetAssignableTeamsAsync(Guid clubId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Teams.AsNoTracking()
            .Where(t => t.ClubId == null)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task AssignTeamAsync(Guid clubId, Guid teamId, string userId, bool isSiteAdmin)
    {
        await EnsureCanManageClubAsync(clubId, userId, isSiteAdmin);

        await using var db = await dbFactory.CreateDbContextAsync();
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Guid == teamId)
            ?? throw new InvalidOperationException("Tým neexistuje.");
        // Přetažení cizího týmu by jeho oddílu tiše sebralo tým — musí ho uvolnit jeho správce.
        if (team.ClubId is Guid current && current != clubId
            && !await access.CanManageClubAsync(current, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Tým patří jinému oddílu.");

        team.ClubId = clubId;
        await db.SaveChangesAsync();
    }

    public async Task UnassignTeamAsync(Guid teamId, string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var team = await db.Teams.FirstOrDefaultAsync(t => t.Guid == teamId)
            ?? throw new InvalidOperationException("Tým neexistuje.");
        if (team.ClubId is not Guid clubId) return;
        await EnsureCanManageClubAsync(clubId, userId, isSiteAdmin);

        team.ClubId = null;
        await db.SaveChangesAsync();
    }

    // ── Přehled ───────────────────────────────────────────────────────────────

    /// <summary>Sekvenčně, ne Task.WhenAll — paralelní dotazy nad jedním DbContextem padají (viz PlayerDetail).</summary>
    public async Task<(int Organizations, int Clubs, int Members)> GetSummaryAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var orgs    = await db.Organizations.CountAsync(o => o.IsActive);
        var clubs   = await db.Clubs.CountAsync(c => c.IsActive);
        var members = await db.ClubMembers.CountAsync(m => m.IsActive);
        return (orgs, clubs, members);
    }

    /// <summary>Oddíly, kde je uživatel hráčem nebo správcem, plus oddíly jeho dětí (IsParent).</summary>
    public async Task<List<MyClubRow>> GetMyClubsAsync(string userId, bool isSiteAdmin)
    {
        var participant = await access.GetParticipantClubIdsAsync(userId, isSiteAdmin);

        await using var db = await dbFactory.CreateDbContextAsync();
        var parentOf = await db.ClubMembers
            .Where(m => m.IsActive && m.Club.IsActive && db.FamilyLinks.Any(f =>
                f.ParentUserId == userId && f.ChildPlayerId == m.PlayerId && f.OrganizationId == m.Club.OrganizationId))
            .Select(m => m.ClubId)
            .Distinct()
            .ToListAsync();

        var ids = participant.Union(parentOf).ToList();
        return await db.Clubs.AsNoTracking()
            .Where(c => ids.Contains(c.Guid))
            .OrderBy(c => c.Name)
            .Select(c => new MyClubRow(c.Guid, c.Name, c.Organization.Name, !participant.Contains(c.Guid)))
            .ToListAsync();
    }

    private async Task EnsureCanManageClubAsync(Guid clubId, string userId, bool isSiteAdmin)
    {
        if (!await access.CanManageClubAsync(clubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Soupisku a týmy spravuje jen správce oddílu.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
