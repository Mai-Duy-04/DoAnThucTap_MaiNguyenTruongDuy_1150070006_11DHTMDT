using Microsoft.EntityFrameworkCore;
using TourWebApp.Data.Models;
using TourWebApp.Models;

namespace TourWebApp.Services;

public interface ISeatInventoryService
{
    int CountGuests(int adults, int children, int infants);
    Task<int> GetRemainingSeatsAsync(int scheduleId, CancellationToken cancellationToken = default);
    Task SynchronizeAsync(int scheduleId, int tourId, CancellationToken cancellationToken = default);
    Task SynchronizeAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Keeps schedule capacity derived from active bookings. Recalculation is deliberately
/// idempotent so legacy database triggers cannot make seats drift through double updates.
/// </summary>
public sealed class SeatInventoryService : ISeatInventoryService
{
    private readonly ApplicationDbContext _db;

    public SeatInventoryService(ApplicationDbContext db) => _db = db;

    public int CountGuests(int adults, int children, int infants)
        => Math.Max(0, adults) + Math.Max(0, children) + Math.Max(0, infants);

    public async Task<int> GetRemainingSeatsAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var capacity = await _db.LichKhoiHanhs
            .Where(x => x.IdLich == scheduleId)
            .Select(x => x.SoChoToiDa)
            .SingleOrDefaultAsync(cancellationToken);

        var reserved = await ActiveBookings(scheduleId)
            .SumAsync(x => x.NguoiLon + x.TreEm + x.TreNho, cancellationToken);

        return Math.Max(0, capacity - reserved);
    }

    public async Task SynchronizeAsync(int scheduleId, int tourId, CancellationToken cancellationToken = default)
    {
        var schedule = await _db.LichKhoiHanhs.SingleAsync(x => x.IdLich == scheduleId, cancellationToken);
        var previousRemainingSeats = schedule.SoChoConLai;
        var reservedForSchedule = await ActiveBookings(scheduleId)
            .SumAsync(x => x.NguoiLon + x.TreEm + x.TreNho, cancellationToken);

        schedule.SoChoConLai = Math.Max(0, schedule.SoChoToiDa - reservedForSchedule);
        schedule.TrangThai = schedule.SoChoConLai switch
        {
            <= 0 => "Hết chỗ",
            <= 5 => "Sắp hết chỗ",
            _ => "Còn chỗ"
        };

        var tour = await _db.Tours.SingleAsync(x => x.IdTour == tourId, cancellationToken);
        tour.SoNguoiDaDat = await _db.DonDatTours
            .Where(x => x.IdTour == tourId && x.TrangThai != BookingPaymentStatus.TrangThaiDaHuy)
            .SumAsync(x => x.NguoiLon + x.TreEm + x.TreNho, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        if (previousRemainingSeats <= 0 && schedule.SoChoConLai > 0)
        {
            var tourLink = $"/Tour/ChiTiet/{tourId}";
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
IF OBJECT_ID(N'dbo.DanhSachCho', N'U') IS NOT NULL
BEGIN
    DECLARE @IdDanhSachCho INT;
    DECLARE @IdTaiKhoan INT;
    SELECT TOP (1) @IdDanhSachCho = IdDanhSachCho, @IdTaiKhoan = IdTaiKhoan
    FROM dbo.DanhSachCho
    WHERE IdLich = {scheduleId} AND TrangThai = N'Đang chờ' AND SoKhach <= {schedule.SoChoConLai}
    ORDER BY NgayDangKy, IdDanhSachCho;

    IF @IdDanhSachCho IS NOT NULL
    BEGIN
        UPDATE dbo.DanhSachCho SET TrangThai = N'Đã thông báo' WHERE IdDanhSachCho = @IdDanhSachCho;
        INSERT INTO dbo.ThongBao (IdDon, NoiDung, NgayTao, DaDoc, IdNguoiNhan, TieuDe, LienKet)
        VALUES (NULL, N'Lịch tour bạn chờ vừa có chỗ trống. Hãy đặt sớm trước khi hết chỗ.', GETDATE(), 0,
                @IdTaiKhoan, N'Tour đã có chỗ trở lại', {tourLink});
    END
END", cancellationToken);
        }
    }

    public async Task SynchronizeAllAsync(CancellationToken cancellationToken = default)
    {
        var reservedBySchedule = await _db.DonDatTours
            .Where(x => x.TrangThai != BookingPaymentStatus.TrangThaiDaHuy)
            .GroupBy(x => x.IdLich)
            .Select(x => new
            {
                IdLich = x.Key,
                Reserved = x.Sum(d => d.NguoiLon + d.TreEm + d.TreNho)
            })
            .ToDictionaryAsync(x => x.IdLich, x => x.Reserved, cancellationToken);

        var reservedByTour = await _db.DonDatTours
            .Where(x => x.TrangThai != BookingPaymentStatus.TrangThaiDaHuy)
            .GroupBy(x => x.IdTour)
            .Select(x => new
            {
                IdTour = x.Key,
                Reserved = x.Sum(d => d.NguoiLon + d.TreEm + d.TreNho)
            })
            .ToDictionaryAsync(x => x.IdTour, x => x.Reserved, cancellationToken);

        var schedules = await _db.LichKhoiHanhs
            .ToListAsync(cancellationToken);

        foreach (var schedule in schedules)
        {
            reservedBySchedule.TryGetValue(schedule.IdLich, out var reserved);
            schedule.SoChoConLai = Math.Max(0, schedule.SoChoToiDa - reserved);
            schedule.TrangThai = schedule.SoChoConLai switch
            {
                <= 0 => "Hết chỗ",
                <= 5 => "Sắp hết chỗ",
                _ => "Còn chỗ"
            };
        }

        var tours = await _db.Tours.ToListAsync(cancellationToken);
        foreach (var tour in tours)
        {
            reservedByTour.TryGetValue(tour.IdTour, out var reserved);
            tour.SoNguoiDaDat = reserved;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<DonDatTour> ActiveBookings(int scheduleId)
        => _db.DonDatTours.Where(x =>
            x.IdLich == scheduleId &&
            x.TrangThai != BookingPaymentStatus.TrangThaiDaHuy);
}
