using AutoMapper;
using Microsoft.AspNetCore.Http;
using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.Services;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceMVC.Models;
using System.Security.Claims;

namespace ECommerceMVC.Controllers
{
    public class KhachHangController : Controller
    {
        // Sử dụng đầy đủ namespace để tránh xung đột với thư mục Models
        private readonly ECommerceMVC.Data.ShoeContext db;
        private readonly IMapper _mapper;
        private readonly INewsletterService _newsletterService;

        public KhachHangController(
            ECommerceMVC.Data.ShoeContext context,
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
            bool DangKyNhanTin = false
        )
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Chỉ định rõ lấy class KhachHang từ thư mục Data
                    var khachHang = _mapper.Map<ECommerceMVC.Data.KhachHang>(model);
                    khachHang.RandomKey = MyUtil.GenerateRamdomKey();
                    khachHang.MatKhau = model.MatKhau.ToMd5Hash(khachHang.RandomKey);
                    khachHang.HieuLuc = true;
                    khachHang.VaiTro = 0;
                    khachHang.DangKyNhanTin = DangKyNhanTin;

                    // Thường trong DB tên là MaQuyen hoặc MaPhanQuyen
                    khachHang.VaiTro = 2;

                    if (Hinh != null)
                    {
                        khachHang.Hinh = MyUtil.UploadHinh(Hinh, "KhachHang");
                    }

                    db.Add(khachHang);
                    await db.SaveChangesAsync();

                    if (DangKyNhanTin)
                    {
                        await _newsletterService.Subscribe(
                            model.Email,
                            model.HoTen,
                            khachHang.MaKh
                        );
                    }

                    TempData["Success"] = "Đăng ký thành công!";
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
                // Sử dụng MaQuyenNavigation để lấy thông tin quyền
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
                    // Use the stored VaiTro value directly to determine role
                    string roleName = khachHang.VaiTro == 1 ? "Admin"
                                    : khachHang.VaiTro == 2 ? "Staff"
                                    : "Customer";

                    var claims = new List<Claim>
                    {
                        new Claim(System.Security.Claims.ClaimTypes.Email, khachHang.Email ?? string.Empty),
                        new Claim(ClaimTypes.Name, khachHang.HoTen ?? string.Empty),
                        new Claim(MySetting.CLAIM_CUSTOMERID, khachHang.MaKh ?? string.Empty),
                        new Claim(ClaimTypes.Role, roleName)
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims,
                        CookieAuthenticationDefaults.AuthenticationScheme
                    );

                    await HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity));

                    HttpContext.Session.SetString("UserRole", roleName);
                    HttpContext.Session.SetString("MaKh", khachHang.MaKh);
                    HttpContext.Session.SetString("HoTen", khachHang.HoTen);

                    if (roleName == "Admin")
                        return RedirectToAction("Index", "Admin");
                    if (roleName == "Staff")
                        return RedirectToAction("Index", "Staff");

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
            HttpContext.Session.Clear();
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

            // Chỉ định rõ ECommerceMVC.Data.HoaDon để tránh trùng tên
            var orders = db.Set<ECommerceMVC.Data.HoaDon>()
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

            var order = db.Set<ECommerceMVC.Data.HoaDon>()
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