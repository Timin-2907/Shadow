using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceMVC.Data;
using ECommerceMVC.Helpers;

namespace ECommerceMVC.Controllers
{
    [AuthorizeRole("Admin")]
    public class AdminOrderController : Controller
    {
        private readonly ShoeContext _context;

        public AdminOrderController(ShoeContext context)
        {
            _context = context;
        }

        // GET: /AdminOrder/Index - Danh sách đơn hàng
        public async Task<IActionResult> Index(int? trangThai, string? search, string? paymentMethod, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHds)
                .AsQueryable();

            // Lọc theo trạng thái
            if (trangThai.HasValue)
            {
                query = query.Where(h => h.MaTrangThai == trangThai.Value);
            }

            // Tìm kiếm theo mã đơn hàng hoặc tên khách hàng
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(h =>
                    h.MaHd.ToString().Contains(search) ||
                    h.HoTen.Contains(search) ||
                    h.MaKh.Contains(search));
            }

            // Lọc theo phương thức thanh toán
            if (!string.IsNullOrEmpty(paymentMethod))
            {
                query = query.Where(h => h.CachThanhToan == paymentMethod);
            }

            // Lọc theo ngày
            if (fromDate.HasValue)
            {
                query = query.Where(h => h.NgayDat >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.AddDays(1);
                query = query.Where(h => h.NgayDat < endDate);
            }

            var orders = await query
                .OrderByDescending(h => h.NgayDat)
                .Select(h => new
                {
                    h.MaHd,
                    h.NgayDat,
                    h.MaKh,
                    KhachHang = h.MaKhNavigation.HoTen,
                    h.HoTen,
                    h.DiaChi,
                    h.DienThoai,
                    h.CachThanhToan,
                    h.MaTrangThai,
                    TrangThai = h.MaTrangThaiNavigation.TenTrangThai,
                    h.VoucherCode,
                    h.VoucherDiscount,
                    h.VnpayTransactionId,
                    TongTien = h.ChiTietHds.Sum(ct => ct.SoLuong * ct.DonGia) - h.VoucherDiscount,
                    SoLuong = h.ChiTietHds.Sum(ct => ct.SoLuong)
                })
                .ToListAsync();

            // Lấy danh sách trạng thái
            ViewBag.TrangThaiList = await _context.TrangThais.ToListAsync();
            ViewBag.TrangThaiSelected = trangThai;

            return View(orders);
        }

        // GET: /AdminOrder/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHds)
                    .ThenInclude(ct => ct.MaHhNavigation)
                .FirstOrDefaultAsync(h => h.MaHd == id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index");
            }

            // Lấy danh sách trạng thái
            ViewBag.TrangThaiList = await _context.TrangThais.ToListAsync();

            return View(order);
        }

        // POST: /AdminOrder/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, int trangThai, string? ghiChu)
        {
            try
            {
                var order = await _context.HoaDons.FindAsync(id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                // Cập nhật trạng thái
                order.MaTrangThai = trangThai;

                // Cập nhật ngày giao nếu hoàn thành
                if (trangThai == 2) // Giả sử 2 là "Đã giao"
                {
                    order.NgayGiao = DateTime.Now;
                }

                // Thêm ghi chú
                if (!string.IsNullOrEmpty(ghiChu))
                {
                    var timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    var adminName = User.Identity?.Name ?? "Admin";
                    var newNote = $"[{timestamp}] {adminName}: {ghiChu}";

                    order.GhiChu = string.IsNullOrEmpty(order.GhiChu)
                        ? newNote
                        : order.GhiChu + "\n" + newNote;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // POST: /AdminOrder/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var order = await _context.HoaDons
                    .Include(h => h.ChiTietHds)
                    .FirstOrDefaultAsync(h => h.MaHd == id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                // Chỉ cho phép xóa đơn hàng chưa xử lý
                if (order.MaTrangThai != 0)
                {
                    return Json(new { success = false, message = "Không thể xóa đơn hàng đã xử lý!" });
                }

                _context.HoaDons.Remove(order);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa đơn hàng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // GET: /AdminOrder/Statistics
        public async Task<IActionResult> Statistics()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            var stats = new
            {
                // Đơn hàng hôm nay
                OrdersToday = await _context.HoaDons
                    .CountAsync(h => h.NgayDat.Date == today),

                // Doanh thu hôm nay
                RevenueToday = await _context.HoaDons
                    .Where(h => h.NgayDat.Date == today && h.MaTrangThai == 2)
                    .SelectMany(h => h.ChiTietHds)
                    .SumAsync(ct => ct.SoLuong * ct.DonGia),

                // Đơn hàng tháng này
                OrdersThisMonth = await _context.HoaDons
                    .CountAsync(h => h.NgayDat >= thisMonth),

                // Doanh thu tháng này
                RevenueThisMonth = await _context.HoaDons
                    .Where(h => h.NgayDat >= thisMonth && h.MaTrangThai == 2)
                    .SelectMany(h => h.ChiTietHds)
                    .SumAsync(ct => ct.SoLuong * ct.DonGia),

                // Đơn hàng đang chờ
                PendingOrders = await _context.HoaDons
                    .CountAsync(h => h.MaTrangThai == 0),

                // Đơn hàng đang xử lý
                ProcessingOrders = await _context.HoaDons
                    .CountAsync(h => h.MaTrangThai == 1),

                // Top 5 khách hàng
                TopCustomers = await _context.HoaDons
                    .Where(h => h.MaTrangThai == 2)
                    .GroupBy(h => new { h.MaKh, h.MaKhNavigation.HoTen })
                    .Select(g => new
                    {
                        MaKh = g.Key.MaKh,
                        HoTen = g.Key.HoTen,
                        SoDonHang = g.Count(),
                        TongTien = g.SelectMany(h => h.ChiTietHds).Sum(ct => ct.SoLuong * ct.DonGia)
                    })
                    .OrderByDescending(x => x.TongTien)
                    .Take(5)
                    .ToListAsync()
            };

            return View(stats);
        }

        // API: Get Stats for Dashboard
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var totalOrders = await _context.HoaDons.CountAsync();
            var pendingOrders = await _context.HoaDons.CountAsync(h => h.MaTrangThai == 0);
            var processingOrders = await _context.HoaDons.CountAsync(h => h.MaTrangThai == 1);
            var completedOrders = await _context.HoaDons.CountAsync(h => h.MaTrangThai == 2);

            var totalRevenue = await _context.HoaDons
                .Where(h => h.MaTrangThai == 2)
                .SelectMany(h => h.ChiTietHds)
                .SumAsync(ct => ct.SoLuong * ct.DonGia);

            var stats = new
            {
                totalOrders,
                pendingOrders,
                processingOrders,
                completedOrders,
                totalRevenue = $"{totalRevenue:N0}₫"
            };

            return Json(stats);
        }

        // GET: /AdminOrder/Print/{id}
        public async Task<IActionResult> Print(int id)
        {
            var order = await _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.ChiTietHds)
                    .ThenInclude(ct => ct.MaHhNavigation)
                .FirstOrDefaultAsync(h => h.MaHd == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}