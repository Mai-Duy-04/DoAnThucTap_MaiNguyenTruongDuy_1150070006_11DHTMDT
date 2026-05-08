using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TourWebApp.Data.Models;

[Table("LoaiTour")]
public partial class LoaiTour
{
    [Key]
    public int IdLoaiTour { get; set; }

    [StringLength(100)]
    public string TenLoai { get; set; } = null!;

    public string? MoTa { get; set; }

    [InverseProperty("IdLoaiTourNavigation")]
    public virtual ICollection<Tour> Tours { get; set; } = new List<Tour>();

    [InverseProperty("IdLoaiTourNavigation")]
    public virtual ICollection<PhieuGiamGia> PhieuGiamGias { get; set; } = new List<PhieuGiamGia>();
}
