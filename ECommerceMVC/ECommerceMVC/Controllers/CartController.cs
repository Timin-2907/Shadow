using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using ECommerceMVC.Helpers;
using ECommerceMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
        public async Task<IActionResult> Checkout(CheckoutVM model, string payment = "COD")
        {
            if (!Cart.Any())
            {
                TempData["Message"] = "Giỏ hàng trống!";
                return Redirect("/");
            }

            var customerId = User.Claims
                .Single(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;

            // ===== LẤY THÔNG TIN GIAO HÀNG =====
            string hoTen, diaChi, dienThoai, ghiChu;

            if (model.GiongKhachHang)
            {
                // Lấy thông tin từ database
                var customer = await db.KhachHangs.FindAsync(customerId);
                if (customer == null)
                {
                    ModelState.AddModelError("", "Không tìm thấy thông tin khách hàng");
                    ViewBag.PaypalClientId = _paypalClient.ClientId;
                    return View(Cart);
                }

                hoTen = customer.HoTen;
                diaChi = customer.DiaChi ?? "";
                dienThoai = customer.DienThoai ?? "";
                ghiChu = model.GhiChu ?? "";
            }
            else
            {
                // Validate thông tin từ form
                if (string.IsNullOrWhiteSpace(model.HoTen) ||
                    string.IsNullOrWhiteSpace(model.DiaChi) ||
                    string.IsNullOrWhiteSpace(model.DienThoai))
                {
                    ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin giao hàng");
                    ViewBag.PaypalClientId = _paypalClient.ClientId;
                    return View(Cart);
                }

                hoTen = model.HoTen;
                diaChi = model.DiaChi;
                dienThoai = model.DienThoai;
                ghiChu = model.GhiChu ?? "";
            }

            // ===== TÍNH TỔNG TIỀN =====
            var voucherCode = HttpContext.Session.GetString("AppliedVoucher");
            decimal voucherDiscount = 0;
            decimal.TryParse(
                HttpContext.Session.GetString("VoucherDiscount"),
                out voucherDiscount
            );

            decimal totalAmount = Cart.Sum(p => p.ThanhTien) - voucherDiscount;

            // ===== VNPAY =====
            if (payment == "Thanh toán VNPay")
            {
                var deliveryInfo = new DeliveryInfoVM
                {
                    HoTen = hoTen,
                    DiaChi = diaChi,
                    DienThoai = dienThoai,
                    GhiChu = ghiChu
                };
                HttpContext.Session.Set("DeliveryInfo", deliveryInfo);

                var vnPayModel = new VnPaymentRequestModel
                {
                    Amount = totalAmount,
                    CreatedDate = DateTime.Now,
                    Description = $"Thanh toan don hang {hoTen}",
                    FullName = hoTen,
                    OrderId = new Random().Next(100000, 999999)
                };

                return Redirect(_vnPayService.CreatePaymentUrl(HttpContext, vnPayModel));
            }

            // ===== COD =====
            var hoadon = new HoaDon
            {
                MaKh = customerId,
                HoTen = hoTen,
                DiaChi = diaChi,
                DienThoai = dienThoai,
                NgayDat = DateTime.Now,
                CachThanhToan = "COD",
                CachVanChuyen = "GRAB",
                MaTrangThai = 0,
                VoucherCode = voucherCode,
                VoucherDiscount = voucherDiscount,
                GhiChu = ghiChu
            };

            db.Database.BeginTransaction();
            try
            {
                db.Add(hoadon);
                await db.SaveChangesAsync();

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

                await db.SaveChangesAsync();
                db.Database.CommitTransaction();

                HttpContext.Session.Remove(MySetting.CART_KEY);
                HttpContext.Session.Remove("AppliedVoucher");
                HttpContext.Session.Remove("VoucherDiscount");

                TempData["Message"] = "Đặt hàng thành công!";
                return View("Success");
            }
            catch (Exception ex)
            {
                db.Database.RollbackTransaction();

                // ✅ LOG CHI TIẾT LỖI
                var errorMsg = $"Error: {ex.Message}";
                if (ex.InnerException != null)
                    errorMsg += $"\nInner: {ex.InnerException.Message}";
                if (ex.InnerException?.InnerException != null)
                    errorMsg += $"\nInner2: {ex.InnerException.InnerException.Message}";

                System.Diagnostics.Debug.WriteLine(errorMsg);
                ModelState.AddModelError("", errorMsg);

                ViewBag.PaypalClientId = _paypalClient.ClientId;
                return View(Cart);
            }
        }

        // ================== VNPAY CALLBACK ==================
        [HttpGet]
        public async Task<IActionResult> PaymentCallBack()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response == null || !response.Success)
            {
                TempData["Message"] = "Thanh toán VNPay thất bại!";
                return View("PaymentFail");
            }

            var customerId = User.Claims
                .Single(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;

            var voucherCode = HttpContext.Session.GetString("AppliedVoucher");
            decimal voucherDiscount = 0;
            decimal.TryParse(
                HttpContext.Session.GetString("VoucherDiscount"),
                out voucherDiscount
            );

            var deliveryInfo = HttpContext.Session.Get<DeliveryInfoVM>("DeliveryInfo");
            if (deliveryInfo == null)
            {
                TempData["Message"] = "Thông tin giao hàng không hợp lệ!";
                return View("PaymentFail");
            }

            var hoadon = new HoaDon
            {
                MaKh = customerId,
                HoTen = deliveryInfo.HoTen,
                DiaChi = deliveryInfo.DiaChi,
                DienThoai = deliveryInfo.DienThoai,
                NgayDat = DateTime.Now,
                CachThanhToan = "VNPAY",
                CachVanChuyen = "GRAB",
                MaTrangThai = 0,
                VoucherCode = voucherCode,
                VoucherDiscount = voucherDiscount,
                VnpayTransactionId = response.TransactionId,
                VnpayResponseCode = response.VnPayResponseCode,
                GhiChu = deliveryInfo.GhiChu
            };

            db.Database.BeginTransaction();
            try
            {
                db.Add(hoadon);
                await db.SaveChangesAsync();

                var cart = Cart;
                foreach (var item in cart)
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

                await db.SaveChangesAsync();
                db.Database.CommitTransaction();

                HttpContext.Session.Remove(MySetting.CART_KEY);
                HttpContext.Session.Remove("AppliedVoucher");
                HttpContext.Session.Remove("VoucherDiscount");
                HttpContext.Session.Remove("DeliveryInfo");

                TempData["Message"] = "Thanh toán VNPay thành công!";
                return View("Success");
            }
            catch (Exception ex)
            {
                db.Database.RollbackTransaction();

                // ✅ LOG CHI TIẾT LỖI
                var errorMsg = $"Error: {ex.Message}";
                if (ex.InnerException != null)
                    errorMsg += $" | Inner: {ex.InnerException.Message}";

                System.Diagnostics.Debug.WriteLine("VNPay Error: " + errorMsg);
                TempData["Message"] = errorMsg;
                return View("PaymentFail");
            }
        }

        // ================== PAYPAL ==================
        [HttpPost]
        public async Task<IActionResult> CreatePaypalOrder()
        {
            try
            {
                var cart = Cart;
                if (!cart.Any())
                {
                    return BadRequest(new { message = "Giỏ hàng trống" });
                }

                var voucherCode = HttpContext.Session.GetString("AppliedVoucher");
                decimal voucherDiscount = 0;
                decimal.TryParse(
                    HttpContext.Session.GetString("VoucherDiscount"),
                    out voucherDiscount
                );

                decimal totalAmount = cart.Sum(p => p.ThanhTien) - voucherDiscount;

                // Chuyển đổi sang USD (tỷ giá mẫu: 1 USD = 25,000 VND)
                decimal totalUSD = totalAmount / 25000;

                var reference = $"ORDER_{DateTime.Now.Ticks}";
                var response = await _paypalClient.CreateOrder(
                    totalUSD.ToString("F2"),
                    "USD",
                    reference
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CapturePaypalOrder(string orderId)
        {
            try
            {
                var response = await _paypalClient.CaptureOrder(orderId);

                if (response.status == "COMPLETED")
                {
                    var customerId = User.Claims
                        .Single(p => p.Type == MySetting.CLAIM_CUSTOMERID).Value;

                    var customer = await db.KhachHangs.FindAsync(customerId);

                    var voucherCode = HttpContext.Session.GetString("AppliedVoucher");
                    decimal voucherDiscount = 0;
                    decimal.TryParse(
                        HttpContext.Session.GetString("VoucherDiscount"),
                        out voucherDiscount
                    );

                    var payer = response.payer;
                    var shipping = response.purchase_units[0].shipping;

                    var hoadon = new HoaDon
                    {
                        MaKh = customerId,
                        HoTen = customer?.HoTen ?? $"{payer.name.given_name} {payer.name.surname}",
                        DiaChi = customer?.DiaChi ?? $"{shipping.address.address_line_1}, {shipping.address.admin_area_2}",
                        DienThoai = customer?.DienThoai ?? "N/A",
                        NgayDat = DateTime.Now,
                        CachThanhToan = "PayPal",
                        CachVanChuyen = "GRAB",
                        MaTrangThai = 0,
                        VoucherCode = voucherCode,
                        VoucherDiscount = voucherDiscount,
                        GhiChu = $"PayPal Order: {orderId}"
                    };

                    db.Database.BeginTransaction();
                    try
                    {
                        db.Add(hoadon);
                        await db.SaveChangesAsync();

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

                        await db.SaveChangesAsync();
                        db.Database.CommitTransaction();

                        HttpContext.Session.Remove(MySetting.CART_KEY);
                        HttpContext.Session.Remove("AppliedVoucher");
                        HttpContext.Session.Remove("VoucherDiscount");

                        return Ok(response);
                    }
                    catch
                    {
                        db.Database.RollbackTransaction();
                        throw;
                    }
                }

                return BadRequest(new { message = "Payment failed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult PaymentSuccess()
        {
            TempData["Message"] = "Thanh toán thành công!";
            return View("Success");
        }
    }
}