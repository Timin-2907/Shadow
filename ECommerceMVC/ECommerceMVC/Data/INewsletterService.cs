using ECommerceMVC.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMVC.Services
{
    public interface INewsletterService
    {
        Task<bool> Subscribe(string email, string? hoTen = null, string? maKh = null);
        Task<bool> Unsubscribe(string token);
        Task<bool> UpdatePreferences(string email, bool sanPhamMoi, bool khuyenMai, bool voucher);
        Task<bool> IsSubscribed(string email);
        Task<NewsletterSubscriber?> GetSubscriber(string email);
        Task<List<NewsletterSubscriber>> GetActiveSubscribers(string? loaiEmail = null);
        Task<NewsletterStats> GetStats();
    }

    public class NewsletterStats
    {
        public int TotalSubscribers { get; set; }
        public int ActiveSubscribers { get; set; }
        public int InactiveSubscribers { get; set; }
        public int RegisteredCustomers { get; set; }
        public int GuestSubscribers { get; set; }
        public int TotalEmailsSent { get; set; }
        public int TotalEmailsOpened { get; set; }
        public double OpenRate { get; set; }
    }

    public class NewsletterService : INewsletterService
    {
        private readonly ShoeContext _context;

        public NewsletterService(ShoeContext context)
        {
            _context = context;
        }

        public async Task<bool> Subscribe(string email, string? hoTen = null, string? maKh = null)
        {
            try
            {
                var existing = await _context.Set<NewsletterSubscriber>()
                    .FirstOrDefaultAsync(s => s.Email == email);

                if (existing != null)
                {
                    // Nếu đã tồn tại nhưng đã hủy → kích hoạt lại
                    if (!existing.IsActive)
                    {
                        existing.IsActive = true;
                        existing.NgayDangKy = DateTime.Now;
                        existing.NgayHuyDangKy = null;
                        existing.UnsubscribeToken = Guid.NewGuid().ToString();
                    }

                    // Cập nhật thông tin nếu có
                    if (!string.IsNullOrEmpty(hoTen))
                        existing.HoTen = hoTen;
                    if (!string.IsNullOrEmpty(maKh))
                        existing.MaKh = maKh;

                    await _context.SaveChangesAsync();
                    return true;
                }

                // Tạo mới
                var subscriber = new NewsletterSubscriber
                {
                    Email = email,
                    HoTen = hoTen,
                    MaKh = maKh,
                    IsActive = true,
                    UnsubscribeToken = Guid.NewGuid().ToString(),
                    NgayDangKy = DateTime.Now
                };

                _context.Set<NewsletterSubscriber>().Add(subscriber);
                await _context.SaveChangesAsync();

                // Cập nhật cột DangKyNhanTin trong KhachHang nếu có
                if (!string.IsNullOrEmpty(maKh))
                {
                    var khachHang = await _context.KhachHangs.FindAsync(maKh);
                    if (khachHang != null)
                    {
                        khachHang.DangKyNhanTin = true;
                        khachHang.NewsletterToken = subscriber.UnsubscribeToken;
                        await _context.SaveChangesAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error subscribing: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> Unsubscribe(string token)
        {
            try
            {
                var subscriber = await _context.Set<NewsletterSubscriber>()
                    .FirstOrDefaultAsync(s => s.UnsubscribeToken == token);

                if (subscriber == null)
                    return false;

                subscriber.IsActive = false;
                subscriber.NgayHuyDangKy = DateTime.Now;

                // Cập nhật KhachHang nếu có
                if (!string.IsNullOrEmpty(subscriber.MaKh))
                {
                    var khachHang = await _context.KhachHangs.FindAsync(subscriber.MaKh);
                    if (khachHang != null)
                    {
                        khachHang.DangKyNhanTin = false;
                        await _context.SaveChangesAsync();
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unsubscribing: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdatePreferences(string email, bool sanPhamMoi, bool khuyenMai, bool voucher)
        {
            try
            {
                var subscriber = await _context.Set<NewsletterSubscriber>()
                    .FirstOrDefaultAsync(s => s.Email == email);

                if (subscriber == null)
                    return false;

                subscriber.NhanThongBaoSanPhamMoi = sanPhamMoi;
                subscriber.NhanThongBaoKhuyenMai = khuyenMai;
                subscriber.NhanThongBaoVoucher = voucher;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating preferences: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsSubscribed(string email)
        {
            return await _context.Set<NewsletterSubscriber>()
                .AnyAsync(s => s.Email == email && s.IsActive);
        }

        public async Task<NewsletterSubscriber?> GetSubscriber(string email)
        {
            return await _context.Set<NewsletterSubscriber>()
                .FirstOrDefaultAsync(s => s.Email == email);
        }

        public async Task<List<NewsletterSubscriber>> GetActiveSubscribers(string? loaiEmail = null)
        {
            var query = _context.Set<NewsletterSubscriber>()
                .Where(s => s.IsActive);

            if (!string.IsNullOrEmpty(loaiEmail))
            {
                query = loaiEmail switch
                {
                    "SanPhamMoi" => query.Where(s => s.NhanThongBaoSanPhamMoi),
                    "KhuyenMai" => query.Where(s => s.NhanThongBaoKhuyenMai),
                    "Voucher" => query.Where(s => s.NhanThongBaoVoucher),
                    _ => query
                };
            }

            return await query.OrderByDescending(s => s.NgayDangKy).ToListAsync();
        }

        public async Task<NewsletterStats> GetStats()
        {
            var subscribers = await _context.Set<NewsletterSubscriber>().ToListAsync();

            return new NewsletterStats
            {
                TotalSubscribers = subscribers.Count,
                ActiveSubscribers = subscribers.Count(s => s.IsActive),
                InactiveSubscribers = subscribers.Count(s => !s.IsActive),
                RegisteredCustomers = subscribers.Count(s => !string.IsNullOrEmpty(s.MaKh)),
                GuestSubscribers = subscribers.Count(s => string.IsNullOrEmpty(s.MaKh)),
                TotalEmailsSent = subscribers.Sum(s => s.EmailsSent),
                TotalEmailsOpened = subscribers.Sum(s => s.EmailsOpened),
                OpenRate = subscribers.Sum(s => s.EmailsSent) > 0
                    ? (double)subscribers.Sum(s => s.EmailsOpened) / subscribers.Sum(s => s.EmailsSent) * 100
                    : 0
            };
        }
    }
}