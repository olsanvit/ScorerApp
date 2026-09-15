using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace ScorerApp.Domain.Services.Clubs;

public record ChatMessageDto(
    Guid Id, Guid ThreadId, string SenderUserId, string SenderName, string Body, DateTimeOffset CreatedAt);

/// <summary>
/// Doručení nových zpráv otevřeným chatům. Blazor Server circuity běží ve stejném procesu,
/// takže stačí pub/sub v paměti místo SignalR hubu. ClubManager otevíral HubConnection ze serveru
/// bez auth cookie a [Authorize] hub mu vracel 401. Platí pro jednu instanci aplikace (jeden
/// kontejner); při škálování na víc instancí by to potřebovalo backplane.
/// </summary>
public class ClubChatBroadcaster(ILogger<ClubChatBroadcaster> logger)
{
    private readonly ConcurrentDictionary<Guid, ImmutableList<Func<ChatMessageDto, Task>>> _subscribers = new();

    public IDisposable Subscribe(Guid threadId, Func<ChatMessageDto, Task> handler)
    {
        _subscribers.AddOrUpdate(threadId,
            _ => ImmutableList.Create(handler),
            (_, list) => list.Add(handler));

        return new Subscription(() => _subscribers.AddOrUpdate(threadId,
            _ => ImmutableList<Func<ChatMessageDto, Task>>.Empty,
            (_, list) => list.Remove(handler)));
    }

    public async Task PublishAsync(ChatMessageDto message)
    {
        if (!_subscribers.TryGetValue(message.ThreadId, out var handlers)) return;

        foreach (var handler in handlers)
        {
            // Jeden odpojený circuit nesmí zastavit doručení ostatním.
            try { await handler(message); }
            catch (Exception ex) { logger.LogWarning(ex, "Doručení zprávy {Id} do chatu selhalo", message.Id); }
        }
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) unsubscribe();
        }
    }
}
