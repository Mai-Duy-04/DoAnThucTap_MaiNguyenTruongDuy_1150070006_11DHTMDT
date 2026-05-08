using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TourWebApp.Data.Models;

[Table("PhieuGiamGia")]
[Index("MaPhieu", Name = "UX_PhieuGiamGia_Ma", IsUnique = true)]
[Index("TrangThai", Name = "IX_PhieuGiamGia_TrangThai")]
public partial class PhieuGiamGia
{
    [Key]
    public int IdPhieuGiamGia { get; set; }

    [StringLength(50)]
    public string MaPhieu { get; set; } = null!;

    [StringLength(200)]
    public string TenPhieu { get; set; } = null!;

    [StringLength(20)]
    public string LoaiGiam { get; set; } = "PhanTram";

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GiaTriGiam { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal DonToiThieu { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal? GiamToiDa { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayBatDau { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayKetThuc { get; set; }

    public int? TongLuotSuDung { get; set; }

    public int DaSuDung { get; set; }

    public int? LuotToiDaMoiTaiKhoan { get; set; }

    [StringLength(20)]
    public string PhamViApDung { get; set; } = "TatCa";

    public int? IdTour { get; set; }

    public int? IdLoaiTour { get; set; }

    public bool TrangThai { get; set; }

    [StringLength(500)]
    public string? MoTa { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayTao { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? NgayCapNhat { get; set; }

    [ForeignKey("IdTour")]
    [InverseProperty("PhieuGiamGias")]
    public virtual Tour? IdTourNavigation { get; set; }

    [ForeignKey("IdLoaiTour")]
    [InverseProperty("PhieuGiamGias")]
    public virtual LoaiTour? IdLoaiTourNavigation { get; set; }

    [InverseProperty("IdPhieuGiamGiaNavigation")]
    public virtual ICollection<DonDatTour> DonDatTours { get; set; } = new List<DonDatTour>();

    [InverseProperty("IdPhieuGiamGiaNavigation")]
    public virtual ICollection<PhieuGiamGiaSuDung> PhieuGiamGiaSuDungs { get; set; } = new List<PhieuGiamGiaSuDung>();

    [InverseProperty("IdPhieuGiamGiaNavigation")]
    public virtual ICollection<PhieuGiamGiaTaiKhoan> PhieuGiamGiaTaiKhoans { get; set; } = new List<PhieuGiamGiaTaiKhoan>();
}
