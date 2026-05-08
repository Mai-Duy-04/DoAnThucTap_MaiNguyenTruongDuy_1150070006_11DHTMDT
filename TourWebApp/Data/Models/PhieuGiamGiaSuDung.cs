using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourWebApp.Data.Models;

[Table("PhieuGiamGiaSuDung")]
public partial class PhieuGiamGiaSuDung
{
    [Key]
    public int IdSuDung { get; set; }

    public int IdPhieuGiamGia { get; set; }

    public int IdDon { get; set; }

    public int IdTaiKhoan { get; set; }

    [StringLength(50)]
    public string MaPhieu { get; set; } = null!;

    [Column(TypeName = "decimal(18, 0)")]
    public decimal SoTienGiam { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime ThoiDiemSuDung { get; set; }

    [StringLength(20)]
    public string TrangThai { get; set; } = "GiuCho";

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [ForeignKey("IdPhieuGiamGia")]
    [InverseProperty("PhieuGiamGiaSuDungs")]
    public virtual PhieuGiamGia IdPhieuGiamGiaNavigation { get; set; } = null!;

    [ForeignKey("IdDon")]
    [InverseProperty("PhieuGiamGiaSuDungs")]
    public virtual DonDatTour IdDonNavigation { get; set; } = null!;

    [ForeignKey("IdTaiKhoan")]
    [InverseProperty("PhieuGiamGiaSuDungs")]
    public virtual TaiKhoan IdTaiKhoanNavigation { get; set; } = null!;
}
