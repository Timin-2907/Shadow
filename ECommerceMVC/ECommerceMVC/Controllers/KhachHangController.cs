using AutoMapper;
using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.Services;              // ✅ THÊM
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceMVC.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly ShoeContext db;
        private readonly IMapper _mapper;
        private readonly INewsletterService _newsletterService; // ✅ THÊM

        // ✅ CẬP NHẬT CONSTRUCTOR
        public KhachHangController(
            ShoeContext context,
            IMapper mapper,
            INewsletterService newsletterService
        )
        {
            db = context;
            _mapper = mapper;
            _newsletterService = newsletterService;
        }

        #region Register
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DangKy(
            RegisterVM model,
            IFormFile Hinh,
            bool DangKyNhanTin = false   // ✅ checkbox newsletter
        )
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var khachHang = _mapper.Map<KhachHang>(model);
                    khachHang.RandomKey = MyUtil.GenerateRamdomKey();
                    khachHang.MatKhau = model.MatKhau.ToMd5Hash(khachHang.RandomKey);
                    khachHang.HieuLuc = true;
                    khachHang.VaiTro = 0;
                    khachHang.DangKyNhanTin = DangKyNhanTin; // ✅ LƯU DB

                    if (Hinh != null)
                    {
                        khachHang.Hinh = MyUtil.UploadHinh(Hinh, "KhachHang");
                    }

                    db.Add(khachHang);
                    await db.SaveChangesAsync();

                    // ✅ ĐĂNG KÝ NEWSLETTER
                    if (DangKyNhanTin)
                    {
                        await _newsletterService.Subscribe(
                            model.Email,
                            model.HoTen,
                            khachHang.MaKh
                        );
                    }

                    TempData["Success"] = "Đăng ký thành công!";
                    TempData["UserName"] = khachHang.MaKh;
                    TempData["Email"] = khachHang.Email;

                    return RedirectToAction("DangKyThanhCong");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi: " + ex.Message);
                }
            }
            return View();
        }

        [HttpGet]
        public IActionResult DangKyThanhCong()
        {
            return View();
        }
        #endregion

        #region Login
        [HttpGet]
        public IActionResult DangNhap(string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DangNhap(LoginVM model, string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;

            if (ModelState.IsValid)
            {
                var khachHang = db.KhachHangs
                    .SingleOrDefault(kh => kh.MaKh == model.UserName);

                if (khachHang == null)
                {
                    ModelState.AddModelError("loi", "Không có khách hàng này");
                }
                else if (!khachHang.HieuLuc)
                {
                    ModelState.AddModelError("loi", "Tài khoản đã bị khóa");
                }
                else if (khachHang.MatKhau != model.Password.ToMd5Hash(khachHang.RandomKey))
                {
                    ModelState.AddModelError("loi", "Sai thông tin đăng nhập");
                }
                else
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, khachHang.Email),
                        new Claim(ClaimTypes.Name, khachHang.HoTen),
                        new Claim(MySetting.CLAIM_CUSTOMERID, khachHang.MaKh),
                        new Claim(ClaimTypes.Role, "Customer")
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims,
                        CookieAuthenticationDefaults.AuthenticationScheme
                    );

                    await HttpContext.SignInAsync(
                        new ClaimsPrincipal(claimsIdentity)
                    );

                    if (Url.IsLocalUrl(ReturnUrl))
                        return Redirect(ReturnUrl);

                    return Redirect("/");
                }
            }
            return View();
        }
        #endregion

        [Authorize]
        public IActionResult Profile()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> DangXuat()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }

        #region Order History
        [Authorize]
        public IActionResult OrderHistory()
        {
            var customerId = User.Claims
                .SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID)?.Value;

            if (string.IsNullOrEmpty(customerId))
                return RedirectToAction("DangNhap");

            var orders = db.HoaDons
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHds)
                .Where(h => h.MaKh == customerId)
                .OrderByDescending(h => h.NgayDat)
                .Select(h => new OrderHistory
                {
                    MaHD = h.MaHd,
                    NgayDat = h.NgayDat,
                    TrangThai = h.MaTrangThaiNavigation.TenTrangThai,
                    PhuongThucThanhToan = h.CachThanhToan,
                    TongTien = (decimal)h.ChiTietHds.Sum(ct => ct.SoLuong * ct.DonGia),
                    VnPayTransactionId = h.VnpayTransactionId ?? "",
                    ChiTiet = new List<OrderDetailViewModel>()
                })
                .ToList();

            return View(orders);
        }

        [Authorize]
        public IActionResult OrderDetail(int id)
        {
            var customerId = User.Claims
                .SingleOrDefault(p => p.Type == MySetting.CLAIM_CUSTOMERID)?.Value;

            if (string.IsNullOrEmpty(customerId))
                return RedirectToAction("DangNhap");

            var order = db.HoaDons
                .Include(h => h.ChiTietHds)
                    .ThenInclude(ct => ct.MaHhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Where(h => h.MaHd == id && h.MaKh == customerId)
                .Select(h => new OrderHistory
                {
                    MaHD = h.MaHd,
                    NgayDat = h.NgayDat,
                    TrangThai = h.MaTrangThaiNavigation.TenTrangThai,
                    PhuongThucThanhToan = h.CachThanhToan,
                    TongTien = (decimal)h.ChiTietHds.Sum(ct => ct.SoLuong * ct.DonGia),
                    VnPayTransactionId = h.VnpayTransactionId ?? "",
                    ChiTiet = h.ChiTietHds.Select(ct => new OrderDetailViewModel
                    {
                        TenHH = ct.MaHhNavigation.TenHh,
                        Hinh = ct.MaHhNavigation.Hinh ?? "",
                        SoLuong = ct.SoLuong,
                        DonGia = (decimal)ct.DonGia,
                        ThanhTien = (decimal)(ct.SoLuong * ct.DonGia)
                    }).ToList()
                })
                .FirstOrDefault();

            if (order == null)
            {
                TempData["Message"] = "Không tìm thấy đơn hàng";
                return RedirectToAction("OrderHistory");
            }

            return View(order);
        }
        #endregion
    }
}
