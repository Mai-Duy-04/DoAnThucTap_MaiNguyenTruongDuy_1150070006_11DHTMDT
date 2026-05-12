namespace TourWebApp.Models.ViewModels;

public class TourCompareVM
{
    public int IdTour { get; set; }
    public string MaTour { get; set; } = string.Empty;
    public string TenTour { get; set; } = string.Empty;
    public string? DiaDiem { get; set; }
    public string? ThoiGian { get; set; }
    public string? PhuongTien { get; set; }
    public decimal? GiaNguoiLon { get; set; }
    public int SoChoConLai { get; set; }
    public string? HinhAnh { get; set; }
    public int LuotXem { get; set; }
}
