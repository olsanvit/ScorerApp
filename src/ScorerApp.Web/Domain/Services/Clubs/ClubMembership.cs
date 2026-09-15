using Microsoft.EntityFrameworkCore;
using ScorerApp.Data;
using ScorerApp.Domain.Models;
using ScorerApp.Domain.Models.Clubs;

namespace ScorerApp.Domain.Services.Clubs;

/// <summary>
/// Sdílené kroky „dostat účet do oddílu“ pro pozvánku, kód skupiny i ruční přidání.
/// Pracují nad předaným DbContextem a NEUKLÁDAJÍ — volající drží transakci celé operace.
/// </summary>
internal static class ClubMembership
{
    public static async Task<OrganizationMember> EnsureOrganizationMemberAsync(
        AppDbContext db, Guid organizationId, string userId, OrgRole role, string? displayName = null)
    {
        var member = await db.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId);

        if (member is null)
        {
            member = new OrganizationMember
            {
                OrganizationId = organizationId,
                UserId         = userId,
                Role           = role,
                DisplayName    = displayName
            };
            db.OrganizationMembers.Add(member);
            return member;
        }

        member.IsActive = true;
        // Pozvánka nesmí nikomu roli snížit — správce pozvaný omylem jako člen by přišel o práva.
        if (role > member.Role) member.Role = role;
        return member;
    }

    /// <summary>
    /// Najde hráče k účtu: nejdřív už spárovaného, pak nespárovaného se stejným e-mailem
    /// (hráč založený na turnaji dřív, než si udělal účet), jinak založí nového.
    /// </summary>
    public static async Task<Player> EnsurePlayerForAccountAsync(
        AppDbContext db, string userId, string? email, string? displayName)
    {
        var player = await db.Players.FirstOrDefaultAsync(p => p.UserId == userId);
        if (player is not null) return player;

        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalized = email.Trim().ToLower();
            player = await db.Players.FirstOrDefaultAsync(p =>
                p.UserId == null && p.Email != null && p.Email.ToLower() == normalized);
            if (player is not null)
            {
                player.UserId = userId;
                return player;
            }
        }

        player = new Player
        {
            Name   = !string.IsNullOrWhiteSpace(displayName) ? displayName.Trim() : NameFromEmail(email) ?? userId,
            Email  = email?.Trim(),
            UserId = userId
        };
        db.Players.Add(player);
        return player;
    }

    public static async Task<ClubMember> EnsureClubMemberAsync(AppDbContext db, Guid clubId, Guid playerId)
    {
        var member = await db.ClubMembers.FirstOrDefaultAsync(m => m.ClubId == clubId && m.PlayerId == playerId);
        if (member is null)
        {
            member = new ClubMember { ClubId = clubId, PlayerId = playerId };
            db.ClubMembers.Add(member);
        }
        member.IsActive = true;
        return member;
    }

    private static string? NameFromEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().Split('@')[0];
}
