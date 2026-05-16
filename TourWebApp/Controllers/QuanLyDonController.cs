using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using TourWebApp.Data.Models;
using TourWebApp.Models;

namespace TourWebApp.Controllers
{
    public class QuanLyDonController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataProtector _checkinProtector;

        private const string VoucherStatusHold = "GiuCho";
        private const string VoucherStatusUsed = "DaSuDung";
        private const string VoucherStatusCancelled = "DaHuy";

        public QuanLyDonController(ApplicationDbContext context, IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _checkinProtector = dataProtectionProvider.CreateProtector("HappyTrip.CheckInQrToken.v1");
        }

        private bool IsAdmin()
            => HttpContext.Session.GetString("VaiTro") == "Admin";

        public IActionResult Index(
            string trangThai = "TatCa",
            DateTime? ngayDatTu = null,
            DateTime? ngayDatDen = null,
            string phuongThucTT = "TatCa",
            string? tuKhoa = null)
        {
            if (!IsAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            ViewBag.PendingCashCount = _context.DonDatTours.Count(d =>
                d.TrangThai == BookingPaymentStatus.TrangThaiChoXacNhanTienMat
                && !d.DaThanhToan);

            var tuKhoaDaTrim = string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim();
            var query = BuildOrderQuery(trangThai, ngayDatTu, ngayDatDen, phuongThucTT, tuKhoaDaTrim);

            ViewBag.TrangThai = trangThai;
            ViewBag.NgayDatTu = ngayDatTu?.ToString("yyyy-MM-dd");
            ViewBag.NgayDatDen = ngayDatDen?.ToString("yyyy-MM-dd");
            ViewBag.PhuongThucTT = phuongThucTT;
            ViewBag.TuKhoa = tuKhoaDaTrim ?? string.Empty;
            return View(query.ToList());
        }

        [HttpGet]
        public IActionResult ExportExcel(
            string trangThai = "TatCa",
            DateTime? ngayDatTu = null,
            DateTime? ngayDatDen = null,
            string phuongThucTT = "TatCa",
            string? tuKhoa = null)
        {
            if (!IsAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            var tuKhoaDaTrim = string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim();
            var danhSachDon = BuildOrderQuery(trangThai, ngayDatTu, ngayDatDen, phuongThucTT, tuKhoaDaTrim)
                .AsNoTracking()
                .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("DonDatTour");

            worksheet.Cell(1, 1).Value = "STT";
            worksheet.Cell(1, 2).Value = "Ma booking";
            worksheet.Cell(1, 3).Value = "Khach hang";
            worksheet.Cell(1, 4).Value = "Tour";
            worksheet.Cell(1, 5).Value = "Ngay dat";
            worksheet.Cell(1, 6).Value = "Phuong thuc TT";
            worksheet.Cell(1, 7).Value = "Tong goc";
            worksheet.Cell(1, 8).Value = "Tong thanh toan";
            worksheet.Cell(1, 9).Value = "Trang thai";

            var headerRange = worksheet.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            for (var i = 0; i < danhSachDon.Count; i++)
            {
                var don = danhSachDon[i];
                var row = i + 2;
                var tongThanhToan = don.TongTienSauGiam ?? (don.TongTien - (don.SoTienGiam ?? 0));
                if (tongThanhToan < 0) tongThanhToan = 0;

                worksheet.Cell(row, 1).Value = i + 1;
                worksheet.Cell(row, 2).Value = don.MaBooking;
                worksheet.Cell(row, 3).Value = don.IdTaiKhoanNavigation.HoTen;
                worksheet.Cell(row, 4).Value = don.IdTourNavigation.TenTour;
                worksheet.Cell(row, 5).Value = don.NgayDat;
                worksheet.Cell(row, 6).Value = don.PhuongThucTT ?? string.Empty;
                worksheet.Cell(row, 7).Value = don.TongTien;
                worksheet.Cell(row, 8).Value = tongThanhToan;
                worksheet.Cell(row, 9).Value = GetTrangThaiHienThi(don);
            }

            worksheet.Column(5).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            worksheet.Column(7).Style.NumberFormat.Format = "#,##0";
            worksheet.Column(8).Style.NumberFormat.Format = "#,##0";
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"DanhSachDon_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        public IActionResult ChiTiet(int id)
        {
            if (!IsAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            var don = _context.DonDatTours
                .Include(d => d.IdTourNavigation)
                .Include(d => d.IdLichNavigation)
                .Include(d => d.IdTaiKhoanNavigation)
                .Include(d => d.IdPhieuGiamGiaNavigation)
                .FirstOrDefault(d => d.IdDon == id);

            if (don == null) return NotFound();

            return View(don);
        }

        [HttpGet]
        public IActionResult ScanCheckIn(string token)
        {
            if (!IsAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "QR khong hop le.";
                return RedirectToAction(nameof(Index));
            }

            if (!TryDecodeCheckInToken(token, out var idDon, out var maBooking))
            {
                TempData["Error"] = "QR khong hop le hoac da bi thay doi.";
                return RedirectToAction(nameof(Index));
            }

            var don = _context.DonDatTours
                .Include(d => d.IdTourNavigation)
                .Include(d => d.IdLichNavigation)
                .Include(d => d.IdTaiKhoanNavigation)
                .FirstOrDefault(d => d.IdDon == idDon && d.MaBooking == maBooking);

            if (don == null)
            {
                TempData["Error"] = "Khong tim thay don tu QR.";
                return RedirectToAction(nameof(Index));
            }

            if (!don.DaThanhToan)
            {
                TempData["Error"] = "Don chua thanh toan, khong the check-in.";
                return RedirectToAction("ChiTiet", new { id = don.IdDon });
            }

            return View(don);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XacNhanCheckIn(int idDon)
        {
            if (!IsAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            var don = _context.DonDatTours.FirstOrDefault(x => x.IdDon == idDon);
            if (don == null) return NotFound();

            if (!don.DaThanhToan)
            {
                TempData["Error"] = "Don chua thanh toan, khong the check-in.";
                return RedirectToAction("ChiTiet", new { id = idDon });
            }

            if (don.TrangThai != "HoanTat")
            {
                don.TrangThai = "HoanTat";
                _context.SaveChanges();
            }

            TempData["Success"] = "Da xac nhan check-in cho don.";
            return RedirectToAction("ChiTiet", new { id = idDon });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Duyet(int id)
        {
            if (!IsAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            var don = _context.DonDatTours.Find(id);
            if (don == null) return NotFound();

            if (don.DaThanhToan)
            {
                TempData["Error"] = "Don da thanh toan, khong can duyet.";
                return RedirectToAction("ChiTiet", new { id });
            }

            don.TrangThai = "DaDuyet";
            _context.SaveChanges();

            TempData["Success"] = "Da duyet don.";
            return RedirectToAction("ChiTiet", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Huy(int id, string? ghiChu)
        {
            if (!IsAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            var don = _context.DonDatTours.FirstOrDefault(x => x.IdDon == id);
            if (don == null) return NotFound();

            if (don.DaThanhToan)
            {
                TempData["Error"] = "Don da thanh toan, khong the huy.";
                return RedirectToAction("ChiTiet", new { id });
            }

            don.TrangThai = "DaHuy";
            don.GhiChu = ghiChu;
            don.NgayHuy = DateTime.Now;
            don.TrangThaiThanhToan = "DaHuy";

            ThuHoiLuotPhieuNeuCan(don, "Admin huy don", xoaBanGhiSuDung: false);

            _context.SaveChanges();

            TempData["Success"] = "Da huy don.";
            return RedirectToAction("ChiTiet", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XacNhanDonChoThuTienMat(int id)
        {
            if (!IsAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            var don = _context.DonDatTours
                .Include(x => x.IdLichNavigation)
                .Include(x => x.IdTourNavigation)
                .FirstOrDefault(x => x.IdDon == id);
            if (don == null) return NotFound();

            if (don.DaThanhToan)
            {
                TempData["Info"] = "Don da duoc xac nhan thanh toan truoc do.";
                return RedirectToAction("ChiTiet", new { id });
            }

            if (don.TrangThai == BookingPaymentStatus.TrangThaiDaHuy)
            {
                TempData["Error"] = "Don da huy, khong the xac nhan.";
                return RedirectToAction("ChiTiet", new { id });
            }

            if (!string.Equals(don.PhuongThucTT, BookingPaymentStatus.PhuongThucTienMat, StringComparison.OrdinalIgnoreCase)
                || don.TrangThai != BookingPaymentStatus.TrangThaiChoXacNhanTienMat)
            {
                TempData["Error"] = "Chi xac nhan duoc don dang o che do cho thu tien mat.";
                return RedirectToAction("ChiTiet", new { id });
            }

            don.DaThanhToan = true;
            don.TrangThai = BookingPaymentStatus.TrangThaiThanhToanThanhCong;
            don.TrangThaiThanhToan = BookingPaymentStatus.TrangThaiTtThanhToanThanhCong;
            don.NgayThanhToan = DateTime.Now;
            don.PhuongThucTT = BookingPaymentStatus.PhuongThucTienMat;

            if (don.IdPhieuGiamGia.HasValue)
            {
                var suDung = _context.PhieuGiamGiaSuDungs
                    .Where(x => x.IdDon == don.IdDon && x.IdPhieuGiamGia == don.IdPhieuGiamGia.Value)
                    .OrderByDescending(x => x.IdSuDung)
                    .FirstOrDefault();

                if (suDung != null && suDung.TrangThai != VoucherStatusUsed)
                {
                    suDung.TrangThai = VoucherStatusUsed;
                    suDung.ThoiDiemSuDung = DateTime.Now;
                    suDung.GhiChu = "Admin xac nhan thu tien mat";
                }
            }

            _context.SaveChanges();

            TempData["Success"] = "Da xac nhan don cho thu tien mat thanh cong.";
            return RedirectToAction("ChiTiet", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Xoa(int id)
        {
            if (!IsAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            var don = _context.DonDatTours.FirstOrDefault(x => x.IdDon == id);
            if (don == null) return NotFound();

            if (don.DaThanhToan)
            {
                TempData["Error"] = "Khong the xoa don da thanh toan.";
                return RedirectToAction("Index");
            }

            ThuHoiLuotPhieuNeuCan(don, "Admin xoa don", xoaBanGhiSuDung: true);

            var thongBaos = _context.ThongBaos
                .Where(tb => tb.IdDon == id)
                .ToList();

            if (thongBaos.Any())
            {
                _context.ThongBaos.RemoveRange(thongBaos);
            }

            _context.DonDatTours.Remove(don);
            _context.SaveChanges();

            TempData["Success"] = "Da xoa don chua thanh toan.";
            return RedirectToAction("Index");
        }

        private IQueryable<DonDatTour> BuildOrderQuery(
            string trangThai,
            DateTime? ngayDatTu,
            DateTime? ngayDatDen,
            string phuongThucTT,
            string? tuKhoa)
        {
            var query = _context.DonDatTours
                .Include(d => d.IdTourNavigation)
                .Include(d => d.IdTaiKhoanNavigation)
                .AsQueryable();

            switch (trangThai)
            {
                case "ChoDuyet":
                    query = query.Where(d => d.TrangThai == "ChoDuyet");
                    break;
                case "DaDuyet":
                    query = query.Where(d => d.TrangThai == "DaDuyet");
                    break;
                case "DaHuy":
                    query = query.Where(d => d.TrangThai == "DaHuy");
                    break;
                case "DaThanhToan":
                    query = query.Where(d => d.DaThanhToan);
                    break;
                case "ChuaThanhToan":
                    query = query.Where(d => !d.DaThanhToan);
                    break;
                case "ChoXacNhanTienMat":
                    query = query.Where(d => d.TrangThai == BookingPaymentStatus.TrangThaiChoXacNhanTienMat);
                    break;
            }

            var tuNgay = ngayDatTu?.Date;
            var denNgay = ngayDatDen?.Date;
            if (tuNgay.HasValue && denNgay.HasValue && tuNgay > denNgay)
            {
                var tam = tuNgay;
                tuNgay = denNgay;
                denNgay = tam;
            }

            if (tuNgay.HasValue)
            {
                query = query.Where(d => d.NgayDat >= tuNgay.Value);
            }

            if (denNgay.HasValue)
            {
                var denNgayBaoGom = denNgay.Value.AddDays(1);
                query = query.Where(d => d.NgayDat < denNgayBaoGom);
            }

            if (!string.IsNullOrWhiteSpace(phuongThucTT) && phuongThucTT != "TatCa")
            {
                query = query.Where(d => d.PhuongThucTT == phuongThucTT);
            }

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                query = query.Where(d =>
                    d.MaBooking.Contains(tuKhoa) ||
                    d.IdTaiKhoanNavigation.HoTen.Contains(tuKhoa));
            }

            return query.OrderByDescending(d => d.NgayDat);
        }

        private static string GetTrangThaiHienThi(DonDatTour don)
        {
            if (don.DaThanhToan)
            {
                return "Da thanh toan";
            }

            if (don.TrangThai == BookingPaymentStatus.TrangThaiDaHuy)
            {
                return "Da huy";
            }

            if (don.TrangThai == BookingPaymentStatus.TrangThaiChoXacNhanTienMat)
            {
                return "Cho xac nhan thu cong";
            }

            if (don.HanThanhToan != null && don.HanThanhToan < DateTime.Now)
            {
                return "Het han thanh toan";
            }

            return "Cho thanh toan";
        }

        private void ThuHoiLuotPhieuNeuCan(DonDatTour don, string ghiChu, bool xoaBanGhiSuDung)
        {
            if (!don.IdPhieuGiamGia.HasValue)
            {
                return;
            }

            var lichSuDung = _context.PhieuGiamGiaSuDungs
                .Where(x => x.IdDon == don.IdDon)
                .ToList();

            if (!lichSuDung.Any())
            {
                return;
            }

            var canTruLuot = lichSuDung.Any(x => x.TrangThai == VoucherStatusHold);
            if (canTruLuot)
            {
                var phieu = _context.PhieuGiamGias.FirstOrDefault(x => x.IdPhieuGiamGia == don.IdPhieuGiamGia.Value);
                if (phieu != null && phieu.DaSuDung > 0)
                {
                    phieu.DaSuDung -= 1;
                    phieu.NgayCapNhat = DateTime.Now;
                }
            }

            if (xoaBanGhiSuDung)
            {
                _context.PhieuGiamGiaSuDungs.RemoveRange(lichSuDung);
                return;
            }

            foreach (var item in lichSuDung)
            {
                if (item.TrangThai == VoucherStatusUsed || item.TrangThai == VoucherStatusCancelled)
                {
                    continue;
                }

                item.TrangThai = VoucherStatusCancelled;
                item.ThoiDiemSuDung = DateTime.Now;
                item.GhiChu = ghiChu;
            }
        }

        private bool TryDecodeCheckInToken(string token, out int idDon, out string maBooking)
        {
            idDon = 0;
            maBooking = string.Empty;

            try
            {
                var raw = _checkinProtector.Unprotect(token);
                var parts = raw.Split('|');
                if (parts.Length != 3) return false;
                if (!int.TryParse(parts[0], out idDon)) return false;
                maBooking = parts[1];
                if (string.IsNullOrWhiteSpace(maBooking)) return false;
                if (!long.TryParse(parts[2], out _)) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
