using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TourWebApp.Data.Models;

[Table("PhieuGiamGiaTaiKhoan")]
[Index("IdTaiKhoan", "IdPhieuGiamGia", Name = "UX_PGGTK_UserVoucher", IsUnique = true)]
[Index("IdTaiKhoan", Name = "IX_PGGTK_User")]
public partial class PhieuGiamGiaTaiKhoan
{
    [Key]
    public int IdLuu { get; set; }

    public int IdTaiKhoan { get; set; }

    public int IdPhieuGiamGia { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayLuu { get; set; }

    public bool TrangThai { get; set; }

    [ForeignKey("IdPhieuGiamGia")]
    [InverseProperty("PhieuGiamGiaTaiKhoans")]
    public virtual PhieuGiamGia IdPhieuGiamGiaNavigation { get; set; } = null!;

    [ForeignKey("IdTaiKhoan")]
    [InverseProperty("PhieuGiamGiaTaiKhoans")]
    public virtual TaiKhoan IdTaiKhoanNavigation { get; set; } = null!;
}
