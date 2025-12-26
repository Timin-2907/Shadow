using ECommerceMVC.ViewModels;

namespace ECommerceMVC.Services
{
    public interface IEmailService
    {
        Task<bool> SendProductPromotionEmail(string toEmail, string toName, EmailProductPromotion promotion);
        Task<bool> SendVoucherEmail(string toEmail, string toName, string voucherCode, decimal discountPercent, DateTime expiryDate);
        Task<bool> SendBulkPromotionEmail(List<string> emails, EmailProductPromotion promotion);
    }

    // ViewModel cho email promotion
    public class EmailProductPromotion
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string ShortDescription { get; set; }
        public string ProductUrl { get; set; }
        public string VoucherCode { get; set; }
        public decimal DiscountPercent { get; set; }
    }
}