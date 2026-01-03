using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerceMVC.Data;
using ECommerceMVC.Helpers;

namespace ECommerceMVC.Controllers
{
    [AuthorizeRole("Staff", "Admin")]  // ✅ Cho phép Staff và Admin truy cập
    public class StaffController : Controller
    {
        private readonly ShoeContext _context;

        public StaffController(ShoeContext context)
        {
            _context = context;
        }

        // ====================================
        // DASHBOARD CHÍNH
        // ====================================
        public IActionResult Index()
        {
            var hoTen = HttpContext.Session.GetString("HoTen");
            ViewBag.HoTen = hoTen ?? "Staff";

            // Đếm số đơn hàng theo trạng thái
            var stats = new
            {
                PendingOrders = _context.HoaDons.Count(h => h.MaTrangThai == 0),
                ProcessingOrders = _context.HoaDons.Count(h => h.MaTrangThai == 1),
                CompletedOrders = _context.HoaDons.Count(h => h.MaTrangThai == 2)
            };

            return View(stats);
        }

        // ====================================
        // QUẢN LÝ ĐỚN HÀNG
        // ====================================

        // Danh sách tất cả đơn hàng
        public async Task<IActionResult> DonHang(int? trangThai)
        {
            var query = _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .AsQueryable();

            // Lọc theo trạng thái nếu có
            if (trangThai.HasValue)
            {
                query = query.Where(h => h.MaTrangThai == trangThai.Value);
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
                    h.CachThanhToan,
                    h.MaTrangThai,
                    TrangThai = h.MaTrangThaiNavigation.TenTrangThai,
                    TongTien = h.ChiTietHds.Sum(ct => ct.SoLuong * ct.DonGia),
                    SoLuong = h.ChiTietHds.Sum(ct => ct.SoLuong)
                })
                .ToListAsync();

            // Lấy danh sách trạng thái
            ViewBag.TrangThaiList = await _context.TrangThais.ToListAsync();
            ViewBag.TrangThaiSelected = trangThai;

            return View(orders);
        }

        // Chi tiết đơn hàng
        public async Task<IActionResult> ChiTiet(int id)
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
                return RedirectToAction("DonHang");
            }

            // Lấy danh sách trạng thái để hiển thị trong dropdown
            ViewBag.TrangThaiList = await _context.TrangThais.ToListAsync();

            return View(order);
        }

        // Cập nhật trạng thái đơn hàng
        [HttpPost]
        public async Task<IActionResult> CapNhatTrangThai(int id, int trangThai, string? ghiChu)
        {
            try
            {
                var order = await _context.HoaDons.FindAsync(id);
                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng!";
                    return RedirectToAction("DonHang");
                }

                // Cập nhật trạng thái
                order.MaTrangThai = trangThai;

                // Cập nhật ngày giao nếu hoàn thành
                if (trangThai == 2) // Giả sử 2 là "Đã giao"
                {
                    order.NgayGiao = DateTime.Now;
                }

                // Thêm ghi chú nếu có
                if (!string.IsNullOrEmpty(ghiChu))
                {
                    var timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    var staffName = HttpContext.Session.GetString("HoTen");
                    var newNote = $"[{timestamp}] {staffName}: {ghiChu}";

                    order.GhiChu = string.IsNullOrEmpty(order.GhiChu)
                        ? newNote
                        : order.GhiChu + "\n" + newNote;
                }

                // Lưu nhân viên xử lý
                var maNv = HttpContext.Session.GetString("MaKh");
                order.MaNv = maNv;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("ChiTiet", new { id = id });
        }

        // In hóa đơn
        public async Task<IActionResult> InHoaDon(int id)
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

        // ====================================
        // TIN NHẮN / HỖ TRỢ KHÁCH HÀNG
        // ====================================

        public async Task<IActionResult> TinNhan()
        {
            // Lấy danh sách đơn hàng có ghi chú hoặc yêu cầu hỗ trợ
            var orders = await _context.HoaDons
                .Include(h => h.MaKhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Where(h => !string.IsNullOrEmpty(h.GhiChu))
                .OrderByDescending(h => h.NgayDat)
                .Take(50)
                .ToListAsync();

            return View(orders);
        }

        // Trả lời yêu cầu hỗ trợ
        [HttpPost]
        public async Task<IActionResult> TraLoiYeuCau(int id, string noiDung)
        {
            try
            {
                var order = await _context.HoaDons.FindAsync(id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                var timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                var staffName = HttpContext.Session.GetString("HoTen");
                var reply = $"[{timestamp}] {staffName} trả lời: {noiDung}";

                order.GhiChu = string.IsNullOrEmpty(order.GhiChu)
                    ? reply
                    : order.GhiChu + "\n" + reply;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã gửi phản hồi thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi: " + ex.Message });
            }
        }

        // ====================================
        // THỐNG KÊ
        // ====================================

        public async Task<IActionResult> ThongKe()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var stats = new
            {
                // Đơn hàng hôm nay
                OrdersToday = await _context.HoaDons
                    .CountAsync(h => h.NgayDat.Date == today),

                // Đơn hàng tháng này
                OrdersThisMonth = await _context.HoaDons
                    .CountAsync(h => h.NgayDat >= thisMonth),

                // Doanh thu tháng này
                RevenueThisMonth = await _context.HoaDons
                    .Where(h => h.NgayDat >= thisMonth && h.MaTrangThai == 2)
                    .SelectMany(h => h.ChiTietHds)
                    .SumAsync(ct => ct.SoLuong * ct.DonGia),

                // Đơn hàng đang xử lý
                PendingOrders = await _context.HoaDons
                    .CountAsync(h => h.MaTrangThai == 0 || h.MaTrangThai == 1)
            };

            return View(stats);
        }
    }
}