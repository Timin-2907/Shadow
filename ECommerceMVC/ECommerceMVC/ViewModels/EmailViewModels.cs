using System.ComponentModel.DataAnnotations;

namespace ECommerceMVC.ViewModels
{
    public class SubscriberViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string HoTen { get; set; } = "";
        public string MaKh { get; set; } = "";
    }

    public class SendProductEmailVM
    {
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn sản phẩm")]
        public int ProductId { get; set; }

        [Range(0, 100, ErrorMessage = "Giảm giá từ 0-100%")]
        public decimal DiscountPercent { get; set; }

        public string? VoucherCode { get; set; }

        public bool SendToAll { get; set; }

        public bool SendToSubscribersOnly { get; set; }

        // ✅ CHỈ GIỮ 1 FIELD - JavaScript sẽ merge checkboxes vào đây
        public string EmailList { get; set; } = "";
    }

    public class SendVoucherEmailVM
    {
        public string VoucherCode { get; set; } = "";
        public decimal DiscountPercent { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public bool SendToAll { get; set; }
        public string EmailList { get; set; } = "";
    }

    public class EmailProductPromotion
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductImage { get; set; } = "";
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string ShortDescription { get; set; } = "";
        public string ProductUrl { get; set; } = "";
        public string? VoucherCode { get; set; }
        public decimal DiscountPercent { get; set; }
    }
}