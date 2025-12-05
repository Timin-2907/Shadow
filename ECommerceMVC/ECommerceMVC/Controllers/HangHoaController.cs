using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMVC.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly ShoeContext db;

        public HangHoaController(ShoeContext context)
        {
            db = context;
        }

        public IActionResult Index(int? loai, string? search, string? sort)
        {
            var hangHoas = db.HangHoas.AsQueryable();

            // Lọc theo loại
            if (loai.HasValue)
            {
                hangHoas = hangHoas.Where(p => p.MaLoai == loai.Value);
            }

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                hangHoas = hangHoas.Where(p => p.TenHh.Contains(search));
            }

            // Sắp xếp
            if (!string.IsNullOrEmpty(sort))
            {
                switch (sort)
                {
                    case "name-asc":
                        hangHoas = hangHoas.OrderBy(p => p.TenHh);
                        break;
                    case "name-desc":
                        hangHoas = hangHoas.OrderByDescending(p => p.TenHh);
                        break;
                    case "price-asc":
                        hangHoas = hangHoas.OrderBy(p => p.DonGia);
                        break;
                    case "price-desc":
                        hangHoas = hangHoas.OrderByDescending(p => p.DonGia);
                        break;
                }
            }

            var result = hangHoas.Select(p => new HangHoaVM
            {
                MaHh = p.MaHh,
                TenHH = p.TenHh,
                DonGia = p.DonGia ?? 0,
                Hinh = p.Hinh ?? "",
                MoTaNgan = p.MoTaDonVi ?? "",
                TenLoai = p.MaLoaiNavigation.TenLoai
            });

            return View(result);
        }

        public IActionResult Search(string? query)
        {
            var hangHoas = db.HangHoas.AsQueryable();

            if (query != null)
            {
                hangHoas = hangHoas.Where(p => p.TenHh.Contains(query));
            }

            var result = hangHoas.Select(p => new HangHoaVM
            {
                MaHh = p.MaHh,
                TenHH = p.TenHh,
                DonGia = p.DonGia ?? 0,
                Hinh = p.Hinh ?? "",
                MoTaNgan = p.MoTaDonVi ?? "",
                TenLoai = p.MaLoaiNavigation.TenLoai
            });

            return View(result);
        }

        public IActionResult Detail(int id)
        {
            var data = db.HangHoas
                .Include(p => p.MaLoaiNavigation)
                .SingleOrDefault(p => p.MaHh == id);

            if (data == null)
            {
                TempData["Message"] = $"Không thấy sản phẩm có mã {id}";
                return Redirect("/404");
            }

            var result = new ChiTietHangHoaVM
            {
                MaHh = data.MaHh,
                TenHH = data.TenHh,
                DonGia = data.DonGia ?? 0,
                ChiTiet = data.MoTa ?? string.Empty,
                Hinh = data.Hinh ?? string.Empty,
                MoTaNgan = data.MoTaDonVi ?? string.Empty,
                TenLoai = data.MaLoaiNavigation.TenLoai,
                SoLuongTon = 10, //tính sau
                DiemDanhGia = 5, //check sau
            };

            return View(result);
        }
    }
}