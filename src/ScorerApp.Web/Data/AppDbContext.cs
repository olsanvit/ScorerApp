using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScorerApp.Domain.Models;

namespace ScorerApp.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamPlayer> TeamPlayers => Set<TeamPlayer>();
    public DbSet<SeasonParticipant> SeasonParticipants => Set<SeasonParticipant>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();
    public DbSet<MatchSet> MatchSets => Set<MatchSet>();
    public DbSet<Race> Races => Set<Race>();
    public DbSet<RaceResult> RaceResults => Set<RaceResult>();

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
    }
}
