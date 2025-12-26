using System.Text;
using Microsoft.AspNetCore.Mvc;
using ECommerceMVC.Services;
using ECommerceMVC.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMVC.Controllers
{
    public class AdminNewsletterController : Controller
    {
        private readonly INewsletterService _newsletterService;
        private readonly ShoeContext _context;

        public AdminNewsletterController(INewsletterService newsletterService, ShoeContext context)
        {
            _newsletterService = newsletterService;
            _context = context;
        }

        // GET: Dashboard
        public async Task<IActionResult> Index()
        {
            var stats = await _newsletterService.GetStats();
            return View(stats);
        }

        // GET: Danh sách subscribers
        public async Task<IActionResult> Subscribers(string? filter = "all")
        {
            var query = _context.Set<NewsletterSubscriber>()
                .Include(s => s.MaKhNavigation)
                .AsQueryable();

            query = filter switch
            {
                "active" => query.Where(s => s.IsActive),
                "inactive" => query.Where(s => !s.IsActive),
                "registered" => query.Where(s => s.MaKh != null),
                "guest" => query.Where(s => s.MaKh == null),
                _ => query
            };

            var subscribers = await query
                .OrderByDescending(s => s.NgayDangKy)
                .ToListAsync();

            ViewBag.Filter = filter;
            return View(subscribers);
        }

        // POST: Export subscribers to CSV
        [HttpPost]
        public async Task<IActionResult> ExportSubscribers()
        {
            var subscribers = await _newsletterService.GetActiveSubscribers();

            var csv = new StringBuilder();
            csv.AppendLine("Email,HoTen,NgayDangKy,SanPhamMoi,KhuyenMai,Voucher");

            foreach (var sub in subscribers)
            {
                csv.AppendLine($"{sub.Email},{sub.HoTen},{sub.NgayDangKy:yyyy-MM-dd}," +
                    $"{sub.NhanThongBaoSanPhamMoi},{sub.NhanThongBaoKhuyenMai},{sub.NhanThongBaoVoucher}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"subscribers_{DateTime.Now:yyyyMMdd}.csv");
        }

        // POST: Xóa subscriber
        [HttpPost]
        public async Task<IActionResult> DeleteSubscriber(int id)
        {
            try
            {
                var subscriber = await _context.Set<NewsletterSubscriber>()
                    .FindAsync(id);

                if (subscriber == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy subscriber" });
                }

                _context.Set<NewsletterSubscriber>().Remove(subscriber);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // GET: Campaign history
        public async Task<IActionResult> Campaigns()
        {
            var campaigns = await _context.Set<EmailCampaign>()
                .OrderByDescending(c => c.NgayTao)
                .ToListAsync();

            return View(campaigns);
        }

        // GET: Campaign details
        public async Task<IActionResult> CampaignDetails(int id)
        {
            var campaign = await _context.Set<EmailCampaign>()
                .Include(c => c.EmailCampaignLogs)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
            {
                return NotFound();
            }

            return View(campaign);
        }

        // API: Get stats
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _newsletterService.GetStats();
            return Json(stats);
        }
    }
}