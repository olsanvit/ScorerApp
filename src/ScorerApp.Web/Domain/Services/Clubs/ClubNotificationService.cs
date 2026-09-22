using SharedServices.Services.Email;
using Microsoft.Extensions.Options;

namespace ScorerApp.Domain.Services.Clubs;

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
    IEmailService email,
    IOptions<NtfySettings> ntfy,
    HttpClient http,
    ILogger<ClubNotificationService> logger)
{
    private readonly NtfySettings _ntfy = ntfy.Value;

    /// <summary>
    /// Tělo musí být už escapované HTML. Odesílá společná služba ze SharedServices (Email:Smtp) — oběžník si
    /// eviduje doručení per příjemce, proto přímé odeslání s výsledkem, ne fronta.
    /// </summary>
    public Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody) =>
        email.SendAsync(new EmailMessage(toEmail, subject, htmlBody, toName));

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
