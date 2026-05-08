using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TourWebApp.Data.Models;

[Table("TaiKhoan")]
[Index("Email", Name = "UQ__TaiKhoan__A9D1053436F203C5", IsUnique = true)]
public partial class TaiKhoan
{
    [Key]
    public int IdTaiKhoan { get; set; }

    [StringLength(100)]
    public string HoTen { get; set; } = null!;

    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(255)]
    public string MatKhau { get; set; } = null!;

    [StringLength(15)]
    public string? SoDienThoai { get; set; }

    [StringLength(255)]
    public string? DiaChi { get; set; }

    [StringLength(20)]
    public string VaiTro { get; set; } = null!;

    public bool TrangThai { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayTao { get; set; }

    [InverseProperty("IdTaiKhoanNavigation")]
    public virtual ICollection<DonDatTour> DonDatTours { get; set; } = new List<DonDatTour>();

    [InverseProperty("IdTaiKhoanNavigation")]
    public virtual ICollection<PhieuGiamGiaSuDung> PhieuGiamGiaSuDungs { get; set; } = new List<PhieuGiamGiaSuDung>();

    [InverseProperty("IdTaiKhoanNavigation")]
    public virtual ICollection<PhieuGiamGiaTaiKhoan> PhieuGiamGiaTaiKhoans { get; set; } = new List<PhieuGiamGiaTaiKhoan>();

    [InverseProperty("IdTaiKhoanNavigation")]
    public virtual ICollection<WishlistTour> WishlistTours { get; set; } = new List<WishlistTour>();
}
