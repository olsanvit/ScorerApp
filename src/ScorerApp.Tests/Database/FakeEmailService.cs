using System.Collections.Concurrent;
using SharedServices.Services.Email;

namespace ScorerApp.Tests.Database;

/// <summary>Místo SMTP jen zaznamená zprávy — testy z nich čtou, co by se odeslalo (a v jakém jazyce).</summary>
public sealed class FakeEmailService : IEmailService
{
    public ConcurrentQueue<EmailMessage> Sent { get; } = new();

    public Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        Sent.Enqueue(message);
        return Task.FromResult(true);
    }

    public void Enqueue(EmailMessage message) => Sent.Enqueue(message);
}
