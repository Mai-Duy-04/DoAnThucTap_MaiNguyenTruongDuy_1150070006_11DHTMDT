using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TourWebApp.Data.Models;

namespace TourWebApp.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataProtector _checkinProtector;

        public TaiKhoanController(ApplicationDbContext context, IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _checkinProtector = dataProtectionProvider.CreateProtector("HappyTrip.CheckInQrToken.v1");
        }

        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DangKy(string HoTen, string Email, string MatKhau, string MatKhauNhapLai)
        {
            if (MatKhau != MatKhauNhapLai)
            {
                ViewBag.ThongBao = "Mat khau khong khop";
                return View();
            }

            if (_context.TaiKhoans.Any(x => x.Email == Email))
            {
                ViewBag.ThongBao = "Email da ton tai";
                return View();
            }

            string vaiTro = "User";
            if (Email.EndsWith("@happydulich.vn", StringComparison.OrdinalIgnoreCase))
            {
                vaiTro = "Admin";
            }
            else if (!Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.ThongBao = "Email phai la gmail hoac happydulich.vn";
                return View();
            }

            var tk = new TaiKhoan
            {
                HoTen = HoTen,
                Email = Email,
                MatKhau = MatKhau,
                VaiTro = vaiTro,
                TrangThai = true,
                NgayTao = DateTime.Now
            };

            _context.TaiKhoans.Add(tk);
            _context.SaveChanges();

            ViewBag.ThongBao = "Dang ky thanh cong, hay dang nhap";
            return View();
        }

        [HttpGet]
        public IActionResult DangNhap(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.ThongBao = TempData["ThongBao"];
            return View();
        }

        [HttpPost]
        public IActionResult DangNhap(string Email, string MatKhau, string? returnUrl)
        {
            var user = _context.TaiKhoans
                .FirstOrDefault(x => x.Email == Email && x.MatKhau == MatKhau && x.TrangThai == true);

            if (user == null)
            {
                ViewBag.ThongBao = "Sai email hoac mat khau";
                return View();
            }

            HttpContext.Session.SetInt32("IdTaiKhoan", user.IdTaiKhoan);
            HttpContext.Session.SetString("HoTen", user.HoTen);
            HttpContext.Session.SetString("VaiTro", user.VaiTro);

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (user.VaiTro == "Admin")
            {
                return RedirectToAction("Dashboard", "QuanTri");
            }

            if (TempData["ReturnUrl"] is string tempReturnUrl && !string.IsNullOrWhiteSpace(tempReturnUrl))
            {
                return Redirect(tempReturnUrl);
            }

            return RedirectToAction("Index", "Travel");
        }

        [HttpGet]
        public IActionResult QuenMatKhau()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuenMatKhau(string Email, string MatKhauMoi, string NhapLaiMatKhauMoi)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(MatKhauMoi) || string.IsNullOrWhiteSpace(NhapLaiMatKhauMoi))
            {
                ViewBag.ThongBao = "Vui long nhap day du thong tin.";
                return View();
            }

            if (!string.Equals(MatKhauMoi, NhapLaiMatKhauMoi, StringComparison.Ordinal))
            {
                ViewBag.ThongBao = "Mat khau nhap lai khong khop.";
                return View();
            }

            var user = _context.TaiKhoans.FirstOrDefault(x => x.Email == Email && x.TrangThai == true);
            if (user == null)
            {
                ViewBag.ThongBao = "Khong tim thay tai khoan voi email nay.";
                return View();
            }

            user.MatKhau = MatKhauMoi;
            _context.SaveChanges();

            TempData["ThongBao"] = "Doi mat khau thanh cong. Ban co the dang nhap lai.";
            return RedirectToAction("DangNhap");
        }

        [HttpGet]
        public IActionResult HoSo()
        {
            var id = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (id == null) return RedirectToAction("DangNhap");

            var user = _context.TaiKhoans.FirstOrDefault(x => x.IdTaiKhoan == id);

            var donCuaToi = _context.DonDatTours
                .Include(d => d.IdTourNavigation)
                .Where(d => d.IdTaiKhoan == id)
                .OrderByDescending(d => d.NgayDat)
                .ToList();

            ViewBag.DonCuaToi = donCuaToi;
            return View(user);
        }

        [HttpPost]
        public IActionResult HoSo(TaiKhoan model)
        {
            var id = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (id == null || model.IdTaiKhoan != id.Value) return RedirectToAction("DangNhap");

            var user = _context.TaiKhoans.FirstOrDefault(x => x.IdTaiKhoan == model.IdTaiKhoan);
            if (user == null) return NotFound();

            user.HoTen = model.HoTen;
            user.SoDienThoai = model.SoDienThoai;
            user.DiaChi = model.DiaChi;

            _context.SaveChanges();
            ViewBag.ThongBao = "Cap nhat ho so thanh cong";

            var donCuaToi = _context.DonDatTours
                .Include(d => d.IdTourNavigation)
                .Where(d => d.IdTaiKhoan == id)
                .OrderByDescending(d => d.NgayDat)
                .ToList();

            ViewBag.DonCuaToi = donCuaToi;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiMatKhau(int idTaiKhoan, string matKhauHienTai, string matKhauMoi, string xacNhanMatKhau)
        {
            var id = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (id == null || idTaiKhoan != id.Value) return RedirectToAction("DangNhap");

            var user = _context.TaiKhoans.FirstOrDefault(x => x.IdTaiKhoan == idTaiKhoan);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(matKhauHienTai) || string.IsNullOrWhiteSpace(matKhauMoi) || string.IsNullOrWhiteSpace(xacNhanMatKhau))
            {
                ViewBag.LoiMatKhau = "Vui long nhap day du thong tin doi mat khau.";
            }
            else if (user.MatKhau != matKhauHienTai)
            {
                ViewBag.LoiMatKhau = "Mat khau hien tai khong dung.";
            }
            else if (matKhauMoi.Length < 6)
            {
                ViewBag.LoiMatKhau = "Mat khau moi phai co it nhat 6 ky tu.";
            }
            else if (matKhauMoi != xacNhanMatKhau)
            {
                ViewBag.LoiMatKhau = "Xac nhan mat khau khong khop.";
            }
            else
            {
                user.MatKhau = matKhauMoi;
                _context.SaveChanges();
                ViewBag.ThongBaoMatKhau = "Doi mat khau thanh cong";
            }

            var donCuaToi = _context.DonDatTours
                .Include(d => d.IdTourNavigation)
                .Where(d => d.IdTaiKhoan == id)
                .OrderByDescending(d => d.NgayDat)
                .ToList();

            ViewBag.DonCuaToi = donCuaToi;
            return View("HoSo", user);
        }

        public IActionResult ChiTietDon(int idDon)
        {
            int? userId = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (userId == null)
                return RedirectToAction("DangNhap");

            var don = _context.DonDatTours
                .Include(d => d.IdTourNavigation)
                .Include(d => d.IdLichNavigation)
                .Include(d => d.IdTaiKhoanNavigation)
                .FirstOrDefault(d => d.IdDon == idDon && d.IdTaiKhoan == userId);

            if (don == null)
                return RedirectToAction("DonCuaToi");

            var payload = $"{don.IdDon}|{don.MaBooking}|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            ViewBag.CheckInToken = _checkinProtector.Protect(payload);

            return View(don);
        }

        public IActionResult DonCuaToi()
        {
            int? userId = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (userId == null)
                return RedirectToAction("DangNhap");

            var dsDon = _context.DonDatTours
                .Include(d => d.IdTourNavigation)
                .Include(d => d.IdLichNavigation)
                .Where(d => d.IdTaiKhoan == userId)
                .OrderByDescending(d => d.NgayDat)
                .ToList();

            return View(dsDon);
        }

        [HttpGet]
        public IActionResult KhoPhieuGiamGia()
        {
            int? userId = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (userId == null)
                return RedirectToAction("DangNhap", new { returnUrl = "/TaiKhoan/KhoPhieuGiamGia" });

            var now = DateTime.Now;

            var khoDaLuu = _context.PhieuGiamGiaTaiKhoans
                .Include(x => x.IdPhieuGiamGiaNavigation)
                    .ThenInclude(x => x.IdTourNavigation)
                .Include(x => x.IdPhieuGiamGiaNavigation)
                    .ThenInclude(x => x.IdLoaiTourNavigation)
                .Where(x => x.IdTaiKhoan == userId && x.TrangThai)
                .OrderByDescending(x => x.NgayLuu)
                .ToList();

            var daLuuIds = khoDaLuu
                .Select(x => x.IdPhieuGiamGia)
                .Distinct()
                .ToList();

            var dsPhieuCoTheLuu = _context.PhieuGiamGias
                .Include(x => x.IdTourNavigation)
                .Include(x => x.IdLoaiTourNavigation)
                .Where(x => x.TrangThai
                    && (!x.NgayBatDau.HasValue || x.NgayBatDau.Value <= now)
                    && (!x.NgayKetThuc.HasValue || x.NgayKetThuc.Value >= now))
                .OrderBy(x => x.NgayKetThuc ?? DateTime.MaxValue)
                .ThenByDescending(x => x.GiaTriGiam)
                .ToList();

            ViewBag.DanhSachPhieuCoTheLuu = dsPhieuCoTheLuu;
            ViewBag.DaLuuIds = daLuuIds;

            return View(khoDaLuu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LuuPhieuGiamGia(int idPhieuGiamGia)
        {
            int? userId = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (userId == null)
                return RedirectToAction("DangNhap", new { returnUrl = "/TaiKhoan/KhoPhieuGiamGia" });

            var now = DateTime.Now;
            var phieu = _context.PhieuGiamGias.FirstOrDefault(x => x.IdPhieuGiamGia == idPhieuGiamGia);
            if (phieu == null)
            {
                TempData["Error"] = "Khong tim thay phieu giam gia.";
                return RedirectToAction(nameof(KhoPhieuGiamGia));
            }

            if (!phieu.TrangThai
                || (phieu.NgayBatDau.HasValue && phieu.NgayBatDau.Value > now)
                || (phieu.NgayKetThuc.HasValue && phieu.NgayKetThuc.Value < now))
            {
                TempData["Error"] = "Phieu giam gia khong con hieu luc de luu.";
                return RedirectToAction(nameof(KhoPhieuGiamGia));
            }

            var existing = _context.PhieuGiamGiaTaiKhoans
                .FirstOrDefault(x => x.IdTaiKhoan == userId && x.IdPhieuGiamGia == idPhieuGiamGia);

            if (existing != null)
            {
                if (existing.TrangThai)
                {
                    TempData["Info"] = "Phieu nay da co trong kho cua ban.";
                    return RedirectToAction(nameof(KhoPhieuGiamGia));
                }

                existing.TrangThai = true;
                existing.NgayLuu = now;
            }
            else
            {
                _context.PhieuGiamGiaTaiKhoans.Add(new PhieuGiamGiaTaiKhoan
                {
                    IdTaiKhoan = userId.Value,
                    IdPhieuGiamGia = idPhieuGiamGia,
                    NgayLuu = now,
                    TrangThai = true
                });
            }

            _context.SaveChanges();
            TempData["Success"] = "Da luu phieu giam gia vao kho.";
            return RedirectToAction(nameof(KhoPhieuGiamGia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BoLuuPhieuGiamGia(int idPhieuGiamGia)
        {
            int? userId = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (userId == null)
                return RedirectToAction("DangNhap", new { returnUrl = "/TaiKhoan/KhoPhieuGiamGia" });

            var existing = _context.PhieuGiamGiaTaiKhoans
                .FirstOrDefault(x => x.IdTaiKhoan == userId && x.IdPhieuGiamGia == idPhieuGiamGia && x.TrangThai);

            if (existing != null)
            {
                existing.TrangThai = false;
                _context.SaveChanges();
                TempData["Success"] = "Da bo phieu khoi kho cua ban.";
            }
            else
            {
                TempData["Info"] = "Phieu nay khong co trong kho cua ban.";
            }

            return RedirectToAction(nameof(KhoPhieuGiamGia));
        }

        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("DangNhap");
        }
    }
}
