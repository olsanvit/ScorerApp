using SharedServices.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScorerApp.Domain.Models;
using ScorerApp.Domain.Models.Clubs;
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
    public DbSet<PlayoffMatch> PlayoffMatches => Set<PlayoffMatch>();
    public DbSet<SportRating> SportRatings => Set<SportRating>();

    // ── Kluby (převzato z ClubManageru) ─────────────────────────────────────
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubMember> ClubMembers => Set<ClubMember>();
    public DbSet<FamilyLink> FamilyLinks => Set<FamilyLink>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<ClubThread> ClubThreads => Set<ClubThread>();
    public DbSet<ThreadParticipant> ThreadParticipants => Set<ThreadParticipant>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatMessageRead> ChatMessageReads => Set<ChatMessageRead>();
    public DbSet<Circular> Circulars => Set<Circular>();
    public DbSet<CircularRecipient> CircularRecipients => Set<CircularRecipient>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<CarReservation> CarReservations => Set<CarReservation>();

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

        builder.Entity<PlayoffMatch>(e =>
        {
            // Účastníci jsou nepovinní (pozice čeká na postupujícího) a mazání účastníka
            // nesmí shodit pavouk — proto SetNull místo kaskády.
            e.HasOne(p => p.ParticipantA).WithMany()
                .HasForeignKey(p => p.ParticipantAId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.ParticipantB).WithMany()
                .HasForeignKey(p => p.ParticipantBId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.Match).WithMany()
                .HasForeignKey(p => p.MatchId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(p => new { p.SeasonId, p.Round, p.BracketPosition });
        });

        builder.Entity<SportRating>(e =>
        {
            e.HasOne(r => r.Player).WithMany()
                .HasForeignKey(r => r.PlayerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Team).WithMany()
                .HasForeignKey(r => r.TeamId).OnDelete(DeleteBehavior.Cascade);
            // Jeden rating na dvojici sport + účastník; filtr na IsDeleted kvůli soft-delete,
            // aby smazaný záznam neblokoval založení nového.
            e.HasIndex(r => new { r.SportId, r.PlayerId })
                .IsUnique().HasFilter("\"IsDeleted\" = false AND \"PlayerId\" IS NOT NULL");
            e.HasIndex(r => new { r.SportId, r.TeamId })
                .IsUnique().HasFilter("\"IsDeleted\" = false AND \"TeamId\" IS NOT NULL");
        });

        ConfigureClubs(builder);

        builder.Entity<Season>().Property(s => s.FormatJson).HasColumnType("text");
        builder.Entity<Sport>().Property(s => s.ScoringRulesJson).HasColumnType("text");
        builder.Entity<League>().Property(l => l.ScoringRulesOverrideJson).HasColumnType("text");

        // Global soft-delete filter for all BaseGuid entities
        ApplySoftDeleteFilters(builder);
    }

    /// <summary>
    /// Klubový modul. Unikátní indexy mají filtr na IsDeleted — jinak by soft-delete záznam
    /// navždy blokoval založení stejného (např. znovupřidání hráče do oddílu).
    /// Vazby na účet (AppUser) jsou Restrict: smazání účtu nesmí tiše smazat historii zpráv a rezervací.
    /// </summary>
    private static void ConfigureClubs(ModelBuilder builder)
    {
        const string notDeleted = "\"IsDeleted\" = false";

        builder.Entity<Team>(e =>
            e.HasOne(t => t.Club).WithMany(c => c.Teams)
                .HasForeignKey(t => t.ClubId).OnDelete(DeleteBehavior.SetNull));

        builder.Entity<SeasonParticipant>(e =>
            e.HasOne(p => p.Club).WithMany()
                .HasForeignKey(p => p.ClubId).OnDelete(DeleteBehavior.SetNull));

        builder.Entity<Organization>(e => e.HasIndex(o => o.Name));

        builder.Entity<OrganizationMember>(e =>
        {
            e.HasOne(m => m.Organization).WithMany(o => o.Members)
                .HasForeignKey(m => m.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.User).WithMany()
                .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(m => new { m.OrganizationId, m.UserId }).IsUnique().HasFilter(notDeleted);
        });

        builder.Entity<Club>(e =>
        {
            e.HasOne(c => c.Organization).WithMany(o => o.Clubs)
                .HasForeignKey(c => c.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(c => new { c.OrganizationId, c.Name }).IsUnique().HasFilter(notDeleted);
            e.HasIndex(c => c.JoinCode).IsUnique().HasFilter(notDeleted);
        });

        builder.Entity<ClubMember>(e =>
        {
            e.HasOne(m => m.Club).WithMany(c => c.Members)
                .HasForeignKey(m => m.ClubId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Player).WithMany()
                .HasForeignKey(m => m.PlayerId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(m => new { m.ClubId, m.PlayerId }).IsUnique().HasFilter(notDeleted);
        });

        builder.Entity<FamilyLink>(e =>
        {
            e.HasOne(f => f.Organization).WithMany()
                .HasForeignKey(f => f.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.ParentUser).WithMany()
                .HasForeignKey(f => f.ParentUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.ChildUser).WithMany()
                .HasForeignKey(f => f.ChildUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(f => new { f.OrganizationId, f.ParentUserId, f.ChildUserId }).IsUnique().HasFilter(notDeleted);
        });

        builder.Entity<Invitation>(e =>
        {
            e.HasOne(i => i.Club).WithMany(c => c.Invitations)
                .HasForeignKey(i => i.ClubId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(i => i.Token).IsUnique();
        });

        builder.Entity<ClubThread>(e =>
        {
            e.HasOne(t => t.Club).WithMany(c => c.Threads)
                .HasForeignKey(t => t.ClubId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.CreatedByUser).WithMany()
                .HasForeignKey(t => t.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ThreadParticipant>(e =>
        {
            e.HasOne(p => p.Thread).WithMany(t => t.Participants)
                .HasForeignKey(p => p.ThreadId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.User).WithMany()
                .HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(p => new { p.ThreadId, p.UserId }).IsUnique().HasFilter(notDeleted);
        });

        builder.Entity<ChatMessage>(e =>
        {
            e.HasOne(m => m.Thread).WithMany(t => t.Messages)
                .HasForeignKey(m => m.ThreadId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.SenderUser).WithMany()
                .HasForeignKey(m => m.SenderUserId).OnDelete(DeleteBehavior.Restrict);
            e.Property(m => m.Body).HasColumnType("text");
            e.HasIndex(m => new { m.ThreadId, m.CreatedAt });
        });

        builder.Entity<ChatMessageRead>(e =>
        {
            e.HasOne(r => r.Message).WithMany(m => m.Reads)
                .HasForeignKey(r => r.MessageId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => new { r.MessageId, r.UserId }).IsUnique();
        });

        builder.Entity<Circular>(e =>
        {
            e.HasOne(c => c.Organization).WithMany()
                .HasForeignKey(c => c.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Club).WithMany()
                .HasForeignKey(c => c.ClubId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.SenderUser).WithMany()
                .HasForeignKey(c => c.SenderUserId).OnDelete(DeleteBehavior.Restrict);
            e.Property(c => c.Body).HasColumnType("text");
        });

        builder.Entity<CircularRecipient>(e =>
        {
            e.HasOne(r => r.Circular).WithMany(c => c.Recipients)
                .HasForeignKey(r => r.CircularId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.User).WithMany()
                .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => new { r.CircularId, r.UserId }).IsUnique();
        });

        builder.Entity<NotificationPreference>(e =>
        {
            e.HasOne(p => p.Club).WithMany()
                .HasForeignKey(p => p.ClubId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => new { p.UserId, p.ClubId }).IsUnique().HasFilter(notDeleted);
        });

        builder.Entity<Car>(e =>
            e.HasOne(c => c.Organization).WithMany(o => o.Cars)
                .HasForeignKey(c => c.OrganizationId).OnDelete(DeleteBehavior.Cascade));

        builder.Entity<CarReservation>(e =>
        {
            e.HasOne(r => r.Car).WithMany(c => c.Reservations)
                .HasForeignKey(r => r.CarId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.User).WithMany()
                .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            e.Property(r => r.KmAtStart).HasPrecision(10, 2);
            e.Property(r => r.KmAtEnd).HasPrecision(10, 2);
            e.HasIndex(r => new { r.CarId, r.DateFrom, r.DateTo });
        });
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
