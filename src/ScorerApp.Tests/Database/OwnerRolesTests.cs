using MercenariesAndBeasts.Infrastructure;
using MercenariesAndBeasts.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ScorerApp.Data;

namespace ScorerApp.Tests.Database;

/// <summary>
/// Vlastník přihlášený poprvé přes Google vznikl bez rolí a Admina neviděl (2026-10-15).
/// Role se mu doplní při přihlášení; nikomu jinému ne.
/// </summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class OwnerRolesTests(DatabaseTestFactory factory)
{
    [Fact]
    public async Task OtherUser_GetsNoRoles()
    {
        var id = await factory.CreateUserAsync("notowner");
        using var scope = factory.Services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = (await um.FindByIdAsync(id))!;

        Assert.False(await OwnerRoles.EnsureAsync(um, user));
        Assert.Empty(await um.GetRolesAsync(user));
    }

    [Fact]
    public async Task Owner_GetsAllRoles_OnlyOnce()
    {
        using var scope = factory.Services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await um.FindByEmailAsync(AccessGate.OwnerEmail);
        var created = user is null;
        if (created)
        {
            user = new AppUser { UserName = "owner-test", Email = AccessGate.OwnerEmail, EmailConfirmed = true };
            Assert.True((await um.CreateAsync(user)).Succeeded);
        }
        var originalRoles = await um.GetRolesAsync(user!);
        var originalIsAdmin = user!.IsAdmin;
        foreach (var role in originalRoles)
            await um.RemoveFromRoleAsync(user, role);

        try
        {
            Assert.True(await OwnerRoles.EnsureAsync(um, user!));
            Assert.Equal(OwnerRoles.All.Order(), (await um.GetRolesAsync(user!)).Order());
            Assert.True(user!.IsAdmin);
            Assert.False(await OwnerRoles.EnsureAsync(um, user));
        }
        finally
        {
            // Sdílená testovací DB: další admin by rozbil AdminUsersTests („posledního admina nelze odebrat“).
            if (created) await um.DeleteAsync(user!);
            else
            {
                foreach (var role in await um.GetRolesAsync(user!)) await um.RemoveFromRoleAsync(user!, role);
                foreach (var role in originalRoles) await um.AddToRoleAsync(user!, role);
                user!.IsAdmin = originalIsAdmin;
                await um.UpdateAsync(user);
            }
        }
    }

    [Theory]
    [InlineData("olsanskyvitek@gmail.com", true)]
    [InlineData("OlsanskyVitek@Gmail.com", true)]
    [InlineData("olsansky575@gmail.com", false)]
    [InlineData(null, false)]
    public void IsOwner_ComparesCaseInsensitively(string? email, bool expected) =>
        Assert.Equal(expected, OwnerRoles.IsOwner(email));
}
