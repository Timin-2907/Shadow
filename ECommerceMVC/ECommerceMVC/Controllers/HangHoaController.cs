using ECommerceMVC.Data;
using ECommerceMVC.ViewModels;
using ECommerceMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ECommerceMVC.Controllers
{
    public class HangHoaController : Controller
    {
        private readonly ShoeContext db;
        private readonly ISEOService _seoService;

        public HangHoaController(ShoeContext context, ISEOService seoService)
        {
            db = context;
            _seoService = seoService;
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

        // URL: /san-pham/giay-nike-air-max-123 (SEO-FRIENDLY)
        [Route("san-pham/{slug}-{id:int}")]
        public IActionResult Detail(int id, string slug)
        {
            var data = db.HangHoas
                .Include(p => p.MaLoaiNavigation)
                .SingleOrDefault(p => p.MaHh == id);

            if (data == null)
            {
                TempData["Message"] = $"Không thấy sản phẩm có mã {id}";
                return Redirect("/404");
            }

            // Tạo slug đúng từ tên sản phẩm
            var correctSlug = _seoService.GenerateSlug(data.TenHh);

            // Redirect nếu slug không đúng (SEO canonical)
            if (slug != correctSlug)
            {
                return RedirectToAction("Detail", new { id = id, slug = correctSlug });
            }

            // Tạo URL đầy đủ cho Open Graph
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var productUrl = $"{baseUrl}/san-pham/{correctSlug}-{id}";
            var imageUrl = $"{baseUrl}/Hinh/HangHoa/{data.Hinh}";

            var result = new ProductSEOViewModel
            {
                MaHH = data.MaHh,
                TenHH = data.TenHh,
                Slug = correctSlug,
                MoTa = data.MoTa ?? string.Empty,
                MoTaNgan = _seoService.GenerateMetaDescription(data.MoTa, 160),
                DonGia = data.DonGia ?? 0,
                Hinh = data.Hinh ?? string.Empty,
                TenLoai = data.MaLoaiNavigation.TenLoai,
                SoLuongTon = 10,  // ✅ Hardcode hoặc tính từ database sau

                // SEO Meta Tags
                MetaTitle = $"{data.TenHh} - Giá {data.DonGia:N0}₫",
                MetaDescription = _seoService.GenerateMetaDescription(data.MoTa, 160),
                MetaKeywords = _seoService.GenerateMetaKeywords(
                    data.TenHh,
                    data.MaLoaiNavigation?.TenLoai,
                    "giày", "mua giày online", "giày chính hãng"
                ),
                CanonicalUrl = productUrl,

                // Open Graph (Facebook)
                OgTitle = data.TenHh,
                OgDescription = _seoService.GenerateMetaDescription(data.MoTa, 200),
                OgImage = imageUrl,
                OgUrl = productUrl,
                OgType = "product",

                // Twitter Card
                TwitterCard = "summary_large_image",
                TwitterTitle = data.TenHh,
                TwitterDescription = _seoService.GenerateMetaDescription(data.MoTa, 200),
                TwitterImage = imageUrl,

                // Product Schema.org (JSON-LD)
                ProductSchema = GenerateProductSchema(data, productUrl, imageUrl)
            };

            return View(result);
        }

        // Tạo structured data cho Google
        private string GenerateProductSchema(HangHoa product, string url, string imageUrl)
        {
            var schema = new
            {
                context = "https://schema.org/",
                type = "Product",
                name = product.TenHh,
                image = imageUrl,
                description = _seoService.GenerateMetaDescription(product.MoTa, 200),
                sku = product.MaHh.ToString(),
                brand = new
                {
                    type = "Brand",
                    name = "ECommerceMVC"
                },
                offers = new
                {
                    type = "Offer",
                    url = url,
                    priceCurrency = "VND",
                    price = product.DonGia,
                    availability = "https://schema.org/InStock",  // ✅ Mặc định còn hàng
                    priceValidUntil = DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd")
                }
            };

            return JsonSerializer.Serialize(schema, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }

        // Danh sách sản phẩm theo loại với SEO-friendly URL
        [Route("danh-muc/{slug}-{id:int}")]
        public IActionResult Category(int id, string slug, int page = 1, int pageSize = 12)
        {
            var loai = db.Loais.FirstOrDefault(l => l.MaLoai == id);

            if (loai == null)
            {
                return NotFound();
            }

            var correctSlug = _seoService.GenerateSlug(loai.TenLoai);
            if (slug != correctSlug)
            {
                return RedirectToAction("Category", new { id = id, slug = correctSlug, page = page });
            }

            var products = db.HangHoas
                .Where(h => h.MaLoai == id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new HangHoaVM
                {
                    MaHh = p.MaHh,
                    TenHH = p.TenHh,
                    DonGia = p.DonGia ?? 0,
                    Hinh = p.Hinh ?? "",
                    MoTaNgan = p.MoTaDonVi ?? "",
                    TenLoai = p.MaLoaiNavigation.TenLoai
                })
                .ToList();

            ViewBag.CategoryName = loai.TenLoai;
            ViewBag.CategorySlug = correctSlug;
            ViewBag.CategoryId = id;
            ViewBag.MetaTitle = $"{loai.TenLoai} - Trang {page}";
            ViewBag.MetaDescription = $"Mua {loai.TenLoai} chính hãng, giá tốt nhất. Miễn phí vận chuyển toàn quốc.";

            return View(products);
        }
        // Route cũ - redirect sang route mới với slug
        [HttpGet]
        [Route("HangHoa/Detail/{id:int}")]
        public IActionResult DetailOld(int id)
        {
            var product = db.HangHoas.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            var slug = _seoService.GenerateSlug(product.TenHh);
            return RedirectToAction("Detail", new { id = id, slug = slug });
        }
    }
}