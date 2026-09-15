using MercenariesAndBeasts.Infrastructure;
using SharedServices.Models.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScorerApp.Domain.Models.Clubs;

/// <summary>
/// Rezervace auta na celé dny. DateOnly a oba krajní dny včetně — auto půjčené do pátku
/// nesmí jít rezervovat od pátku, což původní porovnání přes DateTime o půlnoci pouštělo.
/// </summary>
public class CarReservation : BaseGuid
{
    public Guid CarId { get; set; }
    public Car Car { get; set; } = null!;

    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;

    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }

    public string? Purpose { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    public decimal? KmAtStart { get; set; }
    public decimal? KmAtEnd { get; set; }
    public string? Note { get; set; }

    [NotMapped] public decimal? KmDriven => KmAtEnd - KmAtStart;
    [NotMapped] public int Days => DateTo.DayNumber - DateFrom.DayNumber + 1;

    /// <summary>Stavy, které auto skutečně blokují — zamítnutá a zrušená rezervace termín uvolní.</summary>
    [NotMapped] public bool BlocksCar => Status is not (ReservationStatus.Rejected or ReservationStatus.Cancelled);

    public static bool Overlaps(DateOnly aFrom, DateOnly aTo, DateOnly bFrom, DateOnly bTo) =>
        aFrom <= bTo && aTo >= bFrom;
}
