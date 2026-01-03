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

        // ✅ GIỮ NAVIGATION CHO VOUCHER
        public virtual ICollection<VoucherUsage> VoucherUsages { get; set; } = new List<VoucherUsage>();
    }

    [Table("VoucherUsage")]
    public class VoucherUsage
    {
        [Key]
        public int Id { get; set; }

        public int MaVoucher { get; set; }

        [Column("MaKH")]
        public string MaKh { get; set; } = null!;

        [Column("MaHD")]
        public int MaHd { get; set; }

        public DateTime NgaySuDung { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GiamGia { get; set; }

        [ForeignKey(nameof(MaVoucher))]
        public Voucher Voucher { get; set; } = null!;
    }
    // ✅ KHÔNG CÓ NAVIGATION PROPERTIES
}
