using MercenariesAndBeasts.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ScorerApp.Data;

/// <summary>
/// Identity tu nevyžaduje unikátní e-mail a Google callback dřív zakládal nový účet, i když účet se stejným
/// e-mailem už existoval. Vlastník tak měl dva účty (seed „vitek“ s Admin a Google účet bez rolí) a
/// FindByEmailAsync pak padal na „Sequence contains more than one element“ (2026-10-15).
/// </summary>
public static class DuplicateAccounts
{
    /// <summary>Hlavní účet pro e-mail: přednost má admin, pak účet s heslem (ten ze seedu), pak nejstarší podle Id.</summary>
    public static async Task<AppUser?> FindPrimaryByEmailAsync(UserManager<AppUser> um, string email)
    {
        var normalized = um.NormalizeEmail(email);
        return await um.Users
            .Where(u => u.NormalizedEmail == normalized)
            .OrderByDescending(u => u.IsAdmin)
            .ThenByDescending(u => u.PasswordHash != null)
            .ThenBy(u => u.Id)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Přesune externí přihlášení z duplicitních účtů na hlavní a duplicity zablokuje. Nemaže je — mohou na ně
    /// odkazovat data; e-mail se jim přepíše, aby hledání podle e-mailu bylo zase jednoznačné.
    /// </summary>
    public static async Task<int> MergeAsync(UserManager<AppUser> um, string email)
    {
        var primary = await FindPrimaryByEmailAsync(um, email);
        if (primary is null) return 0;

        var normalized = um.NormalizeEmail(email);
        var duplicates = await um.Users
            .Where(u => u.NormalizedEmail == normalized && u.Id != primary.Id)
            .ToListAsync();

        foreach (var dup in duplicates)
        {
            var primaryProviders = (await um.GetLoginsAsync(primary)).Select(l => l.LoginProvider).ToHashSet();
            foreach (var login in await um.GetLoginsAsync(dup))
            {
                // Odebrat dřív, než se přidá — (provider, key) je primární klíč, na dvou účtech být nemůže.
                await um.RemoveLoginAsync(dup, login.LoginProvider, login.ProviderKey);
                if (!primaryProviders.Contains(login.LoginProvider))
                    await um.AddLoginAsync(primary, login);
            }

            await um.SetEmailAsync(dup, $"merged-{dup.Id}@invalid.local");
            await um.SetLockoutEnabledAsync(dup, true);
            await um.SetLockoutEndDateAsync(dup, DateTimeOffset.MaxValue);
            Log.Warning("Duplicitní účet {DupId} pro {Email} sloučen do {PrimaryId} a zablokován", dup.Id, email, primary.Id);
        }
        return duplicates.Count;
    }
}
