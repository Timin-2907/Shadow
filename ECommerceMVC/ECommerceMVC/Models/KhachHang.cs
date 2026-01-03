using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace ECommerceMVC.Models
{
    public partial class KhachHang
    {
        [Key]
        public string MaKh { get; set; } = null!;

        public string? MatKhau { get; set; }

        [Required]
        public string HoTen { get; set; } = null!;

        public bool GioiTinh { get; set; }

        public DateTime NgaySinh { get; set; }

        public string? DiaChi { get; set; }

        public string? DienThoai { get; set; }

        [Required]
        public string Email { get; set; } = null!;

        public string? Hinh { get; set; }

        public bool HieuLuc { get; set; }

        public int VaiTro { get; set; }

        public string? RandomKey { get; set; }

        public bool DangKyNhanTin { get; set; }

        public string? NewsletterToken { get; set; }

        // Role properties
        public int? RoleId { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }
    }
}