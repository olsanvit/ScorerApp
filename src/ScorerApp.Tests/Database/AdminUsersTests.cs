using System.Net;
using MercenariesAndBeasts.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SharedServices.Services.Admin;

namespace ScorerApp.Tests.Database;

/// <summary>Správa sub-adminů (/admin/users) — role Admin řídí přístup do celé administrace.</summary>
[Collection(DatabaseCollection.Name)]
[Trait("Category", "Database")]
public class AdminUsersTests(DatabaseTestFactory factory)
{
    [Fact]
    public async Task AdminRole_CanBeGrantedAndRevoked_ButNotFromSelfOrLastAdmin()
    {
        var first = await factory.CreateUserAsync("adminone");
        var second = await factory.CreateUserAsync("admintwo");

        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AdminUserService>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        Assert.True((await service.SetAdminAsync(first, makeAdmin: true, currentUserId: first)).Ok);
        Assert.True(await users.IsInRoleAsync((await users.FindByIdAsync(first))!, AdminUserService.AdminRole));
        Assert.True((await users.FindByIdAsync(first))!.IsAdmin);

        var email = await factory.EmailOfAsync(first);
        var found = Assert.Single(await service.SearchAsync(email));
        Assert.True(found.IsAdmin);

        // Sobě ani poslednímu adminovi roli odebrat nelze — aplikace by zůstala bez správce.
        Assert.False((await service.SetAdminAsync(first, makeAdmin: false, currentUserId: first)).Ok);
        var lastAdmin = await service.SetAdminAsync(first, makeAdmin: false, currentUserId: second);
        Assert.False(lastAdmin.Ok);
        Assert.NotNull(lastAdmin.Error);

        Assert.True((await service.SetAdminAsync(second, makeAdmin: true, currentUserId: first)).Ok);
        Assert.True((await service.SetAdminAsync(first, makeAdmin: false, currentUserId: second)).Ok);
        Assert.False(await users.IsInRoleAsync((await users.FindByIdAsync(first))!, AdminUserService.AdminRole));
        Assert.False((await users.FindByIdAsync(first))!.IsAdmin);
    }

    [Fact]
    public async Task UsersPage_RendersForAdmin_AndForbidsOthers()
    {
        var admin = await factory.CreateUserAsync("padminusers");

        using (var client = factory.ClientAs(admin, isAdmin: true))
        using (var response = await client.GetAsync("/admin/users"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            // Seznam ukazuje prvních 50 účtů podle e-mailu — konkrétní účet v něm být nemusí, tabulka ano.
            Assert.Contains("@test.local", await response.Content.ReadAsStringAsync());
        }

        using var plain = factory.ClientAs(await factory.CreateUserAsync("plain"));
        using var forbidden = await plain.GetAsync("/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }
}
