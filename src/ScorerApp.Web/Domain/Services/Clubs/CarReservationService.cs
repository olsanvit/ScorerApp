using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Npgsql;
using ScorerApp.Data;
using ScorerApp.Domain.Models.Clubs;

namespace ScorerApp.Domain.Services.Clubs;

public class CarReservationService(
    IDbContextFactory<AppDbContext> dbFactory,
    ClubAccessService access,
    IStringLocalizer<SharedResource> S)
{
    public async Task<List<Car>> GetCarsAsync(Guid organizationId, string userId, bool isSiteAdmin, bool includeInactive = false)
    {
        // SPZ a vozový park nejsou veřejné — jen pro členy organizace.
        if (!await access.IsOrganizationMemberAsync(organizationId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException(S["ClubErr_CarsMembersOnly"]);

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Cars
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId && (includeInactive || c.IsActive))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Car> SaveCarAsync(Car car, string userId, bool isSiteAdmin)
    {
        if (string.IsNullOrWhiteSpace(car.Name))
            throw new ArgumentException(S["ClubErr_CarNameRequired"]);
        if (!await access.CanManageOrganizationAsync(car.OrganizationId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException(S["ClubErr_CarsManageOrgAdminOnly"]);

        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.Cars.FirstOrDefaultAsync(c => c.Guid == car.Guid);
        if (existing is null)
        {
            car.Name = car.Name.Trim();
            db.Cars.Add(car);
            await db.SaveChangesAsync();
            return car;
        }

        // Ochrana proti podvržení: auto nejde přesunout do organizace, kterou uživatel spravuje.
        if (existing.OrganizationId != car.OrganizationId)
            throw new UnauthorizedAccessException(S["ClubErr_CarOtherOrganization"]);

        existing.Name         = car.Name.Trim();
        existing.LicensePlate = car.LicensePlate;
        existing.Brand        = car.Brand;
        existing.Model        = car.Model;
        existing.Year         = car.Year;
        existing.IsActive     = car.IsActive;
        await db.SaveChangesAsync();
        return existing;
    }

    public async Task<List<CarReservation>> GetReservationsAsync(
        Guid organizationId, string userId, bool isSiteAdmin, DateOnly? from = null, DateOnly? to = null)
    {
        if (!await access.IsOrganizationMemberAsync(organizationId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException(S["ClubErr_ReservationsMembersOnly"]);

        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.CarReservations
            .AsNoTracking()
            .Include(r => r.Car)
            .Include(r => r.User)
            .Where(r => r.Car.OrganizationId == organizationId);
        if (from.HasValue) q = q.Where(r => r.DateTo >= from.Value);
        if (to.HasValue) q = q.Where(r => r.DateFrom <= to.Value);
        return await q.OrderBy(r => r.DateFrom).ToListAsync();
    }

    public async Task<CarReservation> CreateReservationAsync(
        Guid carId, string userId, bool isSiteAdmin, DateOnly from, DateOnly to, string? purpose, decimal? kmAtStart)
    {
        if (to < from)
            throw new ArgumentException(S["ClubErr_ReservationEndBeforeStart"]);

        await using var db = await dbFactory.CreateDbContextAsync();
        var car = await db.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Guid == carId && c.IsActive)
            ?? throw new InvalidOperationException(S["ClubErr_CarNotFound"]);
        if (!await access.IsOrganizationMemberAsync(car.OrganizationId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException(S["ClubErr_ReserveMembersOnly"]);

        return await InSerializableAsync(db, async () =>
        {
            await EnsureFreeAsync(db, carId, from, to, exceptId: null);
            var reservation = new CarReservation
            {
                CarId     = carId,
                UserId    = userId,
                DateFrom  = from,
                DateTo    = to,
                Purpose   = string.IsNullOrWhiteSpace(purpose) ? null : purpose.Trim(),
                KmAtStart = kmAtStart
            };
            db.CarReservations.Add(reservation);
            await db.SaveChangesAsync();
            return reservation;
        });
    }

    public async Task UpdateStatusAsync(Guid reservationId, ReservationStatus newStatus, string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var reservation = await db.CarReservations.Include(r => r.Car).FirstOrDefaultAsync(r => r.Guid == reservationId)
            ?? throw new InvalidOperationException(S["ClubErr_ReservationNotFound"]);

        var isManager = await access.CanManageOrganizationAsync(reservation.Car.OrganizationId, userId, isSiteAdmin);
        var isOwner = reservation.UserId == userId;
        var allowed = newStatus switch
        {
            ReservationStatus.Approved or ReservationStatus.Rejected or ReservationStatus.Pending => isManager,
            ReservationStatus.Cancelled or ReservationStatus.Completed                          => isManager || isOwner,
            _                                                                                   => false
        };
        if (!allowed)
            throw new UnauthorizedAccessException(S["ClubErr_ReservationStatusForbidden"]);

        var wasBlocking = reservation.BlocksCar;
        reservation.Status = newStatus;

        // Obnovená zamítnutá/zrušená rezervace znovu blokuje auto — termín mezitím mohl zabrat někdo jiný.
        if (!wasBlocking && reservation.BlocksCar)
        {
            await InSerializableAsync(db, async () =>
            {
                await EnsureFreeAsync(db, reservation.CarId, reservation.DateFrom, reservation.DateTo, reservation.Guid);
                await db.SaveChangesAsync();
                return true;
            });
            return;
        }

        await db.SaveChangesAsync();
    }

    public async Task CompleteAsync(Guid reservationId, decimal kmAtEnd, string userId, bool isSiteAdmin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var reservation = await db.CarReservations.Include(r => r.Car).FirstOrDefaultAsync(r => r.Guid == reservationId)
            ?? throw new InvalidOperationException(S["ClubErr_ReservationNotFound"]);

        if (reservation.UserId != userId
            && !await access.CanManageOrganizationAsync(reservation.Car.OrganizationId, userId, isSiteAdmin))
            throw new UnauthorizedAccessException(S["ClubErr_TripCompleteForbidden"]);
        if (reservation.KmAtStart.HasValue && kmAtEnd < reservation.KmAtStart.Value)
            throw new ArgumentException(S["ClubErr_OdometerEndLower"]);

        reservation.KmAtEnd = kmAtEnd;
        reservation.Status = ReservationStatus.Completed;
        await db.SaveChangesAsync();
    }

    // Instanční, ne static — hláška potřebuje lokalizátor S z konstruktoru.
    private async Task EnsureFreeAsync(AppDbContext db, Guid carId, DateOnly from, DateOnly to, Guid? exceptId)
    {
        var conflict = await db.CarReservations.AnyAsync(r =>
            r.CarId == carId
            && r.Status != ReservationStatus.Rejected && r.Status != ReservationStatus.Cancelled
            && r.DateFrom <= to && r.DateTo >= from
            && (exceptId == null || r.Guid != exceptId));
        if (conflict)
            throw new InvalidOperationException(S["ClubErr_CarAlreadyReserved"]);
    }

    /// <summary>
    /// Kontrola volného termínu a zápis v jedné Serializable transakci. Bez ní by dvě souběžné
    /// rezervace obě prošly kontrolou (race z auditu ClubManageru). Druhou Postgres odmítne
    /// chybou 40001, kterou hlásíme jako obsazený termín.
    /// </summary>
    // Instanční, ne static — hláška potřebuje lokalizátor S z konstruktoru.
    private async Task<T> InSerializableAsync<T>(AppDbContext db, Func<Task<T>> work)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var result = await work();
            await tx.CommitAsync();
            return result;
        }
        catch (Exception ex) when (IsSerializationFailure(ex))
        {
            throw new InvalidOperationException(S["ClubErr_ReservationConflictRetry"]);
        }
    }

    private static bool IsSerializationFailure(Exception ex) =>
        ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure }
        || ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure };
}
