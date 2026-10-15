using MercenariesAndBeasts.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ScorerApp.Data;

namespace ScorerApp.Tests.Database;

/// <summary>Google login dřív zakládal druhý účet se stejným e-mailem — vlastník pak neměl Admina (2026-10-15).</summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class DuplicateAccountsTests(DatabaseTestFactory factory)
{
    [Fact]
    public async Task Merge_MovesExternalLoginToAdminAccount_AndLocksDuplicate()
    {
        using var scope = factory.Services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var email = $"dup-{Guid.NewGuid():N}@test.local";

        var primary = new AppUser { UserName = $"p-{Guid.NewGuid():N}", Email = email, IsAdmin = true };
        var dup = new AppUser { UserName = $"d-{Guid.NewGuid():N}", Email = email };
        Assert.True((await um.CreateAsync(primary)).Succeeded);
        Assert.True((await um.CreateAsync(dup)).Succeeded);
        var key = Guid.NewGuid().ToString("N");
        Assert.True((await um.AddLoginAsync(dup, new UserLoginInfo("Google", key, "Google"))).Succeeded);

        // Hlavní účet se pozná i bez sloučení — callback ho použije místo založení dalšího.
        Assert.Equal(primary.Id, (await DuplicateAccounts.FindPrimaryByEmailAsync(um, email))!.Id);

        Assert.Equal(1, await DuplicateAccounts.MergeAsync(um, email));

        Assert.Equal(primary.Id, (await um.FindByLoginAsync("Google", key))!.Id);
        Assert.Equal(primary.Id, (await um.FindByEmailAsync(email))!.Id);
        var locked = (await um.FindByIdAsync(dup.Id))!;
        Assert.True(await um.IsLockedOutAsync(locked));
        Assert.Empty(await um.GetLoginsAsync(locked));

        Assert.Equal(0, await DuplicateAccounts.MergeAsync(um, email));
    }
}
