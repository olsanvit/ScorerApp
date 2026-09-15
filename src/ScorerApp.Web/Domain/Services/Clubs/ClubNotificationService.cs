using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ScorerApp.Domain.Services.Clubs;

public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
}

public class NtfySettings
{
    /// <summary>
    /// Výchozí prázdné = ntfy vypnuté. ClubManager měl výchozí veřejné ntfy.sh, kam by se
    /// při nenastavené konfiguraci posílal obsah klubových zpráv komukoli, kdo zná topic.
    /// </summary>
    public string BaseUrl { get; set; } = "";
    public string? Auth { get; set; }
}

public class ClubNotificationService(
    IOptions<SmtpSettings> smtp,
    IOptions<NtfySettings> ntfy,
    HttpClient http,
    ILogger<ClubNotificationService> logger)
{
    private readonly SmtpSettings _smtp = smtp.Value;
    private readonly NtfySettings _ntfy = ntfy.Value;

    /// <summary>Tělo musí být už escapované HTML — služba ho posílá tak, jak je.</summary>
    public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_smtp.Host) || string.IsNullOrWhiteSpace(_smtp.User))
        {
            logger.LogWarning("SMTP není nastavené, e-mail pro {Email} se neposílá", toEmail);
            return false;
        }

        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("ScorerApp", _smtp.From));
            msg.To.Add(new MailboxAddress(toName, toEmail));
            msg.Subject = subject;
            msg.Body = new TextPart("html") { Text = htmlBody };

            // SmtpClient z MailKitu není thread-safe, proto nový pro každé odeslání.
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtp.User, _smtp.Password);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Odeslání e-mailu na {Email} selhalo", toEmail);
            return false;
        }
    }

    public async Task<bool> SendNtfyAsync(string topic, string title, string message, string? tags = null)
    {
        if (string.IsNullOrWhiteSpace(_ntfy.BaseUrl)) return false;

        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"{_ntfy.BaseUrl.TrimEnd('/')}/{topic}")
            {
                Content = new StringContent(message)
            };
            req.Headers.Add("Title", title);
            if (!string.IsNullOrWhiteSpace(tags)) req.Headers.Add("Tags", tags);
            if (!string.IsNullOrWhiteSpace(_ntfy.Auth)) req.Headers.Add("Authorization", _ntfy.Auth);

            var resp = await http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Odeslání ntfy na topic {Topic} selhalo", topic);
            return false;
        }
    }

    /// <summary>
    /// Topic odvozený z Id účtu. ntfy topicy nejsou tajné, takže na serveru bez Ntfy:Auth
    /// je může odebírat kdokoli, kdo prefix odhadne — pro citlivé zprávy nastav autorizaci.
    /// </summary>
    public static string UserTopic(string userId)
    {
        var compact = userId.Replace("-", "");
        return "scorerapp-" + (compact.Length > 8 ? compact[..8] : compact);
    }
}
