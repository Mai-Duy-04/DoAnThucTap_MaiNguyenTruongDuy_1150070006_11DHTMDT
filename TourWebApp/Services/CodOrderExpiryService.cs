using Microsoft.EntityFrameworkCore;
using TourWebApp.Data.Models;
using TourWebApp.Models;

namespace TourWebApp.Services;

public class CodOrderExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CodOrderExpiryService> _logger;

    public CodOrderExpiryService(IServiceScopeFactory scopeFactory, ILogger<CodOrderExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var initialScope = _scopeFactory.CreateScope();
            var inventory = initialScope.ServiceProvider.GetRequiredService<ISeatInventoryService>();
            await inventory.SynchronizeAllAsync(stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Initial seat inventory synchronization failed.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await XuLyDonQuaHan(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Booking expiry job failed.");
            }
        }
    }

    private async Task XuLyDonQuaHan(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seatInventory = scope.ServiceProvider.GetRequiredService<ISeatInventoryService>();
        var now = DateTime.Now;

        var donQuaHan = await db.DonDatTours
            .Include(x => x.IdLichNavigation)
            .Include(x => x.IdTourNavigation)
            .Include(x => x.PhieuGiamGiaSuDungs)
            .Where(x => !x.DaThanhToan
                && x.TrangThai != BookingPaymentStatus.TrangThaiDaHuy
                && x.HanThanhToan.HasValue
                && x.HanThanhToan.Value < now)
            .ToListAsync(stoppingToken);

        if (donQuaHan.Count == 0) return;

        foreach (var don in donQuaHan)
        {
            don.TrangThai = BookingPaymentStatus.TrangThaiDaHuy;
            don.TrangThaiThanhToan = BookingPaymentStatus.TrangThaiTtHetHanThanhToan;

            var suDung = don.PhieuGiamGiaSuDungs.OrderByDescending(x => x.IdSuDung).FirstOrDefault();
            if (suDung != null && suDung.TrangThai == "GiuCho")
            {
                suDung.TrangThai = "DaHuy";
                suDung.ThoiDiemSuDung = now;
                suDung.GhiChu = "Don COD het han tu dong";
            }
        }

        var affectedSchedules = donQuaHan
            .Select(x => new { x.IdLich, x.IdTour })
            .Distinct()
            .ToList();

        await db.SaveChangesAsync(stoppingToken);
        foreach (var item in affectedSchedules)
        {
            await seatInventory.SynchronizeAsync(item.IdLich, item.IdTour, stoppingToken);
        }
        _logger.LogInformation("Booking expiry job canceled {Count} expired orders.", donQuaHan.Count);
    }
}
