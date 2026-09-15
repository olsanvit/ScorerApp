using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using ScorerApp.Data;
using ScorerApp.Domain.Models.Clubs;

namespace ScorerApp.Domain.Services.Clubs;

public enum InvitationAcceptResult
{
    Accepted,
    NotFound,
    EmailMismatch
}

public class InvitationService(
    IDbContextFactory<AppDbContext> dbFactory,
    ClubAccessService access,
    ClubNotificationService notifier,
    IConfiguration config,
    ILogger<InvitationService> logger)
{
    public async Task<Invitation> CreateInvitationAsync(
        string email, Guid clubId, OrgRole role, string inviterUserId, bool isSiteAdmin)
    {
        var normalized = email?.Trim().ToLowerInvariant() ?? "";
        if (!MailAddress.TryCreate(normalized, out _))
            throw new ArgumentException("Neplatný e-mail.");
        if (!await access.CanManageClubAsync(clubId, inviterUserId, isSiteAdmin))
            throw new UnauthorizedAccessException("Zvát do oddílu smí jen jeho správce.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var club = await db.Clubs.Include(c => c.Organization).FirstOrDefaultAsync(c => c.Guid == clubId)
            ?? throw new InvalidOperationException("Oddíl neexistuje.");

        // Správce oddílu nesmí přes pozvánku rozdat vyšší práva, než sám má.
        if (role == OrgRole.OrgAdmin
            && !await access.CanManageOrganizationAsync(club.OrganizationId, inviterUserId, isSiteAdmin))
            throw new UnauthorizedAccessException("Roli správce organizace smí udělit jen správce organizace.");

        var invitation = new Invitation
        {
            Email           = normalized,
            ClubId          = clubId,
            Role            = role,
            InvitedByUserId = inviterUserId
        };
        db.Invitations.Add(invitation);
        await db.SaveChangesAsync();

        await SendInvitationEmailAsync(invitation, club);
        return invitation;
    }

    /// <summary>
    /// Pošle nepřijatou pozvánku znovu s NOVÝM tokenem a prodlouženou platností. Starý odkaz tím
    /// přestane fungovat — po přeposlání by jinak zůstaly platné dva odkazy do oddílu.
    /// </summary>
    public async Task<Invitation> ResendAsync(Guid invitationId, string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var invitation = await db.Invitations
            .Include(i => i.Club).ThenInclude(c => c.Organization)
            .FirstOrDefaultAsync(i => i.Guid == invitationId)
            ?? throw new InvalidOperationException("Pozvánka neexistuje.");
        if (!await access.CanManageClubAsync(invitation.ClubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Pozvánky spravuje jen správce oddílu.");
        if (invitation.AcceptedAt is not null)
            throw new InvalidOperationException("Pozvánka už byla přijata.");

        invitation.Token = Guid.NewGuid().ToString("N");
        invitation.ExpiresAt = DateTimeOffset.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        await SendInvitationEmailAsync(invitation, invitation.Club);
        return invitation;
    }

    /// <summary>
    /// Zruší nepřijatou pozvánku. Remove se převede na soft delete (AuditInterceptor) a globální filtr
    /// ji pak skryje i v GetValidByTokenAsync a AcceptAsync — odkaz z e-mailu hlásí neplatnou pozvánku.
    /// </summary>
    public async Task CancelAsync(Guid invitationId, string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var invitation = await db.Invitations.FirstOrDefaultAsync(i => i.Guid == invitationId)
            ?? throw new InvalidOperationException("Pozvánka neexistuje.");
        if (!await access.CanManageClubAsync(invitation.ClubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Pozvánky spravuje jen správce oddílu.");
        if (invitation.AcceptedAt is not null)
            throw new InvalidOperationException("Pozvánka už byla přijata.");

        db.Invitations.Remove(invitation);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Odkaz do e-mailu musí být absolutní — poštovní klient nezná doménu aplikace, relativní
    /// odkaz z ClubManageru byl nefunkční. App:BaseUrl musí obsahovat i PathBase (např. /scorer).
    /// </summary>
    public string BuildAbsoluteLink(string pathAndQuery)
    {
        var baseUrl = config["App:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("App:BaseUrl není nastavené — odkaz v pozvánce bude relativní a z e-mailu nepůjde otevřít");
            return pathAndQuery;
        }
        return baseUrl.TrimEnd('/') + pathAndQuery;
    }

    public async Task<Invitation?> GetValidByTokenAsync(string token)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        return await db.Invitations
            .AsNoTracking()
            .Include(i => i.Club).ThenInclude(c => c.Organization)
            .FirstOrDefaultAsync(i => i.Token == token && i.AcceptedAt == null && i.ExpiresAt > now);
    }

    /// <summary>
    /// Nepřijaté pozvánky oddílu VČETNĚ prošlých — správce je musí vidět, aby je mohl poslat znovu
    /// nebo zrušit; stránka prošlé odliší štítkem.
    /// </summary>
    public async Task<List<Invitation>> GetPendingForClubAsync(Guid clubId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Invitations
            .AsNoTracking()
            .Where(i => i.ClubId == clubId && i.AcceptedAt == null)
            .OrderBy(i => i.ExpiresAt)
            .ToListAsync();
    }

    public async Task<InvitationAcceptResult> AcceptAsync(string token, string userId, string? displayName = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var now = DateTimeOffset.UtcNow;
        var invitation = await db.Invitations
            .Include(i => i.Club)
            .FirstOrDefaultAsync(i => i.Token == token && i.AcceptedAt == null && i.ExpiresAt > now);
        if (invitation is null) return InvitationAcceptResult.NotFound;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        // Token putuje e-mailem; bez kontroly adresy by pozvánku (i s rolí správce) přijal
        // kdokoli přihlášený, komu se odkaz dostane do ruky.
        if (user?.Email is null || !string.Equals(user.Email.Trim(), invitation.Email, StringComparison.OrdinalIgnoreCase))
            return InvitationAcceptResult.EmailMismatch;

        await ClubMembership.EnsureOrganizationMemberAsync(
            db, invitation.Club.OrganizationId, userId, invitation.Role, displayName);
        var player = await ClubMembership.EnsurePlayerForAccountAsync(db, userId, user.Email, displayName);
        await ClubMembership.EnsureClubMemberAsync(db, invitation.ClubId, player.Guid);

        invitation.AcceptedAt = now;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return InvitationAcceptResult.Accepted;
    }

    /// <summary>Vstup přes kód skupiny — vždy jako běžný člen, vyšší roli uděluje jen správce.</summary>
    public async Task<Club?> JoinByCodeAsync(string code, string userId)
    {
        var normalized = code?.Trim().ToUpperInvariant() ?? "";
        if (normalized.Length == 0) return null;

        await using var db = await dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var club = await db.Clubs.FirstOrDefaultAsync(c => c.JoinCode == normalized && c.IsActive);
        if (club is null) return null;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return null;

        await ClubMembership.EnsureOrganizationMemberAsync(db, club.OrganizationId, userId, OrgRole.Member);
        var player = await ClubMembership.EnsurePlayerForAccountAsync(db, userId, user.Email, null);
        await ClubMembership.EnsureClubMemberAsync(db, club.Guid, player.Guid);

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return club;
    }

    private async Task SendInvitationEmailAsync(Invitation invitation, Club club)
    {
        var link = BuildAbsoluteLink($"/accept-invite?token={invitation.Token}");
        var sent = await notifier.SendEmailAsync(invitation.Email, invitation.Email, $"Pozvánka do oddílu {club.Name}",
            $"<p>Byl(a) jste pozván(a) do oddílu <strong>{WebUtility.HtmlEncode(club.Name)}</strong> " +
            $"({WebUtility.HtmlEncode(club.Organization.Name)}).</p>" +
            $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Přijmout pozvánku</a></p><p>Platnost 7 dní.</p>");
        if (!sent)
            logger.LogWarning("Pozvánku pro {Email} se nepodařilo odeslat e-mailem — odkaz lze předat ručně", invitation.Email);
    }
}
