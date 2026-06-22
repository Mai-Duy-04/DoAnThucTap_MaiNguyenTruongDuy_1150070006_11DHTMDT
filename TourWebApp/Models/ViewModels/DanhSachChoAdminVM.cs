namespace TourWebApp.Models.ViewModels;

public sealed class DanhSachChoAdminVM
{
    public int IdDanhSachCho { get; set; }
    public int IdLich { get; set; }
    public string TenTour { get; set; } = string.Empty;
    public DateOnly NgayKhoiHanh { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public int SoKhach { get; set; }
    public DateTime NgayDangKy { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}
