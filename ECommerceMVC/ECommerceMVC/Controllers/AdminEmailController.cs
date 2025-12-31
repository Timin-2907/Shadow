using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceMVC.Data;
using ECommerceMVC.Services;
using ECommerceMVC.ViewModels;
using System.Linq;

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

        public IActionResult Index()
        {
            return View();
        }

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

            var subscribers = await _context.Set<NewsletterSubscriber>()
                .Where(s => s.IsActive)
                .Select(s => new SubscriberViewModel
                {
                    Id = s.Id,
                    Email = s.Email,
                    HoTen = s.HoTen ?? "Khách hàng",
                    MaKh = s.MaKh ?? ""
                })
                .OrderBy(s => s.HoTen)
                .ToListAsync();

            ViewBag.Subscribers = subscribers ?? new List<SubscriberViewModel>();

            return View();
        }

        [HttpPost]
        //  [ValidateAntiForgeryToken]//
        public async Task<IActionResult> SendProductPromotion(SendProductEmailVM model)
        {
            try
            {
                Console.WriteLine("==============================================");
                Console.WriteLine("=== SendProductPromotion POST CALLED ===");
                Console.WriteLine("==============================================");
                Console.WriteLine($"ProductId: {model.ProductId}");
                Console.WriteLine($"DiscountPercent: {model.DiscountPercent}");
                Console.WriteLine($"VoucherCode: {model.VoucherCode}");
                Console.WriteLine($"SendToAll: {model.SendToAll}");
                Console.WriteLine($"SendToSubscribersOnly: {model.SendToSubscribersOnly}");
                Console.WriteLine($"EmailList length: {model.EmailList?.Length ?? 0}");
                Console.WriteLine($"EmailList content: {model.EmailList}");
                Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

                if (!ModelState.IsValid)
                {
                    Console.WriteLine("=== ModelState ERRORS ===");
                    foreach (var state in ModelState)
                    {
                        if (state.Value.Errors.Count > 0)
                        {
                            Console.WriteLine($"Key: {state.Key}");
                            foreach (var error in state.Value.Errors)
                            {
                                Console.WriteLine($"  Error: {error.ErrorMessage}");
                            }
                        }
                    }
                }

                // Validate ProductId
                if (model.ProductId <= 0)
                {
                    Console.WriteLine("ERROR: ProductId is 0 or negative");
                    TempData["Error"] = "Vui lòng chọn sản phẩm";
                    return RedirectToAction(nameof(SendProductPromotion));
                }

                var product = await _context.HangHoas
                    .FirstOrDefaultAsync(h => h.MaHh == model.ProductId);

                if (product == null)
                {
                    Console.WriteLine($"ERROR: Product not found with ID {model.ProductId}");
                    TempData["Error"] = "Không tìm thấy sản phẩm";
                    return RedirectToAction(nameof(SendProductPromotion));
                }

                Console.WriteLine($"Product found: {product.TenHh}");

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

                // ===== LẤY DANH SÁCH EMAIL =====
                List<string> emails = new List<string>();

                if (model.SendToAll)
                {
                    if (model.SendToSubscribersOnly)
                    {
                        var subscribers = await _newsletterService.GetActiveSubscribers("SanPhamMoi");
                        emails = subscribers.Select(s => s.Email).Distinct().ToList();
                    }
                    else
                    {
                        emails = await _context.KhachHangs
                            .Where(k => k.HieuLuc && !string.IsNullOrEmpty(k.Email))
                            .Select(k => k.Email)
                            .Distinct()
                            .ToListAsync();
                    }
                }
                else
                {
                    // ✅ Chỉ lấy từ EmailList (đã được JS merge từ checkboxes + textarea)
                    if (!string.IsNullOrWhiteSpace(model.EmailList))
                    {
                        emails = model.EmailList
                            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(e => e.Trim())
                            .Where(e => !string.IsNullOrEmpty(e) && e.Contains("@"))
                            .Distinct()
                            .ToList();
                    }
                }

                // Validate email list
                if (!emails.Any())
                {
                    Console.WriteLine("ERROR: No emails to send");
                    TempData["Error"] = "Không có email hợp lệ để gửi. Vui lòng kiểm tra lại.";
                    return RedirectToAction(nameof(SendProductPromotion));
                }

                Console.WriteLine($"=== Sending emails to {emails.Count} recipients ===");

                // Gửi email
                int success = 0;
                int failed = 0;

                foreach (var email in emails)
                {
                    try
                    {
                        Console.WriteLine($"Sending to: {email}");
                        if (await _emailService.SendProductPromotionEmail(email, "Khách hàng", promotion))
                        {
                            success++;
                            Console.WriteLine($"✓ Success: {email}");
                        }
                        else
                        {
                            failed++;
                            Console.WriteLine($"✗ Failed: {email}");
                        }

                        await Task.Delay(300); // Tránh spam
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Exception sending to {email}: {ex.Message}");
                        failed++;
                    }
                }

                Console.WriteLine($"=== Email sending completed: {success} success, {failed} failed ===");

                if (success > 0)
                {
                    TempData["Success"] = $"Đã gửi thành công {success}/{emails.Count} email!";
                    if (failed > 0)
                    {
                        TempData["Warning"] = $"Có {failed} email gửi thất bại.";
                    }
                }
                else
                {
                    TempData["Error"] = "Không gửi được email nào. Vui lòng kiểm tra cấu hình email.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPTION in SendProductPromotion ===");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(SendProductPromotion));
            }
        }

        private string GenerateSlug(string text)
        {
            return System.Text.RegularExpressions.Regex
                .Replace(text.ToLowerInvariant(), @"[^a-z0-9]+", "-")
                .Trim('-');
        }
    }
}