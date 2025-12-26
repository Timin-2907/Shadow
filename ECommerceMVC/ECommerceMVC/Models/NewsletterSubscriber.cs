namespace ECommerceMVC.Data
{
    public class NewsletterSubscriber
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string? HoTen { get; set; }
        public string? MaKh { get; set; }  // Null nếu chưa đăng ký account
        public bool IsActive { get; set; } = true;
        public DateTime NgayDangKy { get; set; } = DateTime.Now;
        public string? UnsubscribeToken { get; set; }
        public DateTime? NgayHuyDangKy { get; set; }

        // Preferences
        public bool NhanThongBaoSanPhamMoi { get; set; } = true;
        public bool NhanThongBaoKhuyenMai { get; set; } = true;
        public bool NhanThongBaoVoucher { get; set; } = true;

        // Tracking
        public int EmailsSent { get; set; } = 0;
        public int EmailsOpened { get; set; } = 0;
        public DateTime? LastEmailSent { get; set; }

        // Navigation
        public virtual KhachHang? MaKhNavigation { get; set; }
    }

    public class EmailCampaign
    {
        public int Id { get; set; }
        public string TenChienDich { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string EmailContent { get; set; } = null!;
        public string LoaiChienDich { get; set; } = null!; // "SanPhamMoi", "KhuyenMai", "Voucher"
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public DateTime? NgayGui { get; set; }
        public string? NguoiTao { get; set; }
        public int TongSoEmail { get; set; }
        public int DaGui { get; set; }
        public int ThanhCong { get; set; }
        public int ThatBai { get; set; }
        public string TrangThai { get; set; } = "Draft"; // Draft, Sending, Completed

        // Navigation
        public virtual ICollection<EmailCampaignLog> EmailCampaignLogs { get; set; } = new List<EmailCampaignLog>();
    }

    public class EmailCampaignLog
    {
        public int Id { get; set; }
        public int CampaignId { get; set; }
        public string Email { get; set; } = null!;
        public DateTime NgayGui { get; set; }
        public bool ThanhCong { get; set; }
        public string? ErrorMessage { get; set; }
        public bool? DaMo { get; set; }
        public DateTime? NgayMo { get; set; }

        // Navigation
        public virtual EmailCampaign Campaign { get; set; } = null!;
    }
}