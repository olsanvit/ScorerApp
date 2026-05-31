using ApexCharts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ScorerApp.Components;
using ScorerApp.Data;
using Serilog;
using Serilog.Exceptions;
using SharedServices.Services;

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
builder.Services.AddScoped<SharedServices.ToastService>();
builder.Services.AddSingleton<ThemeService>(_ => new ThemeService(builder.Configuration));
builder.Services.AddApexCharts();

// ── Domain services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<ScorerApp.Domain.Services.ScoringRulesService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.StandingsService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.MatchGeneratorService>();
builder.Services.AddScoped<ScorerApp.Domain.Services.EloService>();

// ── Database ──────────────────────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
var dsb = new NpgsqlDataSourceBuilder(connStr);
dsb.EnableDynamicJson();
var dataSource = dsb.Build();

builder.Services.AddDbContextFactory<AppDbContext>(opt =>
    opt.UseNpgsql(dataSource));

// Identity needs scoped DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(dataSource));

// ── Identity ──────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<AppUser, IdentityRole>(opt =>
    {
        opt.Password.RequireDigit = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender,
    NoOpEmailSender>();

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/login";
    opt.LogoutPath = "/logout";
    opt.AccessDeniedPath = "/access-denied";
});

builder.Services.AddHttpContextAccessor();

// ── App ───────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseStaticFiles();
app.MapStaticAssets();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ── Migrate + seed ────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await db.Database.MigrateAsync();
    await ScorerApp.Data.SeedData.SeedSportsAsync(db);
    await SeedAdminAsync(userManager, roleManager);
}

app.Run();

// ── Helpers ───────────────────────────────────────────────────────────────────
static async Task SeedAdminAsync(UserManager<AppUser> um, RoleManager<IdentityRole> rm)
{
    const string role = "Admin";
    if (!await rm.RoleExistsAsync(role))
        await rm.CreateAsync(new IdentityRole(role));

    var user = await um.FindByEmailAsync("admin@local");
    if (user is null)
    {
        user = new AppUser { UserName = "admin", Email = "admin@local", EmailConfirmed = true, IsAdmin = true };
        var r = await um.CreateAsync(user, "Admin123.");
        if (!r.Succeeded) throw new Exception(string.Join(", ", r.Errors.Select(e => e.Description)));
    }
    if (!await um.IsInRoleAsync(user, role))
        await um.AddToRoleAsync(user, role);
}

file sealed class NoOpEmailSender : Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage) => Task.CompletedTask;
}

public partial class Program { }
