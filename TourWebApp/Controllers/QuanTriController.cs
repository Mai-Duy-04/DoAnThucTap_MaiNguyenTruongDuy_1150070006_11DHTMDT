using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourWebApp.Data.Models;
using TourWebApp.ViewModels;
using TourWebApp.Models.ViewModels;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using iText.Kernel.Colors;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.IO.Font;
using System.Data;

namespace TourWebApp.Controllers
{
    public class QuanTriController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuanTriController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        private bool LaAdmin()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            return vaiTro == "Admin";
        }

        private IActionResult NeuKhongPhaiAdmin()
        {
            
            return RedirectToAction("DangNhap", "TaiKhoan");
        }

        // ========== DASHBOARD ==========
        public IActionResult Dashboard(string period = "month", DateTime? selectedDate = null)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            var now = DateTime.Now;
            period = (period ?? "month").ToLower();

            DateTime currentStart;
            DateTime currentEnd;
            DateTime previousStart;
            DateTime previousEnd;

            var baseDate = selectedDate?.Date ?? now.Date;

            if (period == "day")
            {
                currentStart = baseDate;
                currentEnd = baseDate.AddDays(1);

                previousStart = currentStart.AddDays(-1);
                previousEnd = currentStart;

                ViewBag.FilterTitle = "Ngày " + baseDate.ToString("dd/MM/yyyy");
                ViewBag.SelectedDate = baseDate.ToString("yyyy-MM-dd");
                ViewBag.SelectedDateText = baseDate.ToString("dd/MM/yyyy");
            }
            else if (period == "week")
            {
                currentEnd = now.Date.AddDays(1);
                currentStart = currentEnd.AddDays(-7);

                previousEnd = currentStart;
                previousStart = previousEnd.AddDays(-7);

                ViewBag.FilterTitle = "7 ngày gần nhất";
                ViewBag.SelectedDate = now.ToString("yyyy-MM-dd");
                ViewBag.SelectedDateText = now.ToString("dd/MM/yyyy");
            }
            else if (period == "quarter")
            {
                int currentQuarter = (now.Month - 1) / 3 + 1;
                int firstMonthOfQuarter = (currentQuarter - 1) * 3 + 1;

                currentStart = new DateTime(now.Year, firstMonthOfQuarter, 1);
                currentEnd = currentStart.AddMonths(3);

                previousEnd = currentStart;
                previousStart = previousEnd.AddMonths(-3);

                ViewBag.FilterTitle = $"Quý {currentQuarter} - {now.Year}";
                ViewBag.SelectedDate = now.ToString("yyyy-MM-dd");
                ViewBag.SelectedDateText = now.ToString("dd/MM/yyyy");
            }
            else if (period == "year")
            {
                currentStart = new DateTime(now.Year, 1, 1);
                currentEnd = new DateTime(now.Year + 1, 1, 1);

                previousStart = new DateTime(now.Year - 1, 1, 1);
                previousEnd = currentStart;

                ViewBag.FilterTitle = "Năm " + now.Year;
                ViewBag.SelectedDate = now.ToString("yyyy-MM-dd");
                ViewBag.SelectedDateText = now.ToString("dd/MM/yyyy");
            }
            else
            {
                period = "month";

                currentStart = new DateTime(now.Year, now.Month, 1);
                currentEnd = currentStart.AddMonths(1);

                previousEnd = currentStart;
                previousStart = previousEnd.AddMonths(-1);

                ViewBag.FilterTitle = "Tháng " + now.ToString("MM/yyyy");
                ViewBag.SelectedDate = now.ToString("yyyy-MM-dd");
                ViewBag.SelectedDateText = now.ToString("dd/MM/yyyy");
            }

            ViewBag.Period = period;

            var validOrders = _context.DonDatTours
                .AsNoTracking()
                .Where(d => d.TrangThai != "DaHuy" && d.TrangThai != "Đã hủy");

            var currentOrders = validOrders
                .Where(d => d.NgayDat >= currentStart && d.NgayDat < currentEnd);

            var previousOrders = validOrders
                .Where(d => d.NgayDat >= previousStart && d.NgayDat < previousEnd);

            var currentPaidOrders = currentOrders.Where(d => d.DaThanhToan);
            var previousPaidOrders = previousOrders.Where(d => d.DaThanhToan);

            decimal currentRevenue = currentPaidOrders
                .Sum(d => (decimal?)((d.TongTienSauGiam ?? d.TongTien))) ?? 0m;

            decimal previousRevenue = previousPaidOrders
                .Sum(d => (decimal?)((d.TongTienSauGiam ?? d.TongTien))) ?? 0m;

            int currentOrderCount = currentOrders.Count();
            int previousOrderCount = previousOrders.Count();

            int currentCustomerCount = currentOrders
                .Sum(d => (int?)(d.NguoiLon + d.TreEm + d.TreNho)) ?? 0;

            int previousCustomerCount = previousOrders
                .Sum(d => (int?)(d.NguoiLon + d.TreEm + d.TreNho)) ?? 0;

            ViewBag.TongDoanhThu = currentRevenue;
            ViewBag.TongDon = currentOrderCount;
            ViewBag.TongKhach = currentCustomerCount;
            ViewBag.TourDangBan = _context.Tours.Count(t => t.TrangThai);

            ViewBag.PhanTramDoanhThu = TinhPhanTram(currentRevenue, previousRevenue);
            ViewBag.PhanTramDon = TinhPhanTram(currentOrderCount, previousOrderCount);
            ViewBag.PhanTramKhach = TinhPhanTram(currentCustomerCount, previousCustomerCount);
            ViewBag.PhanTramTour = 5.2m;

            var labelDoanhThu = new List<string>();
            var dataDoanhThu = new List<decimal>();
            var labelKhach = new List<string>();
            var dataKhach = new List<int>();

            if (period == "day")
            {
                for (int h = 0; h < 24; h++)
                {
                    var hourStart = currentStart.AddHours(h);
                    var hourEnd = hourStart.AddHours(1);

                    var ordersInHour = currentOrders
                        .Where(d => d.NgayDat >= hourStart && d.NgayDat < hourEnd);

                    var paidInHour = ordersInHour.Where(d => d.DaThanhToan);

                    labelDoanhThu.Add($"{h:00}:00");
                    dataDoanhThu.Add(paidInHour.Sum(d => (decimal?)((d.TongTienSauGiam ?? d.TongTien))) ?? 0m);

                    labelKhach.Add($"{h:00}:00");
                    dataKhach.Add(ordersInHour.Sum(d => (int?)(d.NguoiLon + d.TreEm + d.TreNho)) ?? 0);
                }
            }
            else if (period == "quarter" || period == "year")
            {
                var monthStart = new DateTime(currentStart.Year, currentStart.Month, 1);

                while (monthStart < currentEnd)
                {
                    var monthEnd = monthStart.AddMonths(1);

                    var ordersInMonth = currentOrders
                        .Where(d => d.NgayDat >= monthStart && d.NgayDat < monthEnd);

                    var paidInMonth = ordersInMonth.Where(d => d.DaThanhToan);

                    labelDoanhThu.Add(monthStart.ToString("MM/yyyy"));
                    dataDoanhThu.Add(paidInMonth.Sum(d => (decimal?)((d.TongTienSauGiam ?? d.TongTien))) ?? 0m);

                    labelKhach.Add(monthStart.ToString("MM/yyyy"));
                    dataKhach.Add(ordersInMonth.Sum(d => (int?)(d.NguoiLon + d.TreEm + d.TreNho)) ?? 0);

                    monthStart = monthEnd;
                }
            }
            else
            {
                var dayStart = currentStart.Date;

                while (dayStart < currentEnd)
                {
                    var dayEnd = dayStart.AddDays(1);

                    var ordersInDay = currentOrders
                        .Where(d => d.NgayDat >= dayStart && d.NgayDat < dayEnd);

                    var paidInDay = ordersInDay.Where(d => d.DaThanhToan);

                    labelDoanhThu.Add(dayStart.ToString("dd/MM"));
                    dataDoanhThu.Add(paidInDay.Sum(d => (decimal?)((d.TongTienSauGiam ?? d.TongTien))) ?? 0m);

                    labelKhach.Add(dayStart.ToString("dd/MM"));
                    dataKhach.Add(ordersInDay.Sum(d => (int?)(d.NguoiLon + d.TreEm + d.TreNho)) ?? 0);

                    dayStart = dayEnd;
                }
            }

            ViewBag.LabelDoanhThu = labelDoanhThu;
            ViewBag.DataDoanhThu = dataDoanhThu;
            ViewBag.LabelKhach = labelKhach;
            ViewBag.DataKhach = dataKhach;

            ViewBag.TopTour = _context.Tours
                .AsNoTracking()
                .Include(t => t.HinhTours)
                .Where(t => t.TrangThai)
                .OrderByDescending(t => t.SoNguoiDaDat)
                .ThenByDescending(t => t.LuotXem)
                .Take(3)
                .ToList();

            return View();
        }

        private decimal TinhPhanTram(decimal hienTai, decimal kyTruoc)
        {
            if (kyTruoc == 0)
            {
                return hienTai > 0 ? 100 : 0;
            }

            return Math.Round(((hienTai - kyTruoc) / kyTruoc) * 100, 1);
        }

        private decimal TinhPhanTram(int hienTai, int kyTruoc)
        {
            if (kyTruoc == 0)
            {
                return hienTai > 0 ? 100 : 0;
            }

            return Math.Round(((decimal)(hienTai - kyTruoc) / kyTruoc) * 100, 1);
        }

        // ========== DANH SÁCH TOUR ==========
        [HttpGet]
        public IActionResult QuanLyTour(int? idLoaiTour, string? tuKhoa)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            
            var query = _context.Tours
                                .Include(t => t.IdLoaiTourNavigation)
                                .AsQueryable();

            if (idLoaiTour.HasValue && idLoaiTour > 0)
            {
                query = query.Where(t => t.IdLoaiTour == idLoaiTour);
            }

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                tuKhoa = tuKhoa.Trim();
                query = query.Where(t =>
                    t.TenTour.Contains(tuKhoa) ||
                    (t.DiaDiem ?? "").Contains(tuKhoa));
            }

            var vm = new QuanLyTourViewModel
            {
                Tours = query
                    .OrderByDescending(t => t.IdTour)
                    .ToList(),

                LoaiTours = _context.LoaiTours
                    .OrderBy(x => x.TenLoai)
                    .ToList(),

                IdLoaiTour = idLoaiTour,
                TuKhoa = tuKhoa
            };

            return View(vm);
        }

       // ========== FORM THÊM TOUR ==========
        // GET
        [HttpGet]
        public IActionResult ThemTour()
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            ViewBag.LoaiTours = _context.LoaiTours.OrderBy(x => x.TenLoai).ToList();
            return View(new Tour());
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("ThemTour")]
        public IActionResult ThemTour(Tour model, IFormFile? HinhAnhFile)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

         
            if (model.IdLoaiTour == 0)
                ModelState.AddModelError("IdLoaiTour", "Vui lòng chọn loại tour");

            
            if (string.IsNullOrWhiteSpace(model.PhuongTien))
                ModelState.AddModelError("PhuongTien", "Vui lòng chọn phương tiện");

            
            bool maTrung = _context.Tours.Any(t => t.MaTour == model.MaTour);
            if (maTrung)
                ModelState.AddModelError("MaTour", "Mã tour đã tồn tại");

            if (!ModelState.IsValid)
            {
                ViewBag.LoaiTours = _context.LoaiTours.ToList();
                return View(model);
            }

            model.LuotXem = 0;
            model.SoNguoiDaDat = 0;
            model.TrangThai = true;

          
            if (model.PhanTramGiam == null || model.PhanTramGiam == 0)
                model.GiaKhuyenMai = model.GiaGoc;
            else
                model.GiaKhuyenMai = model.GiaGoc - (model.GiaGoc * model.PhanTramGiam / 100);

           
            string? uploadedImagePath = null;
            if (HinhAnhFile != null && HinhAnhFile.Length > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(HinhAnhFile.FileName);
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/tours", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    HinhAnhFile.CopyTo(stream);
                }

                model.HinhAnh = fileName;
                uploadedImagePath = path;
            }

            decimal giaNguoiLon = model.GiaKhuyenMai ?? 0;
            decimal giaTreEm = giaNguoiLon * 0.5m; 
            decimal giaEmBe = 0;

            model.TourGiaChiTiets = new List<TourGiaChiTiet>
            {
                new TourGiaChiTiet {
                    DoiTuong = TourPriceAudience.Adult,
                    Gia = giaNguoiLon,
                    GhiChu = "Tự động tạo"
                },
                new TourGiaChiTiet {
                    DoiTuong = TourPriceAudience.Child,
                    Gia = giaTreEm,
                    GhiChu = "50% giá người lớn"
                },
                new TourGiaChiTiet {
                    DoiTuong = TourPriceAudience.Infant,
                    Gia = giaEmBe,
                    GhiChu = "Miễn phí"
                }
            };

            _context.Tours.Add(model);
            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                if (!string.IsNullOrWhiteSpace(uploadedImagePath) && System.IO.File.Exists(uploadedImagePath))
                {
                    System.IO.File.Delete(uploadedImagePath);
                }

                ModelState.AddModelError(string.Empty, "Không thể lưu tour và bảng giá. Vui lòng kiểm tra dữ liệu rồi thử lại.");
                ViewBag.LoaiTours = _context.LoaiTours.OrderBy(x => x.TenLoai).ToList();
                return View(model);
            }

            TempData["ThongBao"] = "✅ Đã thêm tour mới thành công";
            return RedirectToAction("QuanLyTour");
        }

        // ========== FORM SỬA TOUR ==========
        [HttpGet]
        public IActionResult SuaTour(int id)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            var tour = _context.Tours
                .Include(t => t.HinhTours)
                .Include(t => t.TourGiaChiTiets)
                .FirstOrDefault(t => t.IdTour == id);

            if (tour == null) return NotFound();

            
            ViewBag.LoaiTours = _context.LoaiTours
                                        .OrderBy(x => x.TenLoai)
                                        .ToList();

           
            ViewBag.GiaNguoiLon = tour.TourGiaChiTiets
                .FirstOrDefault(g => TourPriceAudience.IsAdult(g.DoiTuong))?.Gia ?? 0;

            
            ViewBag.GiaTreEm = tour.TourGiaChiTiets
                .FirstOrDefault(g => TourPriceAudience.IsChild(g.DoiTuong))?.Gia ?? 0;

            
            ViewBag.GiaEmBe = tour.TourGiaChiTiets
                .FirstOrDefault(g => TourPriceAudience.IsInfant(g.DoiTuong))?.Gia ?? 0;

            return View(tour);
        }

        public IActionResult ChiTietTour(int id)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            var tour = _context.Tours
                .Include(t => t.IdLoaiTourNavigation)
                .Include(t => t.HinhTours)
                .Include(t => t.LichKhoiHanhs)
                .FirstOrDefault(t => t.IdTour == id);

            if (tour == null) return NotFound();

            return View(tour);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SuaTour(
            Tour model,
            List<IFormFile> HinhAnhFiles,
            decimal? GiaNguoiLon,
            decimal? GiaTreEm,
            decimal? GiaEmBe)

        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            var tour = _context.Tours.Find(model.IdTour);
            if (tour == null) return NotFound();

            if (!GiaNguoiLon.HasValue || GiaNguoiLon.Value < 0)
                ModelState.AddModelError("GiaNguoiLon", "Giá người lớn không hợp lệ");

            if (!GiaTreEm.HasValue || GiaTreEm.Value < 0)
                ModelState.AddModelError("GiaTreEm", "Giá trẻ em không hợp lệ");

            if (!GiaEmBe.HasValue || GiaEmBe.Value < 0)
                ModelState.AddModelError("GiaEmBe", "Giá em bé không hợp lệ");

            if (!ModelState.IsValid)
            {
                _context.Entry(tour).Collection(x => x.HinhTours).Load();
                model.HinhTours = tour.HinhTours;
                ViewBag.LoaiTours = _context.LoaiTours.OrderBy(x => x.TenLoai).ToList();
                ViewBag.GiaNguoiLon = GiaNguoiLon ?? 0;
                ViewBag.GiaTreEm = GiaTreEm ?? 0;
                ViewBag.GiaEmBe = GiaEmBe ?? 0;
                return View(model);
            }

            tour.TenTour = model.TenTour;
            tour.DiaDiem = model.DiaDiem;
            tour.ThoiGian = model.ThoiGian;
            tour.PhuongTien = model.PhuongTien;
            tour.GiaGoc = model.GiaGoc;
            tour.PhanTramGiam = model.PhanTramGiam;
            tour.IdLoaiTour = model.IdLoaiTour;
            tour.MoTa = model.MoTa;
            tour.TrangThai = model.TrangThai;

          
            if (model.PhanTramGiam == null || model.PhanTramGiam == 0)
                tour.GiaKhuyenMai = model.GiaGoc;
            else
                tour.GiaKhuyenMai = model.GiaGoc - (model.GiaGoc * model.PhanTramGiam / 100);

            
            var uploadedImagePaths = new List<string>();
            if (HinhAnhFiles != null && HinhAnhFiles.Count > 0)
            {
                foreach (var file in HinhAnhFiles)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/tours", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    uploadedImagePaths.Add(path);

                    _context.HinhTours.Add(new HinhTour
                    {
                        IdTour = tour.IdTour,
                        UrlHinh = fileName,
                        ThuTu = 0
                    });
                }
            }

            var thumbnail = Request.Form["Thumbnail"];
            if (!string.IsNullOrEmpty(thumbnail))
            {
                tour.HinhAnh = thumbnail;
            }
          
            var giaNguoiLon = GiaNguoiLon.GetValueOrDefault();
            var giaTreEm = GiaTreEm.GetValueOrDefault();
            var giaEmBe = GiaEmBe.GetValueOrDefault();

            
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var oldPrices = _context.TourGiaChiTiets
                    .Where(x => x.IdTour == tour.IdTour)
                    .ToList();

                _context.TourGiaChiTiets.RemoveRange(oldPrices);
                _context.SaveChanges();

                var newPriceList = new List<TourGiaChiTiet>
                {
                    new()
                    {
                        IdTour = tour.IdTour,
                        DoiTuong = TourPriceAudience.Adult,
                        Gia = giaNguoiLon,
                        GhiChu = "Giá cố định"
                    },
                    new()
                    {
                        IdTour = tour.IdTour,
                        DoiTuong = TourPriceAudience.Child,
                        Gia = giaTreEm,
                        GhiChu = "Giá cố định"
                    },
                    new()
                    {
                        IdTour = tour.IdTour,
                        DoiTuong = TourPriceAudience.Infant,
                        Gia = giaEmBe,
                        GhiChu = "Giá cố định"
                    }
                };

                _context.TourGiaChiTiets.AddRange(newPriceList);
                _context.SaveChanges();
                transaction.Commit();
            }
            catch (DbUpdateException)
            {
                transaction.Rollback();
                foreach (var path in uploadedImagePaths.Where(System.IO.File.Exists))
                {
                    System.IO.File.Delete(path);
                }

                _context.ChangeTracker.Clear();
                var currentTour = _context.Tours
                    .Include(x => x.HinhTours)
                    .Include(x => x.TourGiaChiTiets)
                    .FirstOrDefault(x => x.IdTour == model.IdTour);

                ModelState.AddModelError(string.Empty, "Không thể cập nhật tour và bảng giá. Vui lòng kiểm tra dữ liệu rồi thử lại.");
                ViewBag.LoaiTours = _context.LoaiTours.OrderBy(x => x.TenLoai).ToList();
                ViewBag.GiaNguoiLon = currentTour?.TourGiaChiTiets.FirstOrDefault(x => TourPriceAudience.IsAdult(x.DoiTuong))?.Gia ?? giaNguoiLon;
                ViewBag.GiaTreEm = currentTour?.TourGiaChiTiets.FirstOrDefault(x => TourPriceAudience.IsChild(x.DoiTuong))?.Gia ?? giaTreEm;
                ViewBag.GiaEmBe = currentTour?.TourGiaChiTiets.FirstOrDefault(x => TourPriceAudience.IsInfant(x.DoiTuong))?.Gia ?? giaEmBe;
                return View(currentTour ?? model);
            }

            TempData["ThongBao"] = "✅ Đã cập nhật tour";
            return RedirectToAction("QuanLyTour");
        }


      // NGƯNG BÁN
        [HttpPost]
        public IActionResult NgungBanTour(int id)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            var tour = _context.Tours.FirstOrDefault(t => t.IdTour == id);
            if (tour == null) return NotFound();

            tour.TrangThai = false;
            _context.SaveChanges();

            TempData["ThongBao"] = "🚫 Đã ngưng bán tour";
            return RedirectToAction("QuanLyTour");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaTour(int id)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            var tour = _context.Tours
                .Include(t => t.HinhTours)
                .Include(t => t.TourGiaChiTiets)
                .Include(t => t.LichKhoiHanhs)
                .FirstOrDefault(t => t.IdTour == id);

            if (tour == null) return NotFound();

            _context.HinhTours.RemoveRange(tour.HinhTours);
            _context.TourGiaChiTiets.RemoveRange(tour.TourGiaChiTiets);
            _context.LichKhoiHanhs.RemoveRange(tour.LichKhoiHanhs);

            _context.Tours.Remove(tour);
            _context.SaveChanges();

            TempData["ThongBao"] = "✅ Đã xóa tour";
            return RedirectToAction("QuanLyTour");
        }

        // MỞ BÁN LẠI
       [HttpPost]
        public IActionResult MoLaiTour(int id)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            var tour = _context.Tours.FirstOrDefault(t => t.IdTour == id);
            if (tour == null) return NotFound();

            tour.TrangThai = true;
            _context.SaveChanges();

            TempData["ThongBao"] = "✅ Đã mở bán lại tour";
            return RedirectToAction("QuanLyTour");
        }
       
        // FORM THÊM LỊCH
        [HttpGet]
        public IActionResult ThemLich(int idTour)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            ViewBag.IdTour = idTour;
            return View();
        }

       [HttpPost]
        public IActionResult ThemLich(LichKhoiHanh model)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            
            if (model.IdTour == 0)
            {
                TempData["Error"] = "Vui lòng chọn tour trước khi thêm lịch!";
                return RedirectToAction("DanhSachLich");
            }

            model.SoChoConLai = model.SoChoToiDa;
            model.TrangThai = model.SoChoConLai > 0 ? "Còn chỗ" : "Hết chỗ";

            _context.LichKhoiHanhs.Add(model);
            _context.SaveChanges();

            return RedirectToAction("DanhSachLich");
        }

        // ================= SỬA LỊCH KHỞI HÀNH =================
        [HttpPost]
        public IActionResult SuaLich(LichKhoiHanh model)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            
            var lich = _context.LichKhoiHanhs
                .FirstOrDefault(l => l.IdLich == model.IdLich);

            if (lich == null)
            {
                TempData["Error"] = "Không tìm thấy lịch khởi hành!";
                return RedirectToAction("DanhSachLich");
            }

            
            bool coDon = _context.DonDatTours
                .Any(d => d.IdLich == lich.IdLich && d.TrangThai != "DaHuy");

            if (coDon)
            {
                TempData["Error"] = "Lịch đã có người đặt, không thể sửa!";
                return RedirectToAction("DanhSachLich", new { idTour = lich.IdTour });
            }

           
            lich.NgayKhoiHanh = model.NgayKhoiHanh;
            lich.GioKhoiHanh  = model.GioKhoiHanh;
            lich.SoChoToiDa   = model.SoChoToiDa;

            
            int soDaDat = lich.SoChoToiDa - lich.SoChoConLai;   // số đã đặt cũ
            if (soDaDat < 0) soDaDat = 0;

            lich.SoChoConLai = Math.Max(0, model.SoChoToiDa - soDaDat);
            lich.TrangThai   = lich.SoChoConLai > 0 ? "Còn chỗ" : "Hết chỗ";

            _context.SaveChanges();

            TempData["ThongBao"] = "✅ Đã cập nhật lịch khởi hành!";
            return RedirectToAction("DanhSachLich", new { idTour = lich.IdTour });
        }


        // XÓA LỊCH (chỉ khi chưa có đơn)
        [HttpPost]
        public IActionResult XoaLich(int id)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            var lich = _context.LichKhoiHanhs.FirstOrDefault(l => l.IdLich == id);
            if (lich == null) return NotFound();

            bool coDon = _context.DonDatTours.Any(d => d.IdLich == id && d.TrangThai != "DaHuy");

            if (coDon)
            {
                TempData["Error"] = "❌ Không thể xóa lịch đã có người đặt!";
                return RedirectToAction("DanhSachLich", new { idTour = lich.IdTour });
            }

            _context.LichKhoiHanhs.Remove(lich);
            _context.SaveChanges();

            TempData["ThongBao"] = "✅ Đã xóa lịch thành công";

            
            return RedirectToAction("DanhSachLich", new { idTour = lich.IdTour });
        }

        // ================= DANH SÁCH TẤT CẢ LỊCH (sidebar) =================
        public IActionResult DanhSachLich(int? idTour)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            var query = _context.LichKhoiHanhs
            .Include(l => l.IdTourNavigation)
            .Include(l => l.DonDatTours)   // ✅ BẮT BUỘC PHẢI CÓ DÒNG NÀY
            .AsQueryable();


            if (idTour != null)
            {
                query = query.Where(l => l.IdTour == idTour);
            }

            ViewBag.Tours = _context.Tours.ToList();
            ViewBag.IdTour = idTour;
            ViewBag.WaitlistCounts = LaySoLuongDanhSachCho();

            return View(query.OrderByDescending(l => l.NgayKhoiHanh).ToList());
        }

        public IActionResult DanhSachCho(int? idLich)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            var result = new List<DanhSachChoAdminVM>();
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
IF OBJECT_ID(N'dbo.DanhSachCho', N'U') IS NOT NULL
BEGIN
    SELECT ds.IdDanhSachCho, ds.IdLich, t.TenTour, l.NgayKhoiHanh,
           tk.HoTen, tk.Email, tk.SoDienThoai, ds.SoKhach, ds.NgayDangKy, ds.TrangThai
    FROM dbo.DanhSachCho ds
    INNER JOIN dbo.LichKhoiHanh l ON l.IdLich = ds.IdLich
    INNER JOIN dbo.Tour t ON t.IdTour = l.IdTour
    INNER JOIN dbo.TaiKhoan tk ON tk.IdTaiKhoan = ds.IdTaiKhoan
    WHERE (@IdLich IS NULL OR ds.IdLich = @IdLich)
    ORDER BY CASE WHEN ds.TrangThai = N'Đang chờ' THEN 0 ELSE 1 END, ds.NgayDangKy;
END";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@IdLich";
            parameter.Value = idLich.HasValue ? idLich.Value : DBNull.Value;
            command.Parameters.Add(parameter);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new DanhSachChoAdminVM
                {
                    IdDanhSachCho = reader.GetInt32(0),
                    IdLich = reader.GetInt32(1),
                    TenTour = reader.GetString(2),
                    NgayKhoiHanh = DateOnly.FromDateTime(reader.GetDateTime(3)),
                    HoTen = reader.GetString(4),
                    Email = reader.GetString(5),
                    SoDienThoai = reader.IsDBNull(6) ? null : reader.GetString(6),
                    SoKhach = reader.GetInt32(7),
                    NgayDangKy = reader.GetDateTime(8),
                    TrangThai = reader.GetString(9)
                });
            }

            ViewBag.IdLich = idLich;
            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CapNhatDanhSachCho(int id, string trangThai)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            var allowed = new[] { "Đang chờ", "Đã thông báo", "Đã liên hệ", "Đã đặt", "Đã hủy" };
            if (!allowed.Contains(trangThai)) return BadRequest();

            _context.Database.ExecuteSqlInterpolated($@"
IF OBJECT_ID(N'dbo.DanhSachCho', N'U') IS NOT NULL
    UPDATE dbo.DanhSachCho SET TrangThai = {trangThai} WHERE IdDanhSachCho = {id};");

            TempData["ThongBao"] = "Đã cập nhật trạng thái danh sách chờ.";
            return RedirectToAction(nameof(DanhSachCho));
        }

        private Dictionary<int, int> LaySoLuongDanhSachCho()
        {
            var result = new Dictionary<int, int>();
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
IF OBJECT_ID(N'dbo.DanhSachCho', N'U') IS NOT NULL
    SELECT IdLich, COUNT(*) FROM dbo.DanhSachCho
    WHERE TrangThai IN (N'Đang chờ', N'Đã thông báo', N'Đã liên hệ') GROUP BY IdLich;";
            using var reader = command.ExecuteReader();
            while (reader.Read()) result[reader.GetInt32(0)] = reader.GetInt32(1);
            return result;
        }

        [HttpGet]
        public IActionResult ExportHoaDon(int idDon)
        {
            if (!LaAdmin()) return NeuKhongPhaiAdmin();

            var don = _context.DonDatTours
                .Include(x => x.IdTaiKhoanNavigation)
                .Include(x => x.IdTourNavigation)
                .Include(x => x.IdLichNavigation)
                .FirstOrDefault(x => x.IdDon == idDon);

            if (don == null) return NotFound();

            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4);

                // Lề cho gọn
                doc.SetMargins(30, 30, 30, 30);

               // FONT TIẾNG VIỆT
                var fontPathRegular = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts/Roboto-Regular.ttf");
                var fontPathBold    = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/fonts/Roboto-Bold.ttf");

                var fontNormal = PdfFontFactory.CreateFont(fontPathRegular, PdfEncodings.IDENTITY_H);
                var fontBold   = PdfFontFactory.CreateFont(fontPathBold,    PdfEncodings.IDENTITY_H);


                doc.SetFont(fontNormal);

                // ======== MÀU CHỦ ĐẠO ========
                Color primary   = new DeviceRgb(13, 110, 253);
                Color darkText  = new DeviceRgb(33, 37, 41);
                Color lightGray = new DeviceRgb(248, 249, 250);

                // ======== KHUNG NGOÀI ========
                var outerTable = new Table(1).UseAllAvailableWidth();
                outerTable.SetBorder(new SolidBorder(primary, 2));
                outerTable.SetBackgroundColor(ColorConstants.WHITE);
                outerTable.SetPadding(15);

                // ================== HEADER (LOGO + MÃ BOOKING) ==================
                var headerTable = new Table(new float[] { 3, 2 }).UseAllAvailableWidth();

                // Logo (nếu có file)
                var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "logo-happytrip.png");
                Cell cellLogo;
                if (System.IO.File.Exists(logoPath))
                {
                    var imgData = ImageDataFactory.Create(logoPath);
                    var img = new Image(imgData).SetMaxHeight(50).SetAutoScale(true);
                    cellLogo = new Cell().Add(img)
                                        .SetBorder(Border.NO_BORDER)
                                        .SetVerticalAlignment(iText.Layout.Properties.VerticalAlignment.MIDDLE);
                }
                else
                {
                    // fallback chỉ hiện chữ
                    cellLogo = new Cell()
                        .Add(new Paragraph("HappyTrip Travel")
                            .SetFont(fontBold)
                            .SetFontSize(18)
                            .SetFontColor(primary))
                        .SetBorder(Border.NO_BORDER)
                        .SetVerticalAlignment(iText.Layout.Properties.VerticalAlignment.MIDDLE);
                }

                headerTable.AddCell(cellLogo);

                // Mã booking + tiêu đề
                var rightHeader = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.RIGHT);

                rightHeader.Add(new Paragraph("HÓA ĐƠN ĐẶT TOUR")
                    .SetFont(fontBold)
                    .SetFontSize(18)
                    .SetFontColor(darkText));

                rightHeader.Add(new Paragraph($"Mã Booking: {don.MaBooking}")
                    .SetFont(fontBold)
                    .SetFontSize(12)
                    .SetFontColor(primary));

                rightHeader.Add(new Paragraph($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .SetFontSize(9)
                    .SetFontColor(ColorConstants.GRAY));

                headerTable.AddCell(rightHeader);

                outerTable.AddCell(new Cell().Add(headerTable)
                                            .SetBorder(Border.NO_BORDER));

                // ================== THÔNG TIN KHÁCH + TRẠNG THÁI ==================
                var infoTop = new Table(new float[] { 3, 2 }).UseAllAvailableWidth();
                infoTop.SetMarginTop(10);

                // Cột khách hàng
                var cellCustomer = new Cell().SetBorder(Border.NO_BORDER);
                cellCustomer.Add(new Paragraph("THÔNG TIN KHÁCH HÀNG")
                    .SetFont(fontBold)
                    .SetFontSize(11)
                    .SetFontColor(primary));

                var kh = don.IdTaiKhoanNavigation;
                cellCustomer.Add(new Paragraph($"{kh.HoTen}")
                    .SetFont(fontBold)
                    .SetFontSize(11));
                cellCustomer.Add(new Paragraph($"Email: {kh.Email}").SetFontSize(10));
                cellCustomer.Add(new Paragraph($"Số điện thoại: {kh.SoDienThoai}").SetFontSize(10));

                infoTop.AddCell(cellCustomer);

                // Cột trạng thái + tổng tiền mini
                var cellStatus = new Cell()
                    .SetBorder(Border.NO_BORDER)
                    .SetTextAlignment(TextAlignment.RIGHT);

                string labelTrangThai = don.TrangThai;
                Color statusColor = primary;
                if (don.TrangThai == "Đã hủy")
                    statusColor = ColorConstants.RED;
                else if (don.TrangThai == "Chờ duyệt")
                    statusColor = ColorConstants.ORANGE;

                cellStatus.Add(new Paragraph("TRẠNG THÁI ĐƠN")
                    .SetFont(fontBold)
                    .SetFontSize(10)
                    .SetFontColor(ColorConstants.GRAY));

                cellStatus.Add(new Paragraph(labelTrangThai)
                    .SetFont(fontBold)
                    .SetFontSize(12)
                    .SetFontColor(statusColor));

                cellStatus.Add(new Paragraph("\nTỔNG TIỀN")
                    .SetFont(fontBold)
                    .SetFontSize(10)
                    .SetFontColor(ColorConstants.GRAY));

                cellStatus.Add(new Paragraph($"{don.TongTien:N0} đ")
                    .SetFont(fontBold)
                    .SetFontSize(14)
                    .SetFontColor(darkText));

                infoTop.AddCell(cellStatus);

                outerTable.AddCell(new Cell().Add(infoTop)
                                            .SetBorder(Border.NO_BORDER));

                // ================== BLOCK THÔNG TIN TOUR ==================
                var tourBlock = new Table(1).UseAllAvailableWidth();
                tourBlock.SetMarginTop(15);

                // tiêu đề block
                tourBlock.AddCell(
                    new Cell()
                        .Add(new Paragraph("THÔNG TIN TOUR")
                            .SetFont(fontBold)
                            .SetFontSize(11)
                            .SetFontColor(ColorConstants.WHITE))
                        .SetBackgroundColor(primary)
                        .SetBorder(Border.NO_BORDER)
                        .SetPadding(6)
                );

                var tourInner = new Table(new float[] { 1, 2 }).UseAllAvailableWidth();
                tourInner.SetBackgroundColor(lightGray);
                tourInner.SetPadding(8);

                void AddRow(string label, string value)
                {
                    tourInner.AddCell(
                        new Cell()
                            .Add(new Paragraph(label).SetFontSize(10).SetFont(fontBold))
                            .SetBorder(Border.NO_BORDER)
                    );
                    tourInner.AddCell(
                        new Cell()
                            .Add(new Paragraph(value).SetFontSize(10))
                            .SetBorder(Border.NO_BORDER)
                    );
                }

                AddRow("Tour:", don.IdTourNavigation.TenTour);
                AddRow("Khởi hành:",
                    $"{don.IdLichNavigation.NgayKhoiHanh:dd/MM/yyyy} - {don.IdLichNavigation.GioKhoiHanh:hh\\:mm}");
                AddRow("Người lớn:", don.NguoiLon.ToString());
                AddRow("Trẻ em:", don.TreEm.ToString());
                AddRow("Trẻ nhỏ:", don.TreNho.ToString());

                tourBlock.AddCell(new Cell().Add(tourInner).SetBorder(Border.NO_BORDER));

                outerTable.AddCell(new Cell().Add(tourBlock).SetBorder(Border.NO_BORDER));

                // ================== BLOCK THANH TOÁN ==================
                var payBlock = new Table(1).UseAllAvailableWidth();
                payBlock.SetMarginTop(15);

                payBlock.AddCell(
                    new Cell()
                        .Add(new Paragraph("THANH TOÁN")
                            .SetFont(fontBold)
                            .SetFontSize(11)
                            .SetFontColor(ColorConstants.WHITE))
                        .SetBackgroundColor(primary)
                        .SetBorder(Border.NO_BORDER)
                        .SetPadding(6)
                );

                var payInner = new Table(new float[] { 1, 2 }).UseAllAvailableWidth();
                payInner.SetBackgroundColor(lightGray);
                payInner.SetPadding(8);

                AddRowPayment("Tổng tiền:", $"{don.TongTien:N0} đ");
                AddRowPayment("Trạng thái thanh toán:", don.TrangThai);

                void AddRowPayment(string label, string value)
                {
                    payInner.AddCell(
                        new Cell()
                            .Add(new Paragraph(label).SetFontSize(10).SetFont(fontBold))
                            .SetBorder(Border.NO_BORDER)
                    );
                    payInner.AddCell(
                        new Cell()
                            .Add(new Paragraph(value).SetFontSize(10))
                            .SetBorder(Border.NO_BORDER)
                    );
                }

                payBlock.AddCell(new Cell().Add(payInner).SetBorder(Border.NO_BORDER));

                outerTable.AddCell(new Cell().Add(payBlock).SetBorder(Border.NO_BORDER));

                // ================== FOOTER ==================
                var footer = new Paragraph("Cảm ơn bạn đã sử dụng dịch vụ HappyTrip!\nHotline hỗ trợ: 1900 1234 • Website: happytrip.vn")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(9)
                    .SetFontColor(ColorConstants.GRAY)
                    .SetMarginTop(20);

                outerTable.AddCell(new Cell().Add(footer)
                                            .SetBorder(Border.NO_BORDER));

                // Thêm khung ngoài vào document
                doc.Add(outerTable);

                doc.Close();

                return File(
                    stream.ToArray(),
                    "application/pdf",
                    $"HoaDon_{don.MaBooking}.pdf"
                );
            }
        }

    }
}
