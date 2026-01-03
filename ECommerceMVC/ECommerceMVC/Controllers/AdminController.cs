using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ECommerceMVC.Data;
using ECommerceMVC.Helpers;
using ECommerceMVC.ViewModels;

namespace ECommerceMVC.Controllers
{
    [AuthorizeRole("Admin")]  // ✅ CHỈ CHO PHÉP ADMIN TRUY CẬP
    public class AdminController : Controller
    {
        private readonly ShoeContext _context;

        public AdminController(ShoeContext context)
        {
            _context = context;
        }

        // GET: /Admin - Dashboard chính
        public async Task<IActionResult> Index()
        {
            return View();
        }

        // API: Lấy thống kê cho Dashboard
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var totalProducts = await _context.HangHoas.CountAsync();
            var totalCategories = await _context.Loais.CountAsync();
            var totalSuppliers = await _context.NhaCungCaps.CountAsync();

            // Tính tổng giá trị kho hàng
            var totalInventoryValue = await _context.HangHoas
                .SumAsync(h => (h.DonGia ?? 0));

            var stats = new
            {
                totalProducts = totalProducts,
                totalCategories = totalCategories,
                totalSuppliers = totalSuppliers,
                totalInventoryValue = $"{totalInventoryValue:N0}₫"
            };

            return Json(stats);
        }

        // GET: /Admin/Products - Danh sách sản phẩm
        public async Task<IActionResult> Products(string? search, int? categoryId)
        {
            var query = _context.HangHoas
                .Include(h => h.MaLoaiNavigation)
                .Include(h => h.MaNccNavigation)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(h => h.TenHh.Contains(search));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(h => h.MaLoai == categoryId.Value);
            }

            var products = await query
                .OrderByDescending(h => h.MaHh)
                .ToListAsync();

            // Lấy danh sách danh mục cho filter
            ViewBag.Categories = await _context.Loais
                .Select(l => new SelectListItem
                {
                    Value = l.MaLoai.ToString(),
                    Text = l.TenLoai
                })
                .ToListAsync();

            return View(products);
        }

        // GET: /Admin/Categories - Danh sách danh mục
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Loais
                .Include(l => l.HangHoas) // Để đếm số sản phẩm
                .OrderBy(l => l.TenLoai)
                .ToListAsync();

            return View(categories);
        }

        // GET: /Admin/Suppliers - Danh sách nhà cung cấp
        public async Task<IActionResult> Suppliers()
        {
            var suppliers = await _context.NhaCungCaps
                .Include(n => n.HangHoas) // Để đếm số sản phẩm
                .OrderBy(n => n.TenCongTy)
                .ToListAsync();

            return View(suppliers);
        }

        // API: Xóa sản phẩm (AJAX)
        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.HangHoas.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                _context.HangHoas.Remove(product);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa sản phẩm thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // API: Xóa danh mục (AJAX)
        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Loais.FindAsync(id);
                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục" });
                }

                // Kiểm tra xem có sản phẩm nào thuộc danh mục này không
                var hasProducts = await _context.HangHoas.AnyAsync(h => h.MaLoai == id);
                if (hasProducts)
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục đang có sản phẩm" });
                }

                _context.Loais.Remove(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa danh mục thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // API: Xóa nhà cung cấp (AJAX)
        [HttpPost]
        public async Task<IActionResult> DeleteSupplier(string id)
        {
            try
            {
                var supplier = await _context.NhaCungCaps.FindAsync(id);
                if (supplier == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy nhà cung cấp" });
                }

                // Kiểm tra xem có sản phẩm nào của nhà cung cấp này không
                var hasProducts = await _context.HangHoas.AnyAsync(h => h.MaNcc == id);
                if (hasProducts)
                {
                    return Json(new { success = false, message = "Không thể xóa nhà cung cấp đang có sản phẩm" });
                }

                _context.NhaCungCaps.Remove(supplier);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa nhà cung cấp thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}