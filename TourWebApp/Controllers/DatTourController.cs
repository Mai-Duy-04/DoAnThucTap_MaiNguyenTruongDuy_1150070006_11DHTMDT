using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TourWebApp.Data.Models;
using TourWebApp.Models;
using TourWebApp.Models.ViewModels;
using TourWebApp.Services;
using VNPAY;
using VNPAY.Models.Enums;
using VNPAY.Models.Exceptions;

namespace TourWebApp.Controllers
{
    public class DatTourController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IVnpayClient _vnpayClient;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DatTourController> _logger;
        private readonly IDataProtector _checkinProtector;
        private readonly IEmailService _emailService;

        private const string VoucherStatusHold = "GiuCho";
        private const string VoucherStatusUsed = "DaSuDung";
        private const string VoucherStatusCancelled = "DaHuy";

        public DatTourController(ApplicationDbContext db, IVnpayClient vnpayClient, IConfiguration configuration, IWebHostEnvironment environment, ILogger<DatTourController> logger, IDataProtectionProvider dataProtectionProvider, IEmailService emailService)
        {
            _db = db;
            _vnpayClient = vnpayClient;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
            _checkinProtector = dataProtectionProvider.CreateProtector("HappyTrip.CheckInQrToken.v1");
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult NhapThongTin(int idTour, int idLich, int adult = 1, int child = 0, int baby = 0)
        {
            int? userId = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (userId == null)
            {
                TempData["ReturnUrl"] =
                    $"/DatTour/NhapThongTin?idTour={idTour}&idLich={idLich}&adult={adult}&child={child}&baby={baby}";
                TempData["Error"] = "Vui long dang nhap de dat tour!";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            ViewBag.User = _db.TaiKhoans.FirstOrDefault(x => x.IdTaiKhoan == userId);

            var tour = _db.Tours
                .Include(t => t.TourGiaChiTiets)
                .FirstOrDefault(t => t.IdTour == idTour);

            var lich = _db.LichKhoiHanhs.FirstOrDefault(x => x.IdLich == idLich && x.IdTour == idTour);

            if (tour == null || lich == null)
            {
                TempData["Error"] = "Khong tim thay tour hoac lich khoi hanh.";
                return RedirectToAction("Index", "Home");
            }

            NormalizeGuestCounts(ref adult, ref child, ref baby);

            var gia = TinhGiaDatTour(tour, adult, child, baby);

            var vm = new NhapThongTinVM
            {
                IdTour = idTour,
                IdLich = idLich,
                TenTour = tour.TenTour ?? string.Empty,
                NgayKhoiHanh = lich.NgayKhoiHanh.ToDateTime(TimeOnly.MinValue),
                NguoiLon = adult,
                TreEm = child,
                EmBe = baby,
                GiaNguoiLon = gia.GiaNguoiLon,
                GiaTreEm = gia.GiaTreEm,
                GiaEmBe = gia.GiaEmBe,
                TongTien = gia.TongTien,
                SoTienGiam = 0,
                TongTienSauGiam = gia.TongTien,
                MaPhieuGiamGia = string.Empty
            };

            ViewBag.DanhSachMaPhieuDaLuu = LayMaPhieuDaLuu(userId.Value);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoDonVaChuyenSangThanhToan(NhapThongTinVM model)
        {
            int userId = HttpContext.Session.GetInt32("IdTaiKhoan") ?? 0;
            if (userId <= 0)
            {
                TempData["Error"] = "Vui long dang nhap de dat tour.";
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var tour = _db.Tours
                .Include(t => t.TourGiaChiTiets)
                .FirstOrDefault(t => t.IdTour == model.IdTour);

            var lich = _db.LichKhoiHanhs.FirstOrDefault(x => x.IdLich == model.IdLich && x.IdTour == model.IdTour);

            if (tour == null || lich == null)
            {
                TempData["Error"] = "Khong tim thay tour hoac lich khoi hanh.";
                return RedirectToAction("Index", "Home");
            }

            var adult = model.NguoiLon;
            var child = model.TreEm;
            var baby = model.EmBe;
            NormalizeGuestCounts(ref adult, ref child, ref baby);
            model.NguoiLon = adult;
            model.TreEm = child;
            model.EmBe = baby;

            model.TenTour = tour.TenTour ?? string.Empty;
            model.NgayKhoiHanh = lich.NgayKhoiHanh.ToDateTime(TimeOnly.MinValue);

            var gia = TinhGiaDatTour(tour, model.NguoiLon, model.TreEm, model.EmBe);
            model.GiaNguoiLon = gia.GiaNguoiLon;
            model.GiaTreEm = gia.GiaTreEm;
            model.GiaEmBe = gia.GiaEmBe;
            model.TongTien = gia.TongTien;
            model.SoTienGiam = 0;
            model.TongTienSauGiam = gia.TongTien;
            model.PhuongThucThanhToan = string.IsNullOrWhiteSpace(model.PhuongThucThanhToan)
                ? NhapThongTinVM.PaymentMethodVnpay
                : model.PhuongThucThanhToan.Trim();

            PhieuGiamGia? phieu = null;
            decimal soTienGiam = 0;

            var checkPhieu = KiemTraPhieuGiamGia(model.MaPhieuGiamGia, tour, userId, model.TongTien);
            if (!checkPhieu.HopLe)
            {
                ViewBag.User = _db.TaiKhoans.FirstOrDefault(x => x.IdTaiKhoan == userId);
                ViewBag.DanhSachMaPhieuDaLuu = LayMaPhieuDaLuu(userId);
                ModelState.AddModelError(nameof(model.MaPhieuGiamGia), checkPhieu.ThongBaoLoi);
                return View("NhapThongTin", model);
            }

            if (checkPhieu.Phieu != null)
            {
                phieu = checkPhieu.Phieu;
                soTienGiam = checkPhieu.SoTienGiam;
                model.SoTienGiam = soTienGiam;
                model.TongTienSauGiam = model.TongTien - soTienGiam;
                model.MaPhieuGiamGia = phieu.MaPhieu;
            }

            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var don = new DonDatTour
                {
                    MaBooking = TaoMaBooking(),
                    IdTour = model.IdTour,
                    IdLich = model.IdLich,
                    IdTaiKhoan = userId,
                    NguoiLon = model.NguoiLon,
                    TreEm = model.TreEm,
                    TreNho = model.EmBe,
                    GhiChu = model.GhiChu,
                    // Persist the computed booking amount so voucher/payment logic uses the real value.
                    TongTien = model.TongTien,
                    NgayDat = DateTime.Now,
                    HanThanhToan = DateTime.Now.AddMinutes(10),
                    TrangThai = BookingPaymentStatus.TrangThaiChoThanhToan,
                    DaThanhToan = false,
                    TrangThaiThanhToan = BookingPaymentStatus.TrangThaiTtChoThanhToan,
                    PhuongThucTT = model.PhuongThucThanhToan
                };

                _db.DonDatTours.Add(don);
                _db.SaveChanges();

                _db.Entry(don).Reload();

                if (phieu != null)
                {
                    _db.Entry(phieu).Reload();

                    if (phieu.TongLuotSuDung.HasValue && phieu.DaSuDung >= phieu.TongLuotSuDung.Value)
                    {
                        transaction.Rollback();
                        ViewBag.User = _db.TaiKhoans.FirstOrDefault(x => x.IdTaiKhoan == userId);
                        ViewBag.DanhSachMaPhieuDaLuu = LayMaPhieuDaLuu(userId);
                        ModelState.AddModelError(nameof(model.MaPhieuGiamGia), "Phieu giam gia da het luot su dung.");
                        return View("NhapThongTin", model);
                    }

                    var giamThucTe = TinhSoTienGiam(phieu, don.TongTien);
                    don.IdPhieuGiamGia = phieu.IdPhieuGiamGia;
                    don.MaPhieuGiamGia = phieu.MaPhieu;
                    don.SoTienGiam = giamThucTe;
                    don.TongTienSauGiam = don.TongTien - giamThucTe;

                    phieu.DaSuDung += 1;
                    phieu.NgayCapNhat = DateTime.Now;

                    _db.PhieuGiamGiaSuDungs.Add(new PhieuGiamGiaSuDung
                    {
                        IdPhieuGiamGia = phieu.IdPhieuGiamGia,
                        IdDon = don.IdDon,
                        IdTaiKhoan = userId,
                        MaPhieu = phieu.MaPhieu,
                        SoTienGiam = giamThucTe,
                        ThoiDiemSuDung = DateTime.Now,
                        TrangThai = VoucherStatusHold,
                        GhiChu = "Tam giu luot cho den khi thanh toan"
                    });
                }

                _db.SaveChanges();
                transaction.Commit();

                if (string.Equals(model.PhuongThucThanhToan, NhapThongTinVM.PaymentMethodCash, StringComparison.OrdinalIgnoreCase))
                {
                    don.TrangThai = BookingPaymentStatus.TrangThaiChoXacNhanTienMat;
                    don.TrangThaiThanhToan = BookingPaymentStatus.TrangThaiTtChoThuTienMat;
                    _db.SaveChanges();

                    TempData["Info"] = "Don da duoc tao voi hinh thuc tien mat. Vui long den diem hen de thanh toan hoac cho admin xac nhan.";
                    return RedirectToAction("ThanhToan", new { idDon = don.IdDon });
                }

                return RedirectToAction("ThanhToan", new { idDon = don.IdDon });
            }
            catch
            {
                transaction.Rollback();
                ViewBag.User = _db.TaiKhoans.FirstOrDefault(x => x.IdTaiKhoan == userId);
                ViewBag.DanhSachMaPhieuDaLuu = LayMaPhieuDaLuu(userId);
                ModelState.AddModelError(string.Empty, "Khong tao duoc don dat tour. Vui long thu lai.");
                return View("NhapThongTin", model);
            }
        }

        public IActionResult ThanhToan(int idDon)
        {
            int? userId = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (userId == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan", new { returnUrl = $"/DatTour/ThanhToan?idDon={idDon}" });
            }

            var don = _db.DonDatTours
                .Include(t => t.IdTourNavigation)
                .Include(t => t.IdLichNavigation)
                .FirstOrDefault(t => t.IdDon == idDon && t.IdTaiKhoan == userId.Value);

            if (don == null)
            {
                TempData["Error"] = "Khong tim thay don hang.";
                return RedirectToAction("DonCuaToi", "TaiKhoan");
            }

            if (don.TrangThai == BookingPaymentStatus.TrangThaiChoThanhToan && DonDaHetHan(don))
            {
                HuyDonQuaHan(don, "Don het han thanh toan");
                _db.SaveChanges();

                TempData["Error"] = "Don da het han va bi huy tu dong!";
                return RedirectToAction("DonCuaToi", "TaiKhoan");
            }

            // Backfill discount values for orders that have voucher code but missing computed fields.
            if (!string.IsNullOrWhiteSpace(don.MaPhieuGiamGia)
                && (!don.SoTienGiam.HasValue || !don.TongTienSauGiam.HasValue))
            {
                var phieu = _db.PhieuGiamGias.FirstOrDefault(x => x.MaPhieu == don.MaPhieuGiamGia);
                if (phieu != null)
                {
                    var giamThucTe = TinhSoTienGiam(phieu, don.TongTien);
                    don.SoTienGiam = giamThucTe;
                    don.TongTienSauGiam = don.TongTien - giamThucTe;
                    _db.SaveChanges();
                }
            }

            return View(don);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThanhToanVnpay(int idDon, BankCode bankCode = BankCode.ANY)
        {
            int? userId = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (userId == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan", new { returnUrl = $"/DatTour/ThanhToan?idDon={idDon}" });
            }

            var don = _db.DonDatTours
                .Include(t => t.IdLichNavigation)
                .Include(t => t.IdTourNavigation)
                .FirstOrDefault(t => t.IdDon == idDon && t.IdTaiKhoan == userId.Value);

            if (don == null)
            {
                TempData["Error"] = "Don khong ton tai hoac ban khong co quyen thanh toan don nay.";
                return RedirectToAction("DonCuaToi", "TaiKhoan");
            }

            if (don.DaThanhToan || don.TrangThai == BookingPaymentStatus.TrangThaiThanhToanThanhCong)
            {
                TempData["Info"] = "Don da thanh toan truoc do.";
                return RedirectToAction("HoanTat", new { idDon = don.IdDon });
            }

            if (string.Equals(don.PhuongThucTT, NhapThongTinVM.PaymentMethodCash, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Don nay da chon thanh toan tien mat, khong the thanh toan qua VNPAY.";
                return RedirectToAction("ThanhToan", new { idDon = don.IdDon });
            }

            if (don.TrangThai == "ChoThanhToan" && DonDaHetHan(don))
            {
                HuyDonQuaHan(don, "Don het han truoc khi chuyen den VNPAY");
                _db.SaveChanges();
                TempData["Error"] = "Don da het han, vui long dat lai.";
                return RedirectToAction("DonCuaToi", "TaiKhoan");
            }

            var callbackUrl = _configuration["VNPAY:CallbackUrl"] ?? string.Empty;
            if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out var callbackUri)
                || (callbackUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || callbackUri.Host.Equals("127.0.0.1")) && !_environment.IsDevelopment())
            {
                TempData["Error"] = "Cau hinh VNPAY chua hop le: CallbackUrl khong duoc de localhost. Hay dung domain HTTPS public (vd ngrok) da dang ky voi VNPAY.";
                return RedirectToAction("ThanhToan", new { idDon = don.IdDon });
            }

            var tongCanThanhToan = TinhTongCanThanhToan(don);

            // VNPAY sandbox only accepts payments from 5,000 VND.
            // If voucher discounts total to 0, finalize the order without sending to gateway.
            if (tongCanThanhToan == 0)
            {
                CapNhatDonThanhToanThanhCong(don, "MienPhi", "FREE-ORDER");
                _db.SaveChanges();
                TempData["Info"] = "Don hang co gia tri 0d, he thong da tu dong xac nhan thanh toan.";
                return RedirectToAction("HoanTat", new { idDon = don.IdDon });
            }

            if (tongCanThanhToan < 5000 || tongCanThanhToan > 1000000000)
            {
                TempData["Error"] = "So tien thanh toan VNPAY phai tu 5.000 den 1.000.000.000 VND.";
                return RedirectToAction("ThanhToan", new { idDon = don.IdDon });
            }

            try
            {
                var paymentUrlInfo = _vnpayClient.CreatePaymentUrl(
                    (double)tongCanThanhToan,
                    $"Thanh toan tour {don.MaBooking}",
                    bankCode);

                don.MaGiaoDich = paymentUrlInfo.PaymentId.ToString();
                don.PhuongThucTT = BookingPaymentStatus.PhuongThucVnpay;
                don.TrangThaiThanhToan = BookingPaymentStatus.TrangThaiTtDangXuLyVnpay;
                _db.SaveChanges();

                // Debug log to verify the exact return URL embedded in VNPAY request.
                if (Uri.TryCreate(paymentUrlInfo.Url, UriKind.Absolute, out var paymentUri))
                {
                    var returnUrlInQuery = System.Web.HttpUtility.ParseQueryString(paymentUri.Query)["vnp_ReturnUrl"];
                    _logger.LogInformation("VNPAY create URL | Don={IdDon} | PaymentId={PaymentId} | ReturnUrl={ReturnUrl} | RawUrl={RawUrl}",
                        don.IdDon,
                        paymentUrlInfo.PaymentId,
                        returnUrlInQuery,
                        paymentUrlInfo.Url);
                }
                else
                {
                    _logger.LogWarning("VNPAY create URL parse failed | Don={IdDon} | RawUrl={RawUrl}", don.IdDon, paymentUrlInfo.Url);
                }

                return Redirect(paymentUrlInfo.Url);
            }
            catch (Exception)
            {
                TempData["Error"] = "Khong tao duoc URL thanh toan VNPAY. Vui long kiem tra cau hinh TmnCode/HashSecret/CallbackUrl.";
                return RedirectToAction("ThanhToan", new { idDon = don.IdDon });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChuyenSangThanhToanTaiQuay(int idDon)
        {
            int? userId = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (userId == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan", new { returnUrl = $"/DatTour/ThanhToan?idDon={idDon}" });
            }

            var don = _db.DonDatTours.FirstOrDefault(t => t.IdDon == idDon && t.IdTaiKhoan == userId.Value);
            if (don == null)
            {
                TempData["Error"] = "Don khong ton tai hoac ban khong co quyen thao tac.";
                return RedirectToAction("DonCuaToi", "TaiKhoan");
            }

            if (don.DaThanhToan || don.TrangThai == BookingPaymentStatus.TrangThaiThanhToanThanhCong)
            {
                TempData["Info"] = "Don da thanh toan truoc do.";
                return RedirectToAction("HoanTat", new { idDon = don.IdDon });
            }

            if (don.TrangThai == BookingPaymentStatus.TrangThaiDaHuy || don.TrangThaiThanhToan == BookingPaymentStatus.TrangThaiTtHetHanThanhToan)
            {
                TempData["Error"] = "Don da huy hoac het han, khong the doi hinh thuc thanh toan.";
                return RedirectToAction("ThanhToan", new { idDon = don.IdDon });
            }

            don.PhuongThucTT = NhapThongTinVM.PaymentMethodCash;
            don.TrangThai = BookingPaymentStatus.TrangThaiChoXacNhanTienMat;
            don.TrangThaiThanhToan = BookingPaymentStatus.TrangThaiTtChoThuTienMat;
            _db.SaveChanges();

            TempData["Info"] = "Da chuyen sang hinh thuc thanh toan tai quay. Vui long den diem hen de thanh toan.";
            return RedirectToAction("ThanhToan", new { idDon = don.IdDon });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChuyenSangThanhToanOnline(int idDon)
        {
            int? userId = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (userId == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan", new { returnUrl = $"/DatTour/ThanhToan?idDon={idDon}" });
            }

            var don = _db.DonDatTours.FirstOrDefault(t => t.IdDon == idDon && t.IdTaiKhoan == userId.Value);
            if (don == null)
            {
                TempData["Error"] = "Don khong ton tai hoac ban khong co quyen thao tac.";
                return RedirectToAction("DonCuaToi", "TaiKhoan");
            }

            if (don.DaThanhToan || don.TrangThai == BookingPaymentStatus.TrangThaiThanhToanThanhCong)
            {
                TempData["Info"] = "Don da thanh toan truoc do.";
                return RedirectToAction("HoanTat", new { idDon = don.IdDon });
            }

            if (don.TrangThai == BookingPaymentStatus.TrangThaiDaHuy || don.TrangThaiThanhToan == BookingPaymentStatus.TrangThaiTtHetHanThanhToan)
            {
                TempData["Error"] = "Don da huy hoac het han, khong the doi hinh thuc thanh toan.";
                return RedirectToAction("ThanhToan", new { idDon = don.IdDon });
            }

            don.PhuongThucTT = BookingPaymentStatus.PhuongThucVnpay;
            don.TrangThai = BookingPaymentStatus.TrangThaiChoThanhToan;
            don.TrangThaiThanhToan = BookingPaymentStatus.TrangThaiTtChoThanhToan;
            _db.SaveChanges();

            TempData["Info"] = "Da chuyen sang hinh thuc thanh toan online.";
            return RedirectToAction("ThanhToan", new { idDon = don.IdDon });
        }

        [HttpGet]
        public IActionResult VnpayCallback()
        {
            _logger.LogInformation(
                "VNPAY callback hit | Query={Query} | TxnRef={TxnRef} | ResponseCode={ResponseCode} | TransactionStatus={TransactionStatus}",
                Request.QueryString.Value,
                Request.Query["vnp_TxnRef"].ToString(),
                Request.Query["vnp_ResponseCode"].ToString(),
                Request.Query["vnp_TransactionStatus"].ToString());

            try
            {
                var paymentResult = _vnpayClient.GetPaymentResult(Request);
                var paymentId = paymentResult.PaymentId.ToString();

                _logger.LogInformation(
                    "VNPAY callback parsed | PaymentId={PaymentId} | VnpTxnId={VnpTxnId}",
                    paymentId,
                    paymentResult.VnpayTransactionId);

                var don = _db.DonDatTours
                    .Include(t => t.IdLichNavigation)
                    .Include(t => t.IdTourNavigation)
                    .FirstOrDefault(t => t.MaGiaoDich == paymentId);

                if (don == null)
                {
                    TempData["Error"] = "Khong tim thay don tu giao dich VNPAY.";
                    return RedirectToAction("DonCuaToi", "TaiKhoan");
                }

                if (don.TrangThai == "ChoThanhToan" && DonDaHetHan(don))
                {
                    HuyDonQuaHan(don, "Don het han khi VNPAY callback");
                    _db.SaveChanges();
                    TempData["Error"] = "Don da het han thanh toan.";
                    return RedirectToAction("DonCuaToi", "TaiKhoan");
                }

                if (!don.DaThanhToan)
                {
                    CapNhatDonThanhToanThanhCong(don, "VNPAY", paymentResult.VnpayTransactionId.ToString());
                    _db.SaveChanges();
                    _ = GuiMailThanhToanThanhCongAsync(don);
                }

                TempData["Success"] = "Thanh toan VNPAY thanh cong.";
                return RedirectToAction("HoanTat", new { idDon = don.IdDon });
            }
            catch (VnpayException ex)
            {
                var paymentId = Request.Query["vnp_TxnRef"].ToString();
                _logger.LogWarning(ex,
                    "VNPAY callback validation failed | TxnRef={TxnRef} | ResponseCode={ResponseCode} | TransactionStatus={TransactionStatus}",
                    paymentId,
                    Request.Query["vnp_ResponseCode"].ToString(),
                    Request.Query["vnp_TransactionStatus"].ToString());
                if (!string.IsNullOrWhiteSpace(paymentId))
                {
                    var don = _db.DonDatTours.FirstOrDefault(t => t.MaGiaoDich == paymentId);
                    if (don != null && !don.DaThanhToan)
                    {
                        don.TrangThaiThanhToan = $"VNPAY:{ex.TransactionStatusCode}";
                        _db.SaveChanges();
                    }
                }

                TempData["Error"] = $"Thanh toan VNPAY khong thanh cong: {ex.Message}";
                return RedirectToAction("DonCuaToi", "TaiKhoan");
            }
            catch (Exception)
            {
                _logger.LogError(
                    "VNPAY callback unexpected error | Query={Query}",
                    Request.QueryString.Value);
                TempData["Error"] = "Co loi xay ra khi xu ly callback VNPAY.";
                return RedirectToAction("DonCuaToi", "TaiKhoan");
            }
        }

        [HttpGet]
        public IActionResult VnpayIpn()
        {
            _logger.LogInformation(
                "VNPAY ipn hit | Query={Query} | TxnRef={TxnRef} | ResponseCode={ResponseCode} | TransactionStatus={TransactionStatus}",
                Request.QueryString.Value,
                Request.Query["vnp_TxnRef"].ToString(),
                Request.Query["vnp_ResponseCode"].ToString(),
                Request.Query["vnp_TransactionStatus"].ToString());

            try
            {
                var paymentResult = _vnpayClient.GetPaymentResult(Request);
                var paymentId = paymentResult.PaymentId.ToString();

                _logger.LogInformation(
                    "VNPAY ipn parsed | PaymentId={PaymentId} | VnpTxnId={VnpTxnId}",
                    paymentId,
                    paymentResult.VnpayTransactionId);

                var don = _db.DonDatTours
                    .Include(t => t.IdLichNavigation)
                    .Include(t => t.IdTourNavigation)
                    .FirstOrDefault(t => t.MaGiaoDich == paymentId);

                if (don == null)
                {
                    return Json(new { RspCode = "01", Message = "Order not found" });
                }

                if (don.DaThanhToan)
                {
                    return Json(new { RspCode = "02", Message = "Order already confirmed" });
                }

                if (don.TrangThai == "ChoThanhToan" && DonDaHetHan(don))
                {
                    HuyDonQuaHan(don, "Don het han khi VNPAY IPN");
                    _db.SaveChanges();
                    return Json(new { RspCode = "03", Message = "Order expired" });
                }

                CapNhatDonThanhToanThanhCong(don, "VNPAY", paymentResult.VnpayTransactionId.ToString());
                _db.SaveChanges();
                _ = GuiMailThanhToanThanhCongAsync(don);

                return Json(new { RspCode = "00", Message = "Confirm Success" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "VNPAY ipn invalid payload | Query={Query}",
                    Request.QueryString.Value);
                return Json(new { RspCode = "99", Message = "Input data invalid" });
            }
        }

        public IActionResult XacNhanThanhToan(int idDon)
        {
            int? userId = HttpContext.Session.GetInt32("IdTaiKhoan");
            if (userId == null)
            {
                return RedirectToAction("DangNhap", "TaiKhoan", new { returnUrl = $"/DatTour/ThanhToan?idDon={idDon}" });
            }

            var don = _db.DonDatTours
                .Include(t => t.IdLichNavigation)
                .Include(t => t.IdTourNavigation)
                .FirstOrDefault(t => t.IdDon == idDon && t.IdTaiKhoan == userId.Value);

            if (don == null)
            {
                TempData["Error"] = "Don khong ton tai.";
                return RedirectToAction("DonCuaToi", "TaiKhoan");
            }

            if (DonDaHetHan(don))
            {
                HuyDonQuaHan(don, "Don het han khi xac nhan thanh toan");
                _db.SaveChanges();
                TempData["Error"] = "Don da het han va bi huy tu dong!";
                return RedirectToAction("DonCuaToi", "TaiKhoan");
            }

            CapNhatDonThanhToanThanhCong(don, "ChuyenKhoanQR", don.MaGiaoDich);

            _db.SaveChanges();

            return RedirectToAction("HoanTat", new { idDon });
        }

        public IActionResult HoanTat(int idDon)
        {
            var don = _db.DonDatTours
                .Include(t => t.IdTourNavigation)
                .Include(t => t.IdLichNavigation)
                .FirstOrDefault(t => t.IdDon == idDon);

            if (don == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var daXacNhanCheckIn = don.DaThanhToan == true
                && (don.TrangThai == BookingPaymentStatus.TrangThaiThanhToanThanhCong
                    || don.TrangThai == "HoanTat");

            if (daXacNhanCheckIn && !string.IsNullOrWhiteSpace(don.MaBooking))
            {
                var payload = $"{don.IdDon}|{don.MaBooking}";
                ViewBag.CheckInToken = _checkinProtector.Protect(payload);
            }

            return View(don);
        }

        private static bool DonDaHetHan(DonDatTour don)
        {
            return don.HanThanhToan.HasValue && don.HanThanhToan.Value < DateTime.Now;
        }

        private static decimal TinhTongCanThanhToan(DonDatTour don)
        {
            var tongGoc = don.TongTien;
            var soTienGiam = don.SoTienGiam ?? 0;
            var tongCanThanhToan = don.TongTienSauGiam ?? (tongGoc - soTienGiam);
            return tongCanThanhToan < 0 ? 0 : tongCanThanhToan;
        }

        private void HuyDonQuaHan(DonDatTour don, string ghiChu)
        {
            don.TrangThai = BookingPaymentStatus.TrangThaiDaHuy;
            don.TrangThaiThanhToan = BookingPaymentStatus.TrangThaiTtHetHanThanhToan;

            int soKhach = don.NguoiLon + don.TreEm + don.TreNho;
            don.IdLichNavigation.SoChoConLai += soKhach;
            don.IdTourNavigation.SoNguoiDaDat -= soKhach;

            HoanLaiPhieuGiamGiaNeuCan(don, ghiChu);

            _db.Entry(don).Property(x => x.TrangThai).IsModified = true;
            _db.Entry(don).Property(x => x.TrangThaiThanhToan).IsModified = true;
            _db.Entry(don.IdLichNavigation).Property(x => x.SoChoConLai).IsModified = true;
            _db.Entry(don.IdTourNavigation).Property(x => x.SoNguoiDaDat).IsModified = true;
        }

        private void CapNhatDonThanhToanThanhCong(DonDatTour don, string phuongThuc, string? maGiaoDich)
        {
            if (don.DaThanhToan)
            {
                return;
            }

            don.DaThanhToan = true;
            don.TrangThai = BookingPaymentStatus.TrangThaiThanhToanThanhCong;
            don.TrangThaiThanhToan = BookingPaymentStatus.TrangThaiTtThanhToanThanhCong;
            don.NgayThanhToan = DateTime.Now;
            don.PhuongThucTT = phuongThuc;
            if (!string.IsNullOrWhiteSpace(maGiaoDich))
            {
                don.MaGiaoDich = maGiaoDich;
            }

            int soKhach = don.NguoiLon + don.TreEm + don.TreNho;
            don.IdLichNavigation.SoChoConLai -= soKhach;
            don.IdTourNavigation.SoNguoiDaDat += soKhach;

            DanhDauPhieuDaSuDung(don);

            _db.Entry(don).Property(x => x.DaThanhToan).IsModified = true;
            _db.Entry(don).Property(x => x.TrangThai).IsModified = true;
            _db.Entry(don).Property(x => x.TrangThaiThanhToan).IsModified = true;
            _db.Entry(don).Property(x => x.NgayThanhToan).IsModified = true;
            _db.Entry(don).Property(x => x.PhuongThucTT).IsModified = true;
            _db.Entry(don).Property(x => x.MaGiaoDich).IsModified = true;

            _db.Entry(don.IdLichNavigation).Property(x => x.SoChoConLai).IsModified = true;
            _db.Entry(don.IdTourNavigation).Property(x => x.SoNguoiDaDat).IsModified = true;
        }

        private static void NormalizeGuestCounts(ref int adult, ref int child, ref int baby)
        {
            adult = adult < 1 ? 1 : adult;
            child = child < 0 ? 0 : child;
            baby = baby < 0 ? 0 : baby;
        }

        private List<string> LayMaPhieuDaLuu(int idTaiKhoan)
        {
            var now = DateTime.Now;
            return _db.PhieuGiamGiaTaiKhoans
                .Include(x => x.IdPhieuGiamGiaNavigation)
                .Where(x => x.IdTaiKhoan == idTaiKhoan
                    && x.TrangThai
                    && x.IdPhieuGiamGiaNavigation.TrangThai
                    && (!x.IdPhieuGiamGiaNavigation.NgayBatDau.HasValue || x.IdPhieuGiamGiaNavigation.NgayBatDau.Value <= now)
                    && (!x.IdPhieuGiamGiaNavigation.NgayKetThuc.HasValue || x.IdPhieuGiamGiaNavigation.NgayKetThuc.Value >= now))
                .Select(x => x.IdPhieuGiamGiaNavigation.MaPhieu)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        private (decimal GiaNguoiLon, decimal GiaTreEm, decimal GiaEmBe, decimal TongTien) TinhGiaDatTour(Tour tour, int nguoiLon, int treEm, int emBe)
        {
            decimal giaNguoiLon = (decimal)(tour.GiaKhuyenMai ?? tour.GiaGoc ?? 0);
            decimal giaTreEm = (decimal)(tour.TourGiaChiTiets.FirstOrDefault(x => x.DoiTuong == "Trẻ em")?.Gia ?? 0);
            decimal giaEmBe = (decimal)(tour.TourGiaChiTiets.FirstOrDefault(x => x.DoiTuong == "Em bé")?.Gia ?? 0);

            decimal tongTien = nguoiLon * giaNguoiLon + treEm * giaTreEm + emBe * giaEmBe;
            return (giaNguoiLon, giaTreEm, giaEmBe, tongTien);
        }

        private (bool HopLe, string ThongBaoLoi, PhieuGiamGia? Phieu, decimal SoTienGiam) KiemTraPhieuGiamGia(string? maPhieuInput, Tour tour, int idTaiKhoan, decimal tongTien)
        {
            if (string.IsNullOrWhiteSpace(maPhieuInput))
            {
                return (true, string.Empty, null, 0);
            }

            var maPhieu = maPhieuInput.Trim().ToUpperInvariant();

            var phieu = _db.PhieuGiamGias.FirstOrDefault(x => x.MaPhieu == maPhieu);
            if (phieu == null)
            {
                return (false, "Ma phieu khong ton tai.", null, 0);
            }

            if (!phieu.TrangThai)
            {
                return (false, "Phieu giam gia dang tam khoa.", null, 0);
            }

            var now = DateTime.Now;
            if (phieu.NgayBatDau.HasValue && now < phieu.NgayBatDau.Value)
            {
                return (false, "Phieu chua den thoi gian su dung.", null, 0);
            }

            if (phieu.NgayKetThuc.HasValue && now > phieu.NgayKetThuc.Value)
            {
                return (false, "Phieu da het han.", null, 0);
            }

            if (phieu.TongLuotSuDung.HasValue && phieu.DaSuDung >= phieu.TongLuotSuDung.Value)
            {
                return (false, "Phieu da het luot su dung.", null, 0);
            }

            if (phieu.DonToiThieu > 0 && tongTien < phieu.DonToiThieu)
            {
                return (false, $"Don hang toi thieu {phieu.DonToiThieu:N0} de ap dung phieu.", null, 0);
            }

            var phamVi = (phieu.PhamViApDung ?? "TatCa").Trim();
            if (phamVi.Equals("Tour", StringComparison.OrdinalIgnoreCase))
            {
                if (!phieu.IdTour.HasValue || phieu.IdTour.Value != tour.IdTour)
                {
                    return (false, "Phieu chi ap dung cho tour duoc chi dinh.", null, 0);
                }
            }
            else if (phamVi.Equals("LoaiTour", StringComparison.OrdinalIgnoreCase))
            {
                if (!phieu.IdLoaiTour.HasValue || phieu.IdLoaiTour.Value != tour.IdLoaiTour)
                {
                    return (false, "Phieu chi ap dung cho nhom tour duoc chi dinh.", null, 0);
                }
            }

            if (phieu.LuotToiDaMoiTaiKhoan.HasValue)
            {
                var daDung = _db.PhieuGiamGiaSuDungs.Count(x =>
                    x.IdPhieuGiamGia == phieu.IdPhieuGiamGia &&
                    x.IdTaiKhoan == idTaiKhoan &&
                    x.TrangThai == VoucherStatusUsed);

                if (daDung >= phieu.LuotToiDaMoiTaiKhoan.Value)
                {
                    return (false, "Ban da dung het so luot cho phep cua phieu nay.", null, 0);
                }
            }

            var soTienGiam = TinhSoTienGiam(phieu, tongTien);
            if (soTienGiam <= 0)
            {
                return (false, "Phieu giam gia khong hop le voi don hien tai.", null, 0);
            }

            return (true, string.Empty, phieu, soTienGiam);
        }

        private static decimal TinhSoTienGiam(PhieuGiamGia phieu, decimal tongTien)
        {
            if (tongTien <= 0) return 0;

            decimal soTienGiam;
            if ((phieu.LoaiGiam ?? string.Empty).Equals("PhanTram", StringComparison.OrdinalIgnoreCase))
            {
                soTienGiam = tongTien * (phieu.GiaTriGiam / 100m);
            }
            else
            {
                soTienGiam = phieu.GiaTriGiam;
            }

            if (phieu.GiamToiDa.HasValue && phieu.GiamToiDa.Value > 0)
            {
                soTienGiam = Math.Min(soTienGiam, phieu.GiamToiDa.Value);
            }

            soTienGiam = Math.Max(0, soTienGiam);
            soTienGiam = Math.Min(soTienGiam, tongTien);

            return decimal.Round(soTienGiam, 0, MidpointRounding.AwayFromZero);
        }

        private string TaoMaBooking()
        {
            // Keep booking code short (<= 20 chars) and unique enough for high traffic bursts.
            return $"BK{DateTime.Now:yyMMddHHmmss}{Random.Shared.Next(100, 999)}";
        }

        private void DanhDauPhieuDaSuDung(DonDatTour don)
        {
            if (!don.IdPhieuGiamGia.HasValue)
            {
                return;
            }

            var suDung = _db.PhieuGiamGiaSuDungs
                .Where(x => x.IdDon == don.IdDon && x.IdPhieuGiamGia == don.IdPhieuGiamGia.Value)
                .OrderByDescending(x => x.IdSuDung)
                .FirstOrDefault();

            if (suDung == null)
            {
                return;
            }

            suDung.TrangThai = VoucherStatusUsed;
            suDung.ThoiDiemSuDung = DateTime.Now;
            suDung.GhiChu = "Thanh toan thanh cong";
        }

        private void HoanLaiPhieuGiamGiaNeuCan(DonDatTour don, string ghiChu)
        {
            if (!don.IdPhieuGiamGia.HasValue)
            {
                return;
            }

            var suDung = _db.PhieuGiamGiaSuDungs
                .Where(x => x.IdDon == don.IdDon && x.IdPhieuGiamGia == don.IdPhieuGiamGia.Value)
                .OrderByDescending(x => x.IdSuDung)
                .FirstOrDefault();

            if (suDung == null)
            {
                return;
            }

            if (suDung.TrangThai == VoucherStatusCancelled || suDung.TrangThai == VoucherStatusUsed)
            {
                return;
            }

            suDung.TrangThai = VoucherStatusCancelled;
            suDung.ThoiDiemSuDung = DateTime.Now;
            suDung.GhiChu = ghiChu;

            var phieu = _db.PhieuGiamGias.FirstOrDefault(x => x.IdPhieuGiamGia == don.IdPhieuGiamGia.Value);
            if (phieu != null && phieu.DaSuDung > 0)
            {
                phieu.DaSuDung -= 1;
                phieu.NgayCapNhat = DateTime.Now;
            }
        }

        private Task GuiMailThanhToanThanhCongAsync(DonDatTour don)
        {
            try
            {
                var toEmail = _db.TaiKhoans
                    .Where(x => x.IdTaiKhoan == don.IdTaiKhoan)
                    .Select(x => x.Email)
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    return Task.CompletedTask;
                }

                var customerName = _db.TaiKhoans
                    .Where(x => x.IdTaiKhoan == don.IdTaiKhoan)
                    .Select(x => x.HoTen)
                    .FirstOrDefault() ?? "ban";

                var paidAmount = TinhTongCanThanhToan(don);
                var departureDate = don.IdLichNavigation?.NgayKhoiHanh.ToDateTime(TimeOnly.MinValue);
                var tourName = don.IdTourNavigation?.TenTour ?? "Tour";

                return _emailService.SendPaymentSuccessEmailAsync(
                    toEmail,
                    customerName,
                    don.MaBooking,
                    tourName,
                    departureDate,
                    paidAmount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Send payment success email failed for order {IdDon}", don.IdDon);
                return Task.CompletedTask;
            }
        }
    }
}
