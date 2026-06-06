using SharedServices.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScorerApp.Domain.Models;
using SharedServices.Models.Base;
using System.Security.Claims;

namespace ScorerApp.Data;

/// <summary>
/// Main EF Core database context for ScorerApp.
/// Inherits ASP.NET Identity tables and owns all domain entities.
/// Automatically stamps audit fields (CreatedBy, UpdatedBy, IsDeleted) on save.
/// Global query filters exclude soft-deleted records from all queries.
/// </summary>
public class AppDbContext : IdentityDbContext<AppUser>
{
    private readonly IHttpContextAccessor? _httpCtx;

    /// <param name="options">EF Core options (injected by DI).</param>
    /// <param name="httpCtx">Optional HTTP context accessor for resolving current user ID.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options,
                        IHttpContextAccessor? httpCtx = null)
        : base(options) => _httpCtx = httpCtx;

    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<ScorerApp.Domain.Models.TeamPlayer> TeamPlayers => Set<ScorerApp.Domain.Models.TeamPlayer>();
    public DbSet<SeasonParticipant> SeasonParticipants => Set<SeasonParticipant>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();
    public DbSet<MatchSet> MatchSets => Set<MatchSet>();
    public DbSet<Race> Races => Set<Race>();
    public DbSet<RaceResult> RaceResults => Set<RaceResult>();

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var userId = _httpCtx?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        AuditInterceptor.ApplyAudit(this, userId);
        return base.SaveChangesAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SeasonParticipant>(e =>
        {
            e.HasOne(p => p.Player).WithMany(p => p.SeasonParticipants)
                .HasForeignKey(p => p.PlayerId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.Team).WithMany(t => t.SeasonParticipants)
                .HasForeignKey(p => p.TeamId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Match>(e =>
        {
            e.HasOne(m => m.HomeParticipant).WithMany()
                .HasForeignKey(m => m.HomeParticipantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.AwayParticipant).WithMany()
                .HasForeignKey(m => m.AwayParticipantId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Sport>().Property(s => s.ScoringRulesJson).HasColumnType("text");
        builder.Entity<League>().Property(l => l.ScoringRulesOverrideJson).HasColumnType("text");

        // Global soft-delete filter for all BaseGuid entities
        ApplySoftDeleteFilters(builder);
    }

    /// <summary>
    /// Applies a global query filter to all entities that inherit from <see cref="BaseGuid"/>,
    /// excluding soft-deleted records (<c>IsDeleted = true</c>) from every query automatically.
    /// </summary>
    private static void ApplySoftDeleteFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes()
                     .Where(t => typeof(BaseGuid).IsAssignableFrom(t.ClrType)))
        {
            var param = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var prop  = System.Linq.Expressions.Expression.Property(param, nameof(BaseGuid.IsDeleted));
            var notDeleted = System.Linq.Expressions.Expression.Not(prop);
            var lambda = System.Linq.Expressions.Expression.Lambda(notDeleted, param);
            builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
