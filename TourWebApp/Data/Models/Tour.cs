using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TourWebApp.Data.Models;

[Table("Tour")]
[Index("IdLoaiTour", Name = "IX_Tour_Loai")]
[Index("MaTour", Name = "UX_Tour_Ma", IsUnique = true)]
public partial class Tour
{
    [Key]
    public int IdTour { get; set; }

    [StringLength(50)]
    public string MaTour { get; set; } = null!;

    [StringLength(200)]
    public string TenTour { get; set; } = null!;

    [StringLength(200)]
    public string? DiaDiem { get; set; }

    [StringLength(50)]
    public string? ThoiGian { get; set; }

    [StringLength(100)]
    public string? LichKhoiHanhMoTa { get; set; }

    [StringLength(50)]
    public string? PhuongTien { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal? GiaGoc { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal? GiaKhuyenMai { get; set; }

    public int? PhanTramGiam { get; set; }

    [StringLength(255)]
    public string? HinhAnh { get; set; }

    public string? MoTa { get; set; }

    public int LuotXem { get; set; }

    public int SoNguoiDaDat { get; set; }

    public int IdLoaiTour { get; set; }

    public bool TrangThai { get; set; }

    [InverseProperty("IdTourNavigation")]
    public virtual ICollection<BinhLuanTour> BinhLuanTours { get; set; } = new List<BinhLuanTour>();

    [InverseProperty("IdTourNavigation")]
    public virtual ICollection<DonDatTour> DonDatTours { get; set; } = new List<DonDatTour>();

    [InverseProperty("IdTourNavigation")]
    public virtual ICollection<HinhTour> HinhTours { get; set; } = new List<HinhTour>();

    [ForeignKey("IdLoaiTour")]
    [InverseProperty("Tours")]
    public virtual LoaiTour? IdLoaiTourNavigation { get; set; }


    [InverseProperty("IdTourNavigation")]
    public virtual ICollection<LichKhoiHanh> LichKhoiHanhs { get; set; } = new List<LichKhoiHanh>();

    [InverseProperty("IdTourNavigation")]
    public virtual ICollection<TourGiaChiTiet> TourGiaChiTiets { get; set; } = new List<TourGiaChiTiet>();

    [InverseProperty("IdTourNavigation")]
    public virtual ICollection<PhieuGiamGia> PhieuGiamGias { get; set; } = new List<PhieuGiamGia>();

    [InverseProperty("IdTourNavigation")]
    public virtual ICollection<WishlistTour> WishlistTours { get; set; } = new List<WishlistTour>();
}
