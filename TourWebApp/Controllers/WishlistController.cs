using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourWebApp.Data.Models;

namespace TourWebApp.Controllers;

public class WishlistController : Controller
{
    private readonly ApplicationDbContext _context;

    public WishlistController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var userId = HttpContext.Session.GetInt32("IdTaiKhoan");
        if (userId == null)
        {
            return RedirectToAction("DangNhap", "TaiKhoan", new { returnUrl = "/Wishlist" });
        }

        var wishlist = _context.WishlistTours
            .Include(x => x.IdTourNavigation)
            .Where(x => x.IdTaiKhoan == userId.Value)
            .OrderByDescending(x => x.NgayTao)
            .ToList();

        return View(wishlist);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Toggle(int idTour, string? returnUrl)
    {
        var userId = HttpContext.Session.GetInt32("IdTaiKhoan");
        if (userId == null)
        {
            return RedirectToAction("DangNhap", "TaiKhoan", new { returnUrl = returnUrl ?? $"/Tour/ChiTiet/{idTour}" });
        }

        var existing = _context.WishlistTours
            .FirstOrDefault(x => x.IdTaiKhoan == userId.Value && x.IdTour == idTour);

        if (existing == null)
        {
            _context.WishlistTours.Add(new WishlistTour
            {
                IdTaiKhoan = userId.Value,
                IdTour = idTour,
                NgayTao = DateTime.Now
            });
        }
        else
        {
            _context.WishlistTours.Remove(existing);
        }

        _context.SaveChanges();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleByDestination(string destinationSlug, string? returnUrl)
    {
        var userId = HttpContext.Session.GetInt32("IdTaiKhoan");
        if (userId == null)
        {
            return RedirectToAction("DangNhap", "TaiKhoan", new { returnUrl = returnUrl ?? "/Travel" });
        }

        var keyword = destinationSlug?.Trim().ToLowerInvariant() switch
        {
            "phu-quoc" => "phú quốc",
            "vung-tau" => "vũng tàu",
            "da-lat" => "đà lạt",
            "can-tho" => "cần thơ",
            "sai-gon" => "hồ chí minh",
            "mien-tay" => "miền tây",
            _ => null
        };

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return RedirectToLocal(returnUrl);
        }

        var matchedTour = _context.Tours
            .FirstOrDefault(t =>
                (t.DiaDiem != null && EF.Functions.Collate(t.DiaDiem, "SQL_Latin1_General_CP1_CI_AI").Contains(keyword)) ||
                (t.TenTour != null && EF.Functions.Collate(t.TenTour, "SQL_Latin1_General_CP1_CI_AI").Contains(keyword)));

        if (matchedTour == null)
        {
            return RedirectToLocal(returnUrl);
        }

        var existing = _context.WishlistTours
            .FirstOrDefault(x => x.IdTaiKhoan == userId.Value && x.IdTour == matchedTour.IdTour);

        if (existing == null)
        {
            _context.WishlistTours.Add(new WishlistTour
            {
                IdTaiKhoan = userId.Value,
                IdTour = matchedTour.IdTour,
                NgayTao = DateTime.Now
            });
        }
        else
        {
            _context.WishlistTours.Remove(existing);
        }

        _context.SaveChanges();
        return RedirectToLocal(returnUrl);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int idWishlist)
    {
        var userId = HttpContext.Session.GetInt32("IdTaiKhoan");
        if (userId == null)
        {
            return RedirectToAction("DangNhap", "TaiKhoan", new { returnUrl = "/Wishlist" });
        }

        var item = _context.WishlistTours
            .FirstOrDefault(x => x.IdWishlist == idWishlist && x.IdTaiKhoan == userId.Value);

        if (item != null)
        {
            _context.WishlistTours.Remove(item);
            _context.SaveChanges();
        }

        return RedirectToAction(nameof(Index));
    }
}