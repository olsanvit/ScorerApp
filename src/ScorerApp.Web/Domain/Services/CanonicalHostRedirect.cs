using System.Net;

namespace ScorerApp.Domain.Services;

/// <summary>
/// Aplikace je za tunelem dostupná na scorer.vo2info.cz i scorerapp.vo2info.cz. Google OAuth vrací jen na
/// adresy povolené v Google Console, takže přihlášení z druhé domény padalo na redirect_uri_mismatch
/// (2026-10-14). Proto se každá veřejná doména trvale přesměruje na tu z App:BaseUrl.
/// </summary>
public static class CanonicalHostRedirect
{
    /// <summary>Kam přesměrovat, nebo null. IP adresy a localhost (smoke test deploye, LAN) se nepřesměrovávají.</summary>
    public static string? Target(string? baseUrl, string requestHost, string pathBase, string path, string query)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var canonical)) return null;
        if (string.IsNullOrEmpty(requestHost)) return null;

        var host = requestHost.Split(':')[0];
        if (host.Equals(canonical.Host, StringComparison.OrdinalIgnoreCase)) return null;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || IPAddress.TryParse(host.Trim('[', ']'), out _))
            return null;
        // Bez tečky je to jméno v LAN/docker síti (např. název kontejneru), ne veřejná doména.
        if (!host.Contains('.')) return null;

        return $"{canonical.GetLeftPart(UriPartial.Authority)}{pathBase}{path}{query}";
    }

    public static WebApplication UseCanonicalHost(this WebApplication app)
    {
        var baseUrl = app.Configuration["App:BaseUrl"];
        if (app.Environment.IsDevelopment() || string.IsNullOrWhiteSpace(baseUrl)) return app;

        app.Use(async (ctx, next) =>
        {
            var r = ctx.Request;
            var target = Target(baseUrl, r.Host.Value ?? "", r.PathBase, r.Path, r.QueryString.Value ?? "");
            if (target is null) { await next(); return; }
            ctx.Response.Redirect(target, permanent: true);
        });
        return app;
    }
}
