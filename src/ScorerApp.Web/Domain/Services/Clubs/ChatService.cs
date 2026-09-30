using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using ScorerApp.Data;
using ScorerApp.Domain.Models.Clubs;

namespace ScorerApp.Domain.Services.Clubs;

public class ChatService(
    IDbContextFactory<AppDbContext> dbFactory,
    ClubAccessService access,
    ClubChatBroadcaster broadcaster,
    ChatNotificationDispatcher dispatcher,
    IStringLocalizer<SharedResource> S)
{
    public const int MaxBodyLength = 4000;

    /// <summary>Vlákno smí založit jen správce oddílu (spec). V ClubManageru to nešlo vůbec — chyběl spouštěč i kontrola.</summary>
    public async Task<ClubThread> CreateThreadAsync(Guid clubId, string title, ThreadType type, string userId, bool isSiteAdmin)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(S["ClubErr_ThreadTitleRequired"], nameof(title));
        if (!await access.CanManageClubAsync(clubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException(S["ClubErr_ThreadCreateManagerOnly"]);

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


    /// <summary>
    /// Otevře soukromou konverzaci s druhým členem oddílu; existující vlákno vrátí místo zakládání nového.
    /// DM je vlákno oddílu s explicitními účastníky — oprávnění tak dál stojí na členství v oddílu
    /// a nebylo potřeba měnit schéma (ClubId zůstává povinné).
    /// </summary>
    public async Task<ClubThread> StartDirectThreadAsync(Guid clubId, string otherUserId, string userId, bool isSiteAdmin)
    {
        if (string.IsNullOrWhiteSpace(otherUserId) || otherUserId == userId)
            throw new ArgumentException(S["ClubErr_DirectSelf"], nameof(otherUserId));
        if (!await access.IsClubParticipantAsync(clubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException(S["ClubErr_ThreadAccessDenied"]);
        // Druhá strana musí být členem téhož oddílu, jinak by šlo psát komukoli v aplikaci.
        if (!await access.IsClubParticipantAsync(clubId, otherUserId, isSiteAdmin: false))
            throw new ArgumentException(S["ClubErr_DirectNotMember"], nameof(otherUserId));

        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.ClubThreads
            .Where(t => t.ClubId == clubId && t.ThreadType == ThreadType.Direct && !t.IsArchived
                        && t.Participants.Any(p => p.UserId == userId)
                        && t.Participants.Any(p => p.UserId == otherUserId)
                        && t.Participants.Count == 2)
            .FirstOrDefaultAsync();
        if (existing is not null) return existing;

        var otherName = await db.Users.Where(u => u.Id == otherUserId)
            .Select(u => u.UserName ?? u.Email).FirstOrDefaultAsync() ?? otherUserId;
        var thread = new ClubThread
        {
            ClubId          = clubId,
            Title           = otherName!,
            ThreadType      = ThreadType.Direct,
            CreatedByUserId = userId,
            Participants =
            [
                new ThreadParticipant { UserId = userId },
                new ThreadParticipant { UserId = otherUserId }
            ]
        };
        db.ClubThreads.Add(thread);
        await db.SaveChangesAsync();
        return thread;
    }


    /// <summary>Členové oddílu s účtem, kterým lze napsat soukromě (bez volajícího).</summary>
    public async Task<List<(string UserId, string Name)>> GetDirectCandidatesAsync(Guid clubId, string userId, bool isSiteAdmin)
    {
        if (!await access.IsClubParticipantAsync(clubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException(S["ClubErr_ThreadAccessDenied"]);

        await using var db = await dbFactory.CreateDbContextAsync();
        var ids = await ClubAccessService.ClubAccountIds(db, clubId).Where(id => id != userId).ToListAsync();
        return await db.Users
            .Where(u => ids.Contains(u.Id))
            .OrderBy(u => u.UserName)
            .Select(u => new ValueTuple<string, string>(u.Id, u.UserName ?? u.Email ?? u.Id))
            .ToListAsync();
    }

    /// <summary>Členství v oddílu nestačí — soukromé vlákno vidí jen jeho účastníci.</summary>
    private static bool CanSeeThread(ClubThread thread, string userId) =>
        thread.ThreadType != ThreadType.Direct || thread.Participants.Any(p => p.UserId == userId);

    public async Task<List<ClubThread>> GetThreadsForUserAsync(string userId, bool isSiteAdmin)
    {
        var clubIds = await access.GetParticipantClubIdsAsync(userId, isSiteAdmin);

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.ClubThreads
            .AsNoTracking()
            .Include(t => t.Club)
            .Include(t => t.Participants)
            .Where(t => clubIds.Contains(t.ClubId) && !t.IsArchived
                        && (t.ThreadType != ThreadType.Direct
                            || t.Participants.Any(p => p.UserId == userId)))
            .OrderBy(t => t.Club.Name).ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Posledních <paramref name="take"/> zpráv ve vzestupném pořadí — feed se čte odshora dolů.</summary>
    public async Task<List<ChatMessage>> GetMessagesAsync(Guid threadId, string userId, bool isSiteAdmin, int take = 100)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var thread = await db.ClubThreads
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Guid == threadId);
        if (thread is null
            || !await access.IsClubParticipantAsync(thread.ClubId, userId, isSiteAdmin)
            || !CanSeeThread(thread, userId))
            throw new UnauthorizedAccessException(S["ClubErr_ThreadAccessDenied"]);

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
            throw new ArgumentException(S["ClubErr_MessageEmpty"], nameof(body));
        if (body.Length > MaxBodyLength)
            throw new ArgumentException(S["ClubErr_MessageTooLong", MaxBodyLength], nameof(body));

        await using var db = await dbFactory.CreateDbContextAsync();
        var thread = await db.ClubThreads
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.Guid == threadId)
            ?? throw new InvalidOperationException(S["ClubErr_ThreadNotFound"]);
        if (thread.IsArchived)
            throw new InvalidOperationException(S["ClubErr_ThreadArchived"]);
        if (!await access.IsClubParticipantAsync(thread.ClubId, userId, isSiteAdmin) || !CanSeeThread(thread, userId))
            throw new UnauthorizedAccessException(S["ClubErr_ThreadAccessDenied"]);

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
                        && (m.Thread.ThreadType != ThreadType.Direct
                            || m.Thread.Participants.Any(p => p.UserId == userId))
                        && m.SenderUserId != userId && !m.Reads.Any(r => r.UserId == userId))
            .GroupBy(m => m.ThreadId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    public async Task ArchiveThreadAsync(Guid threadId, string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var thread = await db.ClubThreads
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.Guid == threadId)
            ?? throw new InvalidOperationException(S["ClubErr_ThreadNotFound"]);
        if (!await access.CanManageClubAsync(thread.ClubId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException(S["ClubErr_ThreadArchiveManagerOnly"]);

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
            throw new UnauthorizedAccessException(S["ClubErr_NotificationSettingsMembersOnly"]);

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
            throw new UnauthorizedAccessException(S["ClubErr_NotificationSettingsMembersOnly"]);

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
