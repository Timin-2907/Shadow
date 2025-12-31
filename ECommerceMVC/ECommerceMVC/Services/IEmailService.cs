using ECommerceMVC.ViewModels;

namespace ECommerceMVC.Services
{
    public interface IEmailService
    {
        Task<bool> SendProductPromotionEmail(string toEmail, string customerName, EmailProductPromotion promotion);
        Task<bool> SendVoucherEmail(string toEmail, string customerName, string voucherCode, decimal discountPercent);
        Task<bool> SendOrderConfirmationEmail(string toEmail, string customerName, int orderId);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendProductPromotionEmail(string toEmail, string customerName, EmailProductPromotion promotion)
        {
            try
            {
                // TODO: Implement actual email sending logic
                // For now, just simulate success
                _logger.LogInformation($"Sending product promotion email to {toEmail}");

                // Simulate delay
                await Task.Delay(100);

                // Return true to simulate success
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending product promotion email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendVoucherEmail(string toEmail, string customerName, string voucherCode, decimal discountPercent)
        {
            try
            {
                _logger.LogInformation($"Sending voucher email to {toEmail}");
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending voucher email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendOrderConfirmationEmail(string toEmail, string customerName, int orderId)
        {
            try
            {
                _logger.LogInformation($"Sending order confirmation email to {toEmail}");
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending order confirmation email: {ex.Message}");
                return false;
            }
        }
    }
}