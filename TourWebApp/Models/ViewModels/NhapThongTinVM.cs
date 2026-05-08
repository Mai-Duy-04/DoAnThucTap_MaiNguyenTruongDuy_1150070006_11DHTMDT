namespace TourWebApp.Models.ViewModels
{
    public class NhapThongTinVM
    {
        public const string PaymentMethodVnpay = "VNPAY";
        public const string PaymentMethodCash = "TienMat";

        public int IdTour { get; set; }
        public int IdLich { get; set; }

        public string TenTour { get; set; } = string.Empty;
        public DateTime NgayKhoiHanh { get; set; }

        // So luong khach
        public int NguoiLon { get; set; }
        public int TreEm { get; set; }
        public int EmBe { get; set; }

        // Gia (chi de hien thi tren form, khong tin client)
        public decimal GiaNguoiLon { get; set; }
        public decimal GiaTreEm { get; set; }
        public decimal GiaEmBe { get; set; }

        // Tong tien
        public decimal TongTien { get; set; }
        public decimal SoTienGiam { get; set; }
        public decimal TongTienSauGiam { get; set; }
        public string? MaPhieuGiamGia { get; set; }

        public int TongKhach => NguoiLon + TreEm + EmBe;

        // Thong tin lien he
        public string HoTen { get; set; } = string.Empty;
        public string SDT { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string GhiChu { get; set; } = string.Empty;

        public string PhuongThucThanhToan { get; set; } = PaymentMethodVnpay;
    }
}
