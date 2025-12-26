namespace ECommerceMVC.ViewModels
{
    public class ProductSEOViewModel
    {
        public int MaHH { get; set; }
        public string TenHH { get; set; }
        public string Slug { get; set; }
        public string MoTa { get; set; }
        public string MoTaNgan { get; set; }
        public decimal DonGia { get; set; }
        public string Hinh { get; set; }
        public string TenLoai { get; set; }
        public int SoLuongTon { get; set; }

        // SEO Properties
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string CanonicalUrl { get; set; }

        // Open Graph (Facebook, LinkedIn, etc.)
        public string OgTitle { get; set; }
        public string OgDescription { get; set; }
        public string OgImage { get; set; }
        public string OgUrl { get; set; }
        public string OgType { get; set; } = "product";

        // Twitter Card
        public string TwitterCard { get; set; } = "summary_large_image";
        public string TwitterTitle { get; set; }
        public string TwitterDescription { get; set; }
        public string TwitterImage { get; set; }

        // Product Schema.org
        public string ProductSchema { get; set; }
    }
}