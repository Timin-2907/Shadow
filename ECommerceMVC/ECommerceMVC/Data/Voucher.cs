using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceMVC.Data
{
    [Table("Voucher")]
    public class Voucher
    {
        [Key]
        public int MaVoucher { get; set; }

        public string Code { get; set; } = null!;
        public decimal DiscountPercent { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public int SoLuong { get; set; }
        public int DaSuDung { get; set; }
        public bool HieuLuc { get; set; }
        public string? MoTa { get; set; }
        public DateTime NgayTao { get; set; }

        // Navigation
        public virtual ICollection<VoucherUsage> VoucherUsages { get; set; } = new List<VoucherUsage>();
    }

    public class VoucherUsage
    {
        public int Id { get; set; }
        public int MaVoucher { get; set; }
        public string MaKh { get; set; } = null!;
        public int MaHd { get; set; }
        public DateTime NgaySuDung { get; set; }
        public decimal GiamGia { get; set; }

        // Navigation
        public virtual Voucher MaVoucherNavigation { get; set; } = null!;
        public virtual KhachHang MaKhNavigation { get; set; } = null!;
        public virtual HoaDon MaHdNavigation { get; set; } = null!;
    }
}