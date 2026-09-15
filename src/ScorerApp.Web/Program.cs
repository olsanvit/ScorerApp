using ApexCharts;
using MercenariesAndBeasts.Infrastructure;
using MudBlazor.Services;
using MercenariesAndBeasts.Infrastructure.Auth;
using SharedServices;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ScorerApp.Components;
using ScorerApp.Data;
using Serilog;
using Serilog.Exceptions;
using SharedServices.Services;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using MercenariesAndBeasts.Infrastructure.Localization;

Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Logs"));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30,
        shared: true, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ── Blazor ────────────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRazorPages();

// ── UI services ───────────────────────────────────────────────────────────────
builder.Services.AddSharedUI(builder.Configuration);
builder.Services.AddGlobalErrorNotifications();
builder.Services.AddSimpleLocalization();
builder.Services.AddMudServices();
builder.Services.AddApexCharts();

// ── Domain services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<ScorerApp.Domain.Services.ScoringRulesService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.StandingsService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.MatchGeneratorService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.EloService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.SeasonFormatService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.PlayoffService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.SportRatingService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.SeasonScheduleService>();

// ── Kluby (modul převzatý z ClubManageru) ─────────────────────────────────────
builder.Services.Configure<ScorerApp.Domain.Services.Clubs.SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<ScorerApp.Domain.Services.Clubs.NtfySettings>(builder.Configuration.GetSection("Ntfy"));
// Jen typed HttpClient — ClubManager službu registroval ještě jednou přes AddScoped, čímž přebil HttpClient z factory.
builder.Services.AddHttpClient<ScorerApp.Domain.Services.Clubs.ClubNotificationService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.Clubs.ClubAccessService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.Clubs.ClubService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.Clubs.InvitationService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.Clubs.ChatService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.Clubs.CircularService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.Clubs.CarReservationService>();
builder.Services.AddSingleton<ScorerApp.Domain.Services.Clubs.ClubChatBroadcaster>();
builder.Services.AddSingleton<ScorerApp.Domain.Services.Clubs.ChatNotificationDispatcher>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ScorerApp.Domain.Services.Clubs.ChatNotificationDispatcher>());

// ── Database ──────────────────────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
var dsb = new NpgsqlDataSourceBuilder(connStr);
dsb.EnableDynamicJson();
var dataSource = dsb.Build();

builder.Services.AddDbContextFactory<AppDbContext>(opt =>
    opt.UseNpgsql(dataSource));

// optionsLifetime Singleton: vedle AddDbContextFactory by jinak AddDbContext přepnul konfiguraci kontextu
// na scoped a singleton IDbContextFactory by na ni sahal z kořenového provideru. V Development (validace
// scope) pak factory nešla získat vůbec a padla každá stránka s DbFactory; v produkci jen skrytá chyba životnosti.
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(dataSource), ServiceLifetime.Scoped, ServiceLifetime.Singleton);

// ── Auth (Identity + optional Google OAuth) ───────────────────────────────────
builder.Services.AddMabAuth<AppDbContext>(builder.Configuration);
builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender,
    NoOpEmailSender>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

// ── App ───────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

var pathBase = builder.Configuration["PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
    app.UsePathBase(pathBase);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseStaticFiles();
app.MapStaticAssets();
app.UseRequestLocalization();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapHealthChecks("/health");
app.MapMabCultureEndpoint();
app.MapRazorPages();

// Results.Redirect() nerespektuje UsePathBase (na rozdíl od Cookie-auth challenge
// redirectů, které jsou pathbase-aware) — relativní "/" by tak vedlo mimo "/scorer".
static string WithBase(HttpContext http, string path) => $"{http.Request.PathBase}{path}";

// ── Google OAuth external login endpoints ─────────────────────────────────────
app.MapPost("/Identity/Account/ExternalLogin", async (
    HttpContext http,
    SignInManager<AppUser> signInManager) =>
{
    var provider  = http.Request.Form["provider"].ToString();
    var returnUrl = http.Request.Form["returnUrl"].ToString() ?? "/";
    var callback  = $"/Identity/Account/ExternalLogin/Callback?returnUrl={Uri.EscapeDataString(returnUrl)}";
    var props     = signInManager.ConfigureExternalAuthenticationProperties(provider, callback);
    return Results.Challenge(props, new[] { provider });
}).DisableAntiforgery();

app.MapGet("/Identity/Account/ExternalLogin/Callback", async (
    HttpContext http,
    string? returnUrl,
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    IWebHostEnvironment env,
    IConfiguration config) =>
{
    returnUrl ??= "/";
    var info = await signInManager.GetExternalLoginInfoAsync();
    if (info is null)
        return Results.Redirect(WithBase(http, "/login?error=external"));

    var signIn = await signInManager.ExternalLoginSignInAsync(
        info.LoginProvider, info.ProviderKey, isPersistent: true);

    if (signIn.Succeeded)
    {
        var signedInUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (signedInUser is not null)
        {
            var denied = await AccessGate.CheckAsync(signedInUser, signInManager, env, config);
            if (denied is not null) return Results.Redirect(WithBase(http, denied));
        }
        return Results.Redirect(WithBase(http, returnUrl));
    }

    var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
    if (string.IsNullOrWhiteSpace(email))
        return Results.Redirect(WithBase(http, "/login?error=noemail"));

    var user = new AppUser { UserName = email, Email = email };
    var created = await userManager.CreateAsync(user);
    if (created.Succeeded)
    {
        await userManager.AddLoginAsync(user, info);
        await signInManager.SignInAsync(user, isPersistent: true);
        var deniedNew = await AccessGate.CheckAsync(user, signInManager, env, config);
        if (deniedNew is not null) return Results.Redirect(WithBase(http, deniedNew));
        return Results.Redirect(WithBase(http, returnUrl));
    }

    var existing = await userManager.FindByEmailAsync(email);
    if (existing is not null)
    {
        await userManager.AddLoginAsync(existing, info);
        await signInManager.SignInAsync(existing, isPersistent: true);
        var deniedExisting = await AccessGate.CheckAsync(existing, signInManager, env, config);
        if (deniedExisting is not null) return Results.Redirect(WithBase(http, deniedExisting));
        return Results.Redirect(WithBase(http, returnUrl));
    }

    return Results.Redirect(WithBase(http, "/login?error=external"));
});

// ── Mobile app login handoff ───────────────────────────────────────────────────
// Google blokuje OAuth přihlášení v embedded WebView appky, proto se otevírá
// v externím prohlížeči (Chrome Custom Tabs). Po dokončení tam appka session
// nemá — tyto dva endpointy předají krátkodobým jednorázovým tokenem přihlášení
// zpět appce přes deep link (schéma "scorerapp://").
var mobileHandoffTokens = new ConcurrentDictionary<string, (string UserId, DateTime Expires)>();

app.MapGet("/mobile-handoff", (HttpContext http) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return Results.Redirect(WithBase(http, "/login"));

    var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    mobileHandoffTokens[token] = (userId, DateTime.UtcNow.AddMinutes(2));

    return Results.Redirect($"scorerapp://auth?token={token}");
}).RequireAuthorization();

app.MapGet("/mobile-token-login", async (
    HttpContext http,
    string? token,
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager) =>
{
    if (string.IsNullOrEmpty(token) ||
        !mobileHandoffTokens.TryRemove(token, out var entry) ||
        entry.Expires < DateTime.UtcNow)
    {
        return Results.Redirect(WithBase(http, "/login?error=external"));
    }

    var user = await userManager.FindByIdAsync(entry.UserId);
    if (user is null)
        return Results.Redirect(WithBase(http, "/login?error=external"));

    await signInManager.SignInAsync(user, isPersistent: true);
    return Results.Redirect(WithBase(http, "/"));
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(MercenariesAndBeasts.Infrastructure.Components.Account.Login).Assembly);

// ── Migrate + seed ────────────────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await db.Database.MigrateAsync();
    await ScorerApp.Data.SeedData.SeedSportsAsync(db);
    await EnsureAdminAsync(userManager, roleManager, app.Configuration);
}
catch (Exception ex) { Log.Warning(ex, "DB migration/seed skipped — DB not available"); }

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────────────
static async Task EnsureAdminAsync(UserManager<AppUser> um, RoleManager<IdentityRole> rm, IConfiguration config)
{
    // Vytvoř všechny 3 role
    foreach (var r in new[] { "Admin", "Moderator", "LoginUser" })
        if (!await rm.RoleExistsAsync(r)) await rm.CreateAsync(new IdentityRole(r));

    // Heslo jen z konfigurace (env Seed__AdminPassword) — v kódu veřejného repa by bylo čitelné komukoli.
    await EnsureUserAsync(um, "olsanskyvitek@gmail.com", "vitek", config["Seed:AdminPassword"]);
}

static async Task EnsureUserAsync(UserManager<AppUser> um, string email, string username, string? password)
{
    var user = await um.FindByEmailAsync(email);
    if (user is null)
    {
        // Existující účet heslo nepotřebuje; nový bez nakonfigurovaného hesla radši nezakládat, než ho založit se známým.
        if (string.IsNullOrWhiteSpace(password))
        {
            Log.Warning("Admin účet {Email} neexistuje a Seed:AdminPassword není nastavené — účet se nezakládá", email);
            return;
        }
        user = new AppUser
        {
            UserName           = username,
            Email              = email,
            EmailConfirmed     = true,
            IsAdmin            = true,
            IsWhitelisted      = true,
            MustChangePassword = true
        };
        var r = await um.CreateAsync(user, password);
        if (!r.Succeeded) return;
    }
    else if (!user.IsAdmin) { user.IsAdmin = true; await um.UpdateAsync(user); }

    // Admin dostane všechny 3 role (hierarchie)
    foreach (var role in new[] { "Admin", "Moderator", "LoginUser" })
        if (!await um.IsInRoleAsync(user, role))
            await um.AddToRoleAsync(user, role);
}

file sealed class NoOpEmailSender : Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage) => Task.CompletedTask;
}

public partial class Program { }
