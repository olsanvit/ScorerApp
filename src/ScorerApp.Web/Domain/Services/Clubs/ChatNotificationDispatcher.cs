using System.Net;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using ScorerApp.Data;
using ScorerApp.Domain.Models.Clubs;

namespace ScorerApp.Domain.Services.Clubs;

public record ChatNotificationJob(Guid MessageId);

/// <summary>
/// E-mail a ntfy k chatovým zprávám mimo request — odesílatel nečeká na SMTP.
/// Oproti ClubManageru respektuje NotificationPreference.MinPriority a posílá i ntfy.
/// Služby s DB přístupem si bere z nového scope pro každou zprávu — dispatcher je singleton
/// a scoped služby (notifier s HttpClientem, DbContext) nesmí držet po celou dobu běhu aplikace.
/// </summary>
public class ChatNotificationDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<ChatNotificationDispatcher> logger) : BackgroundService
{
    private readonly Channel<ChatNotificationJob> _channel = Channel.CreateUnbounded<ChatNotificationJob>();

    public void Enqueue(ChatNotificationJob job) => _channel.Writer.TryWrite(job);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(ct))
        {
            try { await ProcessAsync(job, ct); }
            catch (Exception ex) { logger.LogError(ex, "Notifikace k chatové zprávě {Id} selhala", job.MessageId); }
        }
    }

    /// <summary>Priorita zprávy ve stejné škále jako NotifyMinPriority (All &lt; High &lt; Urgent).</summary>
    public static NotifyMinPriority PriorityOf(ThreadType type) => type switch
    {
        ThreadType.Debt                             => NotifyMinPriority.Urgent,
        ThreadType.Announcement or ThreadType.Event => NotifyMinPriority.High,
        _                                           => NotifyMinPriority.All
    };

    /// <summary>
    /// Debt se doručuje e-mailem vždy (spec: ignoruje preference). Bez uložené preference
    /// chodí e-mail jen u důležitých vláken, aby běžný chat nezahltil schránky.
    /// </summary>
    public static (bool Email, bool Ntfy) Channels(ThreadType type, NotificationPreference? pref)
    {
        var priority = PriorityOf(type);
        if (type == ThreadType.Debt) return (true, pref?.NtfyEnabled ?? false);
        if (pref is null) return (priority >= NotifyMinPriority.High, false);

        var meets = priority >= pref.MinPriority;
        return (meets && pref.EmailEnabled, meets && pref.NtfyEnabled);
    }

    private async Task ProcessAsync(ChatNotificationJob job, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var notifier = scope.ServiceProvider.GetRequiredService<ClubNotificationService>();

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var msg = await db.ChatMessages
            .AsNoTracking()
            .Include(m => m.Thread).ThenInclude(t => t.Club)
            .Include(m => m.SenderUser)
            .FirstOrDefaultAsync(m => m.Guid == job.MessageId, ct);
        if (msg is null) return;

        var recipientIds = await ClubAccessService.ClubAccountIds(db, msg.Thread.ClubId)
            .Where(id => id != msg.SenderUserId)
            .ToListAsync(ct);
        if (recipientIds.Count == 0) return;

        var prefs = await db.NotificationPreferences
            .Where(p => p.ClubId == msg.Thread.ClubId && recipientIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, ct);
        var users = await db.Users
            .Where(u => recipientIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.UserName })
            .ToListAsync(ct);

        var sender = msg.SenderUser.UserName ?? msg.SenderUserId;
        var subject = $"[{msg.Thread.Club.Name}] {msg.Thread.Title}";
        // Obsah zprávy i názvy píšou uživatelé — bez escapování by šlo do e-mailu vložit HTML.
        var html = $"<p><strong>{WebUtility.HtmlEncode(sender)}</strong> napsal(a) ve vlákně " +
                   $"<em>{WebUtility.HtmlEncode(msg.Thread.Title)}</em>:</p>" +
                   $"<blockquote>{WebUtility.HtmlEncode(msg.Body).Replace("\n", "<br>")}</blockquote>";
        var preview = msg.Body.Length > 200 ? msg.Body[..200] + "…" : msg.Body;

        foreach (var user in users)
        {
            var (email, ntfy) = Channels(msg.Thread.ThreadType, prefs.GetValueOrDefault(user.Id));

            if (email && !string.IsNullOrWhiteSpace(user.Email))
                await notifier.SendEmailAsync(user.Email, user.UserName ?? user.Email, subject, html);
            if (ntfy)
                await notifier.SendNtfyAsync(ClubNotificationService.UserTopic(user.Id), subject, $"{sender}: {preview}");
        }
    }
}
