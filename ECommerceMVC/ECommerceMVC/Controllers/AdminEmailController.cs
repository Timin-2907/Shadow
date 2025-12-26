using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceMVC.Data;
using ECommerceMVC.Services;
using ECommerceMVC.ViewModels;

namespace ECommerceMVC.Controllers
{
    public class AdminEmailController : Controller
    {
        private readonly ShoeContext _context;
        private readonly IEmailService _emailService;
        private readonly INewsletterService _newsletterService;

        public AdminEmailController(
            ShoeContext context,
            IEmailService emailService,
            INewsletterService newsletterService)
        {
            _context = context;
            _emailService = emailService;
            _newsletterService = newsletterService;
        }

        // =========================
        // DASHBOARD
        // =========================
        public IActionResult Index()
        {
            return View();
        }

        // =========================
        // GỬI EMAIL SẢN PHẨM
        // =========================
        public async Task<IActionResult> SendProductPromotion()
        {
            ViewBag.Products = await _context.HangHoas
                .Include(h => h.MaLoaiNavigation)
                .OrderByDescending(h => h.NgaySx)
                .Take(50)
                .ToListAsync();

            ViewBag.Vouchers = await _context.Set<Voucher>()
                .Where(v => v.HieuLuc && v.NgayKetThuc > DateTime.Now)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendProductPromotion(SendProductEmailVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ";
                return RedirectToAction(nameof(SendProductPromotion));
            }

            var product = await _context.HangHoas
                .FirstOrDefaultAsync(h => h.MaHh == model.ProductId);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm";
                return RedirectToAction(nameof(SendProductPromotion));
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var productUrl = $"{baseUrl}/san-pham/{GenerateSlug(product.TenHh)}-{product.MaHh}";
            var imageUrl = $"{baseUrl}/Hinh/HangHoa/{product.Hinh}";

            decimal originalPrice = (decimal)(product.DonGia ?? 0);
            decimal salePrice = originalPrice;

            if (model.DiscountPercent > 0)
                salePrice = originalPrice * (1 - model.DiscountPercent / 100);

            var promotion = new EmailProductPromotion
            {
                ProductId = product.MaHh,
                ProductName = product.TenHh,
                ProductImage = imageUrl,
                OriginalPrice = originalPrice,
                SalePrice = salePrice,
                ShortDescription = product.MoTaDonVi ?? product.TenHh,
                ProductUrl = productUrl,
                VoucherCode = model.VoucherCode,
                DiscountPercent = model.DiscountPercent
            };

            // ===== LẤY EMAIL =====
            List<string> emails;

            if (model.SendToAll)
            {
                if (model.SendToSubscribersOnly)
                {
                    var subscribers = await _newsletterService.GetActiveSubscribers("SanPhamMoi");
                    emails = subscribers.Select(s => s.Email).ToList();
                }
                else
                {
                    emails = await _context.KhachHangs
                        .Where(k => k.HieuLuc)
                        .Select(k => k.Email)
                        .ToListAsync();
                }
            }
            else
            {
                emails = model.EmailList
                    .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .ToList();
            }

            if (!emails.Any())
            {
                TempData["Error"] = "Không có email để gửi";
                return RedirectToAction(nameof(SendProductPromotion));
            }

            int success = 0;
            foreach (var email in emails)
            {
                if (await _emailService.SendProductPromotionEmail(email, "Khách hàng", promotion))
                    success++;

                await Task.Delay(300);
            }

            TempData["Success"] = $"Đã gửi {success}/{emails.Count} email";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // GỬI VOUCHER
        // =========================
        public IActionResult SendVoucher()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendVoucher(SendVoucherEmailVM model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ";
                return RedirectToAction(nameof(SendVoucher));
            }

            if (string.IsNullOrEmpty(model.VoucherCode))
                model.VoucherCode = GenerateVoucherCode();

            var voucher = new Voucher
            {
                Code = model.VoucherCode,
                DiscountPercent = model.DiscountPercent,
                MaxDiscountAmount = model.MaxDiscountAmount,
                MinOrderAmount = model.MinOrderAmount,
                NgayBatDau = model.StartDate,
                NgayKetThuc = model.ExpiryDate,
                SoLuong = model.Quantity,
                HieuLuc = true,
                NgayTao = DateTime.Now
            };

            _context.Add(voucher);
            await _context.SaveChangesAsync();

            var emails = model.SendToAll
                ? await _context.KhachHangs.Where(k => k.HieuLuc).Select(k => k.Email).ToListAsync()
                : model.EmailList.Split(',', ';', '\n').Select(e => e.Trim()).ToList();

            int success = 0;
            foreach (var email in emails)
            {
                if (await _emailService.SendVoucherEmail(
                    email, "Khách hàng", voucher.Code, voucher.DiscountPercent, voucher.NgayKetThuc))
                    success++;

                await Task.Delay(300);
            }

            TempData["Success"] = $"Đã gửi {success}/{emails.Count} email voucher";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // HÀM PHỤ
        // =========================
        private string GenerateVoucherCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var rnd = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[rnd.Next(s.Length)]).ToArray());
        }

        private string GenerateSlug(string text)
        {
            return System.Text.RegularExpressions.Regex
                .Replace(text.ToLowerInvariant(), @"[^a-z0-9]+", "-")
                .Trim('-');
        }
    }

    // =========================
    // VIEW MODELS
    // =========================
    public class SendProductEmailVM
    {
        public int ProductId { get; set; }
        public decimal DiscountPercent { get; set; }
        public string? VoucherCode { get; set; }
        public bool SendToAll { get; set; }
        public bool SendToSubscribersOnly { get; set; }
        public string EmailList { get; set; } = "";
    }

    public class SendVoucherEmailVM
    {
        public string VoucherCode { get; set; } = "";
        public decimal DiscountPercent { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public bool SendToAll { get; set; }
        public string EmailList { get; set; } = "";
    }
}
