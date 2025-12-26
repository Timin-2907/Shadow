using ECommerceMVC.Data;
using ECommerceMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMVC.Controllers
{
    public class NewsletterController : Controller
    {
        private readonly INewsletterService _newsletterService;
        private readonly ShoeContext _context;

        public NewsletterController(INewsletterService newsletterService, ShoeContext context)
        {
            _newsletterService = newsletterService;
            _context = context;
        }

        // POST: Đăng ký nhận tin từ footer
        [HttpPost]
        public async Task<IActionResult> Subscribe(string email, string? source = "footer")
        {
            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                return Json(new
                {
                    success = false,
                    message = "Email không hợp lệ"
                });
            }

            var result = await _newsletterService.Subscribe(email);

            if (result)
            {
                return Json(new
                {
                    success = true,
                    message = "Đăng ký nhận tin thành công! Cảm ơn bạn đã quan tâm."
                });
            }

            return Json(new
            {
                success = false,
                message = "Đã có lỗi xảy ra. Vui lòng thử lại sau."
            });
        }

        // POST: Đăng ký khi tạo tài khoản
        [HttpPost]
        public async Task<IActionResult> SubscribeWithAccount(string email, string hoTen, string maKh)
        {
            var result = await _newsletterService.Subscribe(email, hoTen, maKh);
            return Json(new { success = result });
        }

        // GET: Hủy đăng ký qua link trong email
        [HttpGet]
        public async Task<IActionResult> Unsubscribe(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Link không hợp lệ";
                return RedirectToAction("Index", "Home");
            }

            var result = await _newsletterService.Unsubscribe(token);

            if (result)
            {
                ViewBag.Message = "Bạn đã hủy đăng ký nhận tin thành công.";
                ViewBag.Success = true;
            }
            else
            {
                ViewBag.Message = "Không tìm thấy thông tin đăng ký hoặc link đã hết hạn.";
                ViewBag.Success = false;
            }

            return View();
        }

        // GET: Trang quản lý preferences
        [HttpGet]
        public async Task<IActionResult> Preferences(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return NotFound();
            }

            var subscriber = await _context.Set<NewsletterSubscriber>()
                .FirstOrDefaultAsync(s => s.UnsubscribeToken == token);

            if (subscriber == null)
            {
                return NotFound();
            }

            return View(subscriber);
        }

        // POST: Cập nhật preferences
        [HttpPost]
        public async Task<IActionResult> UpdatePreferences(
            string token,
            bool sanPhamMoi,
            bool khuyenMai,
            bool voucher)
        {
            var subscriber = await _context.Set<NewsletterSubscriber>()
                .FirstOrDefaultAsync(s => s.UnsubscribeToken == token);

            if (subscriber == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin" });
            }

            var result = await _newsletterService.UpdatePreferences(
                subscriber.Email,
                sanPhamMoi,
                khuyenMai,
                voucher
            );

            if (result)
            {
                return Json(new
                {
                    success = true,
                    message = "Cập nhật tùy chọn thành công!"
                });
            }

            return Json(new
            {
                success = false,
                message = "Có lỗi xảy ra"
            });
        }

        // API: Kiểm tra email đã đăng ký chưa
        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            var isSubscribed = await _newsletterService.IsSubscribed(email);
            return Json(new { subscribed = isSubscribed });
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}