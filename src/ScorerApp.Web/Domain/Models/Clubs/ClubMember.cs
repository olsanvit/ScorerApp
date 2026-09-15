using SharedServices.Models.Base;

namespace ScorerApp.Domain.Models.Clubs;

/// <summary>
/// Hráč na soupisce oddílu. Vazba je na <see cref="Player"/>, ne na účet — děti a hráči
/// přidaní na místě účet nemají. Chat a oběžníky dostane jen hráč spárovaný přes Player.UserId.
/// </summary>
public class ClubMember : BaseGuid
{
    public Guid ClubId { get; set; }
    public Club Club { get; set; } = null!;

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public string? Position { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
