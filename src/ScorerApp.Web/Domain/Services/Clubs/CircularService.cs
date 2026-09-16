using System.Net;
using Microsoft.EntityFrameworkCore;
using ScorerApp.Data;
using ScorerApp.Domain.Models.Clubs;

namespace ScorerApp.Domain.Services.Clubs;

public record CircularDetail(Circular? Circular, bool Forbidden);

public record DebtSummary(
    Guid Id, string Subject, DateTimeOffset? SentAt, string? ClubName,
    int ReadCount, int UnreadCount, List<string> UnreadNames);

/// <summary>Oběžníky (v ClubManageru MessageService) — hromadné zprávy s evidencí doručení a přečtení.</summary>
public class CircularService(
    IDbContextFactory<AppDbContext> dbFactory,
    ClubAccessService access,
    ClubNotificationService notifier,
    ILogger<CircularService> logger)
{
    public async Task<List<Circular>> GetCircularsAsync(Guid organizationId, Guid? clubId, string userId, bool isSiteAdmin)
    {
        var seesAll = isSiteAdmin || await access.GetOrgRoleAsync(organizationId, userId) >= OrgRole.ClubManager;

        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.Circulars
            .AsNoTracking()
            .Include(c => c.Club)
            .Include(c => c.SenderUser)
            .Include(c => c.Recipients)
            .Where(c => c.OrganizationId == organizationId);
        if (clubId.HasValue) q = q.Where(c => c.ClubId == clubId);
        // Běžný člen vidí jen oběžníky, které sám dostal nebo poslal.
        if (!seesAll) q = q.Where(c => c.SenderUserId == userId || c.Recipients.Any(r => r.UserId == userId));

        return await q.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<CircularDetail> GetCircularAsync(Guid id, string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var circular = await db.Circulars
            .AsNoTracking()
            .Include(c => c.Club)
            .Include(c => c.SenderUser)
            .Include(c => c.Recipients).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(c => c.Guid == id);
        if (circular is null) return new(null, false);

        var allowed = isSiteAdmin
                      || circular.SenderUserId == userId
                      || circular.Recipients.Any(r => r.UserId == userId)
                      || await access.GetOrgRoleAsync(circular.OrganizationId, userId) >= OrgRole.ClubManager;
        return allowed ? new(circular, false) : new(null, true);
    }

    public async Task MarkReadAsync(Guid circularId, string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var recipient = await db.CircularRecipients
            .FirstOrDefaultAsync(r => r.CircularId == circularId && r.UserId == userId && r.ReadAt == null);
        if (recipient is null) return;

        recipient.ReadAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<List<string>> PreviewRecipientNamesAsync(Guid organizationId, Guid? clubId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ids = await RecipientIds(db, organizationId, clubId).ToListAsync();
        return await db.Users
            .Where(u => ids.Contains(u.Id))
            .OrderBy(u => u.UserName)
            .Select(u => u.UserName ?? u.Email ?? u.Id)
            .ToListAsync();
    }

    public async Task<(Guid Id, int Recipients)> SendAsync(
        Guid organizationId, Guid? clubId, string senderUserId, bool isSiteAdmin,
        string subject, string body, CircularType type, bool sendEmail, bool sendNtfy)
    {
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Vyplň předmět i text oběžníku.");

        var allowed = clubId.HasValue
            ? await access.CanManageClubAsync(clubId.Value, senderUserId, isSiteAdmin)
            : await access.CanManageOrganizationAsync(organizationId, senderUserId, isSiteAdmin);
        if (!allowed)
            throw new UnauthorizedAccessException(clubId.HasValue
                ? "Oběžník do oddílu smí poslat jen jeho správce."
                : "Oběžník celé organizaci smí poslat jen správce organizace.");

        Guid id;
        int count;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            if (clubId.HasValue && !await db.Clubs.AnyAsync(c => c.Guid == clubId && c.OrganizationId == organizationId))
                throw new InvalidOperationException("Oddíl nepatří do vybrané organizace.");

            var recipientIds = await RecipientIds(db, organizationId, clubId).ToListAsync();
            if (recipientIds.Count == 0)
                throw new InvalidOperationException("Oběžník nemá žádné příjemce s účtem.");

            await using var tx = await db.Database.BeginTransactionAsync();
            var circular = new Circular
            {
                OrganizationId = organizationId,
                ClubId         = clubId,
                SenderUserId   = senderUserId,
                Subject        = subject.Trim(),
                Body           = body.Trim(),
                Type           = type,
                SendEmail      = sendEmail,
                SendNtfy       = sendNtfy,
                Status         = CircularStatus.Sent,
                SentAt         = DateTimeOffset.UtcNow
            };
            circular.Recipients.AddRange(recipientIds.Distinct().Select(uid => new CircularRecipient
            {
                UserId      = uid,
                EmailStatus = sendEmail ? DeliveryStatus.Pending : DeliveryStatus.Sent,
                NtfyStatus  = sendNtfy ? DeliveryStatus.Pending : DeliveryStatus.Sent
            }));
            db.Circulars.Add(circular);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            id = circular.Guid;
            count = circular.Recipients.Count;
        }

        if (sendEmail || sendNtfy)
        {
            // Oběžník je uložený a potvrzený; selhání doručení ho nesmí vrátit — stav příjemců ukáže, komu nedošel.
            try { await DeliverAsync(id); }
            catch (Exception ex) { logger.LogError(ex, "Doručení oběžníku {Id} selhalo", id); }
        }

        return (id, count);
    }

    public async Task<List<DebtSummary>> GetDebtSummariesAsync(Guid organizationId, string userId, bool isSiteAdmin)
    {
        if (!isSiteAdmin && !(await access.GetOrgRoleAsync(organizationId, userId) >= OrgRole.ClubManager))
            throw new UnauthorizedAccessException("Nedoplatky vidí jen správci.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var circulars = await db.Circulars
            .AsNoTracking()
            .Include(c => c.Club)
            .Include(c => c.Recipients).ThenInclude(r => r.User)
            .Where(c => c.OrganizationId == organizationId
                        && c.Type == CircularType.Debt && c.Status == CircularStatus.Sent)
            .OrderByDescending(c => c.SentAt)
            .ToListAsync();

        return circulars.Select(c => new DebtSummary(
            c.Guid,
            c.Subject,
            c.SentAt,
            c.Club?.Name,
            c.Recipients.Count(r => r.ReadAt.HasValue),
            c.Recipients.Count(r => !r.ReadAt.HasValue),
            c.Recipients.Where(r => !r.ReadAt.HasValue)
                .Select(r => r.User.UserName ?? r.User.Email ?? r.UserId)
                .Take(20).ToList()
        )).ToList();
    }

    // Oddílový oběžník jde i rodičům hráčů — dítě bez účtu by se o nedoplatku jinak nedozvědělo.
    // Celoorganizační rodiče už obsahuje, propojení z nich dělá členy organizace.
    private static IQueryable<string> RecipientIds(AppDbContext db, Guid organizationId, Guid? clubId) =>
        clubId.HasValue
            ? ClubAccessService.ClubAccountIds(db, clubId.Value).Union(ClubAccessService.ClubParentIds(db, clubId.Value))
            : ClubAccessService.OrganizationAccountIds(db, organizationId);

    /// <summary>Nepřečtené odeslané oběžníky účtu napříč organizacemi — pro odznak na Home.</summary>
    public async Task<int> GetUnreadCountAsync(string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.CircularRecipients.CountAsync(r =>
            r.UserId == userId && r.ReadAt == null && r.Circular.Status == CircularStatus.Sent);
    }

    private async Task DeliverAsync(Guid circularId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var circular = await db.Circulars
            .Include(c => c.Recipients).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(c => c.Guid == circularId);
        if (circular is null) return;

        // Text píše uživatel — escapovat, jinak by šlo do e-mailu vložit libovolné HTML.
        var html = $"<p>{WebUtility.HtmlEncode(circular.Body).Replace("\n", "<br>")}</p>";
        var preview = circular.Body.Length > 200 ? circular.Body[..200] + "…" : circular.Body;

        foreach (var r in circular.Recipients)
        {
            if (circular.SendEmail && r.EmailStatus == DeliveryStatus.Pending)
            {
                r.EmailStatus = string.IsNullOrWhiteSpace(r.User.Email)
                    ? DeliveryStatus.Failed
                    : await notifier.SendEmailAsync(r.User.Email, r.User.UserName ?? r.User.Email, circular.Subject, html)
                        ? DeliveryStatus.Sent
                        : DeliveryStatus.Failed;
            }

            if (circular.SendNtfy && r.NtfyStatus == DeliveryStatus.Pending)
            {
                r.NtfyStatus = await notifier.SendNtfyAsync(
                    ClubNotificationService.UserTopic(r.UserId), circular.Subject, preview, NtfyTags(circular.Type))
                    ? DeliveryStatus.Sent
                    : DeliveryStatus.Failed;
            }
        }

        await db.SaveChangesAsync();
    }

    private static string NtfyTags(CircularType type) => type switch
    {
        CircularType.Debt     => "warning,money_with_wings",
        CircularType.Reminder => "bell",
        CircularType.Event    => "calendar",
        _                     => "envelope"
    };
}
