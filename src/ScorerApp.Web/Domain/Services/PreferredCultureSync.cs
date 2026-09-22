using System.Security.Claims;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ScorerApp.Data;

namespace ScorerApp.Domain.Services;

/// <summary>
/// Přepínač jazyka (/set-culture ze SharedServices) ukládá volbu jen do cookie prohlížeče. E-maily a notifikace
/// ale jdou příjemcům na pozadí, kde jejich cookie nevidíme — jazyk proto musí být u účtu (AppUser.PreferredCulture).
/// Login ho odtud zase načte do cookie, takže volba platí i na jiném zařízení.
/// Do DB se sahá jen při změně; poslední známá hodnota se drží v paměti, ať se nezapisuje při každém požadavku.
/// </summary>
public class PreferredCultureSync(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx, IMemoryCache cache, IDbContextFactory<AppDbContext> dbFactory)
    {
        var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId)
            && ctx.Request.Cookies.TryGetValue(CookieRequestCultureProvider.DefaultCookieName, out var raw)
            && CookieRequestCultureProvider.ParseCookieValue(raw)?.UICultures.FirstOrDefault().Value is { } culture
            && CultureScope.Supported.Contains(culture)
            && !(cache.TryGetValue(CacheKey(userId), out string? known) && known == culture))
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            await db.Users
                .Where(u => u.Id == userId && u.PreferredCulture != culture)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.PreferredCulture, culture));
            cache.Set(CacheKey(userId), culture, TimeSpan.FromHours(1));
        }

        await next(ctx);
    }

    private static string CacheKey(string userId) => $"preferred-culture:{userId}";
}
