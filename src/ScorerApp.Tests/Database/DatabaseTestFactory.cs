using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using MercenariesAndBeasts.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedServices.Services.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ScorerApp.Tests.Database;

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DatabaseTestFactory>
{
    public const string Name = "Database";
}

/// <summary>
/// Aplikace nad samostatnou lokální DB ScorerApp_Tests s testovací autentizací (bez hesel).
/// DB se před startem smaže — Program.cs ji při startu vytvoří migracemi, takže testy zároveň
/// ověřují, že migrace včetně AddClubsModule projdou na čisté databázi.
/// Heslo k Postgresu se čte z appsettings.Development.json, aby nebylo zdvojené v kódu testů.
/// </summary>
public class DatabaseTestFactory : WebApplicationFactory<Program>
{
    public const string TestDatabase = "ScorerApp_Tests";

    public string ConnectionString { get; }

    /// <summary>Zprávy, které by aplikace odeslala e-mailem.</summary>
    public FakeEmailService Emails { get; } = new();

    public DatabaseTestFactory()
    {
        ConnectionString = BuildConnectionString();
        RecreateDatabase(ConnectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development zapíná validaci scope v DI — tím testy chytí i chyby jako singleton se scoped závislostí.
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.ConfigureTestServices(services =>
        {
            // Skutečný SMTP v testech ne — falešná služba zprávy zaznamená a testy je čtou.
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            // AddIdentity nastaví výchozí schéma na Identity cookie; PostConfigure běží až po něm, takže ho přebije.
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultScheme = TestAuthHandler.SchemeName;
                o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                o.DefaultForbidScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    /// <summary>Klient přihlášený jako daný účet; <c>null</c> = anonymní požadavek.</summary>
    public HttpClient ClientAs(string? userId, bool isAdmin = false)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (userId is not null) client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userId);
        if (isAdmin) client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, "Admin");
        return client;
    }

    /// <summary>Založí účet bez hesla — testy se přihlašují přes TestAuthHandler, ne přes login.</summary>
    public async Task<string> CreateUserAsync(string prefix)
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = new AppUser
        {
            UserName = $"{prefix}-{suffix}",
            Email = $"{prefix}-{suffix}@test.local",
            EmailConfirmed = true,
            IsWhitelisted = true
        };
        var result = await users.CreateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        return user.Id;
    }

    public async Task<string> EmailOfAsync(string userId)
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        return (await users.FindByIdAsync(userId))!.Email!;
    }

    private static string BuildConnectionString()
    {
        var fromEnv = Environment.GetEnvironmentVariable("SCORERAPP_TEST_DB");
        if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        const string relative = "src/ScorerApp.Web/appsettings.Development.json";
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, relative)))
            dir = dir.Parent;
        if (dir is null)
            throw new InvalidOperationException($"Nenalezen {relative} — nastav proměnnou SCORERAPP_TEST_DB.");

        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir.FullName, relative)));
        var dev = json.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()!;
        return new NpgsqlConnectionStringBuilder(dev) { Database = TestDatabase }.ConnectionString;
    }

    private static void RecreateDatabase(string connectionString)
    {
        var admin = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres", Pooling = false };
        try
        {
            using var connection = new NpgsqlConnection(admin.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE IF EXISTS \"{TestDatabase}\" WITH (FORCE)";
            command.ExecuteNonQuery();
        }
        catch (NpgsqlException ex)
        {
            throw new InvalidOperationException(
                "Databázové testy potřebují lokální Postgres (~/scorerapp-deploy-prep/10-dev-db-start.sh). " +
                "Bez něj je vynech: dotnet test --filter Category!=Database", ex);
        }
    }
}

/// <summary>
/// Přihlásí požadavek podle hlaviček — účet a role bez hesla a bez cookie.
/// Chybějící hlavička = anonymní požadavek, takže [Authorize] vrátí 401 a lze ho ověřit.
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserHeader = "X-Test-UserId";
    public const string RolesHeader = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers[UserHeader].ToString();
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId)
        };
        foreach (var role in Request.Headers[RolesHeader].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries))
            claims.Add(new Claim(ClaimTypes.Role, role.Trim()));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
