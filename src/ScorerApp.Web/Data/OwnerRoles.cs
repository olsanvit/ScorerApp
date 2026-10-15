using MercenariesAndBeasts.Infrastructure;
using MercenariesAndBeasts.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;

namespace ScorerApp.Data;

/// <summary>
/// Role vlastníka aplikace. Seed při startu je doplní jen účtu, který už existuje — účet vzniklý až prvním
/// přihlášením přes Google (bez Seed:AdminPassword se jinak nezaloží) zůstal bez rolí a Admina neviděl
/// (2026-10-15). Proto se volá i po každém externím přihlášení.
/// </summary>
public static class OwnerRoles
{
    public static readonly string[] All = ["Admin", "Moderator", "LoginUser"];

    public static bool IsOwner(string? email) =>
        string.Equals(email, AccessGate.OwnerEmail, StringComparison.OrdinalIgnoreCase);

    /// <summary>Doplní vlastníkovi chybějící role. Vrací true, pokud se něco změnilo (pak je třeba obnovit cookie).</summary>
    public static async Task<bool> EnsureAsync(UserManager<AppUser> um, AppUser user)
    {
        if (!IsOwner(user.Email)) return false;

        var changed = false;
        if (!user.IsAdmin)
        {
            user.IsAdmin = true;
            await um.UpdateAsync(user);
            changed = true;
        }
        foreach (var role in All)
        {
            if (await um.IsInRoleAsync(user, role)) continue;
            await um.AddToRoleAsync(user, role);
            changed = true;
        }
        return changed;
    }
}
