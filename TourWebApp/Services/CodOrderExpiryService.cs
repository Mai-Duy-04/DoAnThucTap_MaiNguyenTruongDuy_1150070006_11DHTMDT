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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await XuLyDonCodQuaHan(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "COD expiry job failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task XuLyDonCodQuaHan(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.Now;

        var donQuaHan = await db.DonDatTours
            .Include(x => x.IdLichNavigation)
            .Include(x => x.IdTourNavigation)
            .Include(x => x.PhieuGiamGiaSuDungs)
            .Where(x => !x.DaThanhToan
                && x.PhuongThucTT == BookingPaymentStatus.PhuongThucTienMat
                && x.TrangThai == BookingPaymentStatus.TrangThaiChoXacNhanTienMat
                && x.TrangThaiThanhToan == BookingPaymentStatus.TrangThaiTtChoThuTienMat
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

        await db.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("COD expiry job canceled {Count} orders.", donQuaHan.Count);
    }
}