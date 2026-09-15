using Microsoft.EntityFrameworkCore;
using ScorerApp.Data;
using ScorerApp.Domain.Models.Clubs;

namespace ScorerApp.Domain.Services.Clubs;

public class ChatService(
    IDbContextFactory<AppDbContext> dbFactory,
    ClubAccessService access,
    ClubChatBroadcaster broadcaster,
    ChatNotificationDispatcher dispatcher)
{
    public const int MaxBodyLength = 4000;

    /// <summary>Vlákno smí založit jen správce oddílu (spec). V ClubManageru to nešlo vůbec — chyběl spouštěč i kontrola.</summary>
    public async Task<ClubThread> CreateThreadAsync(Guid clubId, string title, ThreadType type, string userId, bool isSiteAdmin)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Zadej název vlákna.", nameof(title));
        if (!await access.CanManageClubAsync(clubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Vlákno smí založit jen správce oddílu.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var thread = new ClubThread
        {
            ClubId          = clubId,
            Title           = title.Trim(),
            ThreadType      = type,
            CreatedByUserId = userId
        };
        db.ClubThreads.Add(thread);
        await db.SaveChangesAsync();
        return thread;
    }

    public async Task<List<ClubThread>> GetThreadsForUserAsync(string userId, bool isSiteAdmin)
    {
        var clubIds = await access.GetParticipantClubIdsAsync(userId, isSiteAdmin);

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ClubThreads
            .AsNoTracking()
            .Include(t => t.Club)
            .Where(t => clubIds.Contains(t.ClubId) && !t.IsArchived)
            .OrderBy(t => t.Club.Name).ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Posledních <paramref name="take"/> zpráv ve vzestupném pořadí — feed se čte odshora dolů.</summary>
    public async Task<List<ChatMessage>> GetMessagesAsync(Guid threadId, string userId, bool isSiteAdmin, int take = 100)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var clubId = await db.ClubThreads
            .Where(t => t.Guid == threadId)
            .Select(t => (Guid?)t.ClubId)
            .FirstOrDefaultAsync();
        if (clubId is null || !await access.IsClubParticipantAsync(clubId.Value, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Do tohoto vlákna nemáš přístup.");

        var latest = await db.ChatMessages
            .AsNoTracking()
            .Include(m => m.SenderUser)
            .Where(m => m.ThreadId == threadId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .ToListAsync();
        latest.Reverse();
        return latest;
    }

    public async Task<ChatMessage> SendMessageAsync(Guid threadId, string userId, bool isSiteAdmin, string body)
    {
        body = body?.Trim() ?? "";
        if (body.Length == 0)
            throw new ArgumentException("Zpráva je prázdná.", nameof(body));
        if (body.Length > MaxBodyLength)
            throw new ArgumentException($"Zpráva je delší než {MaxBodyLength} znaků.", nameof(body));

        await using var db = await dbFactory.CreateDbContextAsync();
        var thread = await db.ClubThreads.FirstOrDefaultAsync(t => t.Guid == threadId)
            ?? throw new InvalidOperationException("Vlákno neexistuje.");
        if (thread.IsArchived)
            throw new InvalidOperationException("Vlákno je archivované.");
        if (!await access.IsClubParticipantAsync(thread.ClubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Do tohoto vlákna nemáš přístup.");

        var msg = new ChatMessage { ThreadId = threadId, SenderUserId = userId, Body = body };
        db.ChatMessages.Add(msg);
        await db.SaveChangesAsync();

        var senderName = await db.Users.Where(u => u.Id == userId).Select(u => u.UserName).FirstOrDefaultAsync();
        await broadcaster.PublishAsync(new ChatMessageDto(
            msg.Guid, threadId, userId, senderName ?? userId, body, msg.CreatedAt));
        dispatcher.Enqueue(new ChatNotificationJob(msg.Guid));
        return msg;
    }

    public async Task MarkThreadReadAsync(Guid threadId, string userId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var unread = await db.ChatMessages
            .Where(m => m.ThreadId == threadId && m.SenderUserId != userId
                        && !m.Reads.Any(r => r.UserId == userId))
            .Select(m => m.Guid)
            .ToListAsync();
        if (unread.Count == 0) return;

        db.ChatMessageReads.AddRange(unread.Select(id => new ChatMessageRead { MessageId = id, UserId = userId }));
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Stejný uživatel ve dvou záložkách označil totéž současně — unikátní index to zachytí
            // a výsledek („přečteno“) je stejný, takže chybu nemá smysl propagovat.
        }
    }

    public async Task<Dictionary<Guid, int>> GetUnreadCountsAsync(string userId, bool isSiteAdmin)
    {
        var clubIds = await access.GetParticipantClubIdsAsync(userId, isSiteAdmin);

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ChatMessages
            .Where(m => clubIds.Contains(m.Thread.ClubId) && !m.Thread.IsArchived
                        && m.SenderUserId != userId && !m.Reads.Any(r => r.UserId == userId))
            .GroupBy(m => m.ThreadId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    public async Task ArchiveThreadAsync(Guid threadId, string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var thread = await db.ClubThreads.FirstOrDefaultAsync(t => t.Guid == threadId)
            ?? throw new InvalidOperationException("Vlákno neexistuje.");
        if (!await access.CanManageClubAsync(thread.ClubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Vlákno smí archivovat jen správce oddílu.");

        thread.IsArchived = true;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Nastavení notifikací účtu pro oddíl. Bez uloženého záznamu vrací neuloženou instanci s výchozími
    /// hodnotami třídy — ty odpovídají chování dispatcheru bez preference, takže UI ukazuje skutečný stav.
    /// </summary>
    public async Task<NotificationPreference> GetPreferenceAsync(Guid clubId, string userId, bool isSiteAdmin)
    {
        if (!await access.IsClubParticipantAsync(clubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Nastavení notifikací je jen pro členy oddílu.");

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.NotificationPreferences
                   .AsNoTracking()
                   .FirstOrDefaultAsync(p => p.ClubId == clubId && p.UserId == userId)
               ?? new NotificationPreference { ClubId = clubId, UserId = userId };
    }

    public async Task SavePreferenceAsync(
        Guid clubId, string userId, bool isSiteAdmin, bool emailEnabled, bool ntfyEnabled, NotifyMinPriority minPriority)
    {
        if (!await access.IsClubParticipantAsync(clubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException("Nastavení notifikací je jen pro členy oddílu.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var preference = await db.NotificationPreferences.FirstOrDefaultAsync(p => p.ClubId == clubId && p.UserId == userId);
        if (preference is null)
        {
            preference = new NotificationPreference { ClubId = clubId, UserId = userId };
            db.NotificationPreferences.Add(preference);
        }

        preference.EmailEnabled = emailEnabled;
        preference.NtfyEnabled  = ntfyEnabled;
        preference.MinPriority  = minPriority;
        await db.SaveChangesAsync();
    }
}
