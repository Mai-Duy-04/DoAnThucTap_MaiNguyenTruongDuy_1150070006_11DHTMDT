using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TourWebApp.Data.Models;

[Table("WishlistTour")]
[Index(nameof(IdTaiKhoan), nameof(IdTour), Name = "UX_WishlistTour_UserTour", IsUnique = true)]
public class WishlistTour
{
    [Key]
    public int IdWishlist { get; set; }

    public int IdTaiKhoan { get; set; }

    public int IdTour { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime NgayTao { get; set; }

    [ForeignKey(nameof(IdTaiKhoan))]
    public virtual TaiKhoan IdTaiKhoanNavigation { get; set; } = null!;

    [ForeignKey(nameof(IdTour))]
    public virtual Tour IdTourNavigation { get; set; } = null!;
}