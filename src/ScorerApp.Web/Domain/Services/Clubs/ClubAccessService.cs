using Microsoft.EntityFrameworkCore;
using ScorerApp.Data;
using ScorerApp.Domain.Models.Clubs;

namespace ScorerApp.Domain.Services.Clubs;

/// <summary>
/// Jediné místo s pravidly oprávnění klubového modulu. ClubManager je měl rozházená po stránkách
/// a část míst (SignalR hub, správa členů) je nekontrolovala vůbec — služby proto ověřují přístup
/// samy, nezávisle na tom, jestli to udělala i stránka.
/// </summary>
public class ClubAccessService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<OrgRole?> GetOrgRoleAsync(Guid organizationId, string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.OrganizationMembers
            .Where(m => m.OrganizationId == organizationId && m.UserId == userId && m.IsActive)
            .Select(m => (OrgRole?)m.Role)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CanManageOrganizationAsync(Guid organizationId, string? userId, bool isSiteAdmin) =>
        isSiteAdmin || await GetOrgRoleAsync(organizationId, userId) == OrgRole.OrgAdmin;

    public async Task<bool> CanManageClubAsync(Guid clubId, string? userId, bool isSiteAdmin)
    {
        if (isSiteAdmin) return true;
        if (string.IsNullOrEmpty(userId)) return false;

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Clubs
            .Where(c => c.Guid == clubId)
            .AnyAsync(c => db.OrganizationMembers.Any(m =>
                m.OrganizationId == c.OrganizationId && m.UserId == userId
                && m.IsActive && m.Role >= OrgRole.ClubManager));
    }

    /// <summary>Kdo smí číst a psát oddílový chat: správci oddílu a hráči ze soupisky spárovaní s účtem.</summary>
    public async Task<bool> IsClubParticipantAsync(Guid clubId, string? userId, bool isSiteAdmin)
    {
        if (await CanManageClubAsync(clubId, userId, isSiteAdmin)) return true;
        if (string.IsNullOrEmpty(userId)) return false;

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ClubMembers.AnyAsync(m =>
            m.ClubId == clubId && m.IsActive && m.Player.UserId == userId);
    }

    public async Task<bool> IsOrganizationMemberAsync(Guid organizationId, string? userId, bool isSiteAdmin)
    {
        if (isSiteAdmin) return true;
        if (string.IsNullOrEmpty(userId)) return false;

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.OrganizationMembers.AnyAsync(m =>
                   m.OrganizationId == organizationId && m.UserId == userId && m.IsActive)
               || await db.ClubMembers.AnyAsync(m =>
                   m.IsActive && m.Club.OrganizationId == organizationId && m.Player.UserId == userId);
    }

    public async Task<List<Guid>> GetParticipantClubIdsAsync(string? userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (isSiteAdmin)
            return await db.Clubs.Where(c => c.IsActive).Select(c => c.Guid).ToListAsync();
        if (string.IsNullOrEmpty(userId)) return [];

        var managed = db.Clubs
            .Where(c => c.IsActive && db.OrganizationMembers.Any(m =>
                m.OrganizationId == c.OrganizationId && m.UserId == userId
                && m.IsActive && m.Role >= OrgRole.ClubManager))
            .Select(c => c.Guid);
        var rostered = db.ClubMembers
            .Where(m => m.IsActive && m.Club.IsActive && m.Player.UserId == userId)
            .Select(m => m.ClubId);

        return await managed.Union(rostered).ToListAsync();
    }

    /// <summary>Účty oddílu — příjemci chatu a oddílových oběžníků. Hráči bez účtu vypadnou, nemají kam doručit.</summary>
    public static IQueryable<string> ClubAccountIds(AppDbContext db, Guid clubId)
    {
        var players = db.ClubMembers
            .Where(m => m.ClubId == clubId && m.IsActive && m.Player.UserId != null)
            .Select(m => m.Player.UserId!);
        var managers = db.OrganizationMembers
            .Where(om => om.IsActive && om.Role >= OrgRole.ClubManager
                         && db.Clubs.Any(c => c.Guid == clubId && c.OrganizationId == om.OrganizationId))
            .Select(om => om.UserId);
        return players.Union(managers);
    }

    public static IQueryable<string> OrganizationAccountIds(AppDbContext db, Guid organizationId)
    {
        var members = db.OrganizationMembers
            .Where(m => m.OrganizationId == organizationId && m.IsActive)
            .Select(m => m.UserId);
        var players = db.ClubMembers
            .Where(m => m.IsActive && m.Club.OrganizationId == organizationId && m.Player.UserId != null)
            .Select(m => m.Player.UserId!);
        return members.Union(players);
    }
}
