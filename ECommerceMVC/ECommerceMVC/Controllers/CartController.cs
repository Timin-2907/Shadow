using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using ECommerceMVC.Helpers;
using ECommerceMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMVC.Controllers
{
    public class CartController : Controller
    {
        private readonly ShoeContext db;
        private readonly PaypalClient _paypalClient;
        private readonly IVnPayService _vnPayService;

        public CartController(
            ShoeContext context,
            PaypalClient paypalClient,
            IVnPayService vnPayService)
        {
            db = context;
            _paypalClient = paypalClient;
            _vnPayService = vnPayService;
        }

        // ================== CART ==================
        public List<CartItem> Cart =>
            HttpContext.Session.Get<List<CartItem>>(MySetting.CART_KEY) ?? new List<CartItem>();

        public IActionResult Index()
        {
            return View(Cart);
        }

        public IActionResult AddToCart(int id, int quantity = 1)
        {
            var cart = Cart;
            var item = cart.SingleOrDefault(p => p.MaHh == id);

            if (item == null)
            {
                var hh = db.HangHoas.SingleOrDefault(p => p.MaHh == id);
                if (hh == null) return Redirect("/404");

                item = new CartItem
                {
                    MaHh = hh.MaHh,
                    TenHH = hh.TenHh,
                    DonGia = (decimal)(hh.DonGia ?? 0),
                    Hinh = hh.Hinh ?? "",
                    SoLuong = quantity
                };
                cart.Add(item);
            }
            else
            {
                item.SoLuong += quantity;
            }

            HttpContext.Session.Set(MySetting.CART_KEY, cart);
            return RedirectToAction("Index");
        }

        public IActionResult RemoveCart(int id)
        {
            var cart = Cart;
            var item = cart.SingleOrDefault(p => p.MaHh == id);
            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.Set(MySetting.CART_KEY, cart);
            }
            return RedirectToAction("Index");
        }

        // ================== APPLY VOUCHER ==================
        [HttpPost]
        public async Task<IActionResult> ApplyVoucher([FromBody] VoucherRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.VoucherCode))
                return Json(new { success = false, message = "Vui lòng nhập mã voucher" });

            var voucher = await db.Set<Voucher>()
                .FirstOrDefaultAsync(v => v.Code == request.VoucherCode && v.HieuLuc);

            if (voucher == null)
                return Json(new { success = false, message = "Mã voucher không hợp lệ hoặc đã hết hạn" });

            if (voucher.NgayBatDau > DateTime.Now || voucher.NgayKetThuc < DateTime.Now)
                return Json(new { success = false, message = "Voucher không còn hiệu lực" });

            if (voucher.DaSuDung >= voucher.SoLuong)
                return Json(new { success = false, message = "Voucher đã hết lượt sử dụng" });

            decimal orderAmount = Cart.Sum(p => p.ThanhTien);

            if (voucher.MinOrderAmount.HasValue && orderAmount < voucher.MinOrderAmount.Value)
            {
                return Json(new
                {
                    success = false,
                    message = $"Đơn hàng tối thiểu {voucher.MinOrderAmount.Value:N0}₫"
                });
            }

            decimal discount = orderAmount * voucher.DiscountPercent / 100;

            if (voucher.MaxDiscountAmount.HasValue && discount > voucher.MaxDiscountAmount.Value)
            {
                discount = voucher.MaxDiscountAmount.Value;
            }

            // Lưu vào Session
            HttpContext.Session.SetString("AppliedVoucher", voucher.Code);
            HttpContext.Session.SetString("VoucherDiscount", discount.ToString());

            return Json(new
            {
                success = true,
                voucherCode = voucher.Code,
                discountPercent = voucher.DiscountPercent,
                discountAmount = discount,
                maxDiscount = voucher.MaxDiscountAmount ?? discount,
                finalAmount = orderAmount - discount,
                message = "Áp dụng voucher thành công!"
            });
        }

        // Thêm class này vào cuối CartController
        public class VoucherRequest
        {
            public string VoucherCode { get; set; }
        }

        // ================== CHECKOUT ==================
        [Authorize]
        [HttpGet]
        public IActionResult Checkout()
        {
            if (!Cart.Any()) return Redirect("/");
            ViewBag.PaypalClientId = _paypalClient.ClientId;
            return View(Cart);
        }

        [Authorize]
        [HttpPost]
        public IActionResult Checkout(CheckoutVM model, string payment = "COD")
        {
            if (!ModelState.IsValid) return View(Cart);

            var voucherCode = HttpContext.Session.GetString("AppliedVoucher");
            decimal voucherDiscount = 0;
            decimal.TryParse(
                HttpContext.Session.GetString("VoucherDiscount"),
                out voucherDiscount
            );

            decimal totalAmount = Cart.Sum(p => p.ThanhTien) - voucherDiscount;

            // VNPay
            if (payment == "Thanh toán VNPay")
            {
                var vnPayModel = new VnPaymentRequestModel
                {
                    Amount = totalAmount,
                    CreatedDate = DateTime.Now,
                    Description = model.HoTen,
                    FullName = model.HoTen,
                    OrderId = new Random().Next(1000, 99999)
                };
                return Redirect(_vnPayService.CreatePaymentUrl(HttpContext, vnPayModel));
            }

            // COD
            var customerId = User.Claims
                .Single(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;

            var hoadon = new HoaDon
            {
                MaKh = customerId,
                HoTen = model.HoTen,
                DiaChi = model.DiaChi,
                DienThoai = model.DienThoai,
                NgayDat = DateTime.Now,
                CachThanhToan = "COD",
                CachVanChuyen = "GRAB",
                MaTrangThai = 0,
                VoucherCode = voucherCode,
                VoucherDiscount = voucherDiscount
            };

            db.Database.BeginTransaction();
            try
            {
                db.Add(hoadon);
                db.SaveChanges();

                foreach (var item in Cart)
                {
                    db.Add(new ChiTietHd
                    {
                        MaHd = hoadon.MaHd,
                        MaHh = item.MaHh,
                        SoLuong = item.SoLuong,
                        DonGia = item.DonGia,
                        GiamGia = 0
                    });
                }

                if (!string.IsNullOrEmpty(voucherCode))
                {
                    var voucher = db.Set<Voucher>()
                        .FirstOrDefault(v => v.Code == voucherCode);

                    if (voucher != null)
                    {
                        voucher.DaSuDung++;
                        db.Add(new VoucherUsage
                        {
                            MaVoucher = voucher.MaVoucher,
                            MaHd = hoadon.MaHd,
                            MaKh = customerId,
                            NgaySuDung = DateTime.Now,
                            GiamGia = voucherDiscount
                        });
                    }
                }

                db.SaveChanges();
                db.Database.CommitTransaction();

                HttpContext.Session.Remove(MySetting.CART_KEY);
                HttpContext.Session.Remove("AppliedVoucher");
                HttpContext.Session.Remove("VoucherDiscount");

                return View("Success");
            }
            catch
            {
                db.Database.RollbackTransaction();
                return View(Cart);
            }
        }
    }
}
