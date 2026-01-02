using ECommerceMVC.ViewModels;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

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
                var message = new MimeMessage();

                // From
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];
                message.From.Add(new MailboxAddress(fromName, fromEmail));

                // To
                message.To.Add(new MailboxAddress(customerName, toEmail));

                // Subject
                message.Subject = $"🎉 Ưu đãi đặc biệt: {promotion.ProductName}";

                // Body HTML
                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = GenerateProductPromotionHTML(customerName, promotion);

                message.Body = bodyBuilder.ToMessageBody();

                // Send via SMTP
                using (var client = new SmtpClient())
                {
                    var smtpServer = _configuration["EmailSettings:SmtpServer"];
                    var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                    var smtpUser = _configuration["EmailSettings:SmtpUser"];
                    var smtpPassword = _configuration["EmailSettings:SmtpPassword"];

                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(smtpUser, smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"✅ Email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error sending email to {toEmail}: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private string GenerateProductPromotionHTML(string customerName, EmailProductPromotion promotion)
        {
            var discountText = promotion.DiscountPercent > 0
                ? $"<span style='background: #ff4757; color: white; padding: 5px 15px; border-radius: 20px; font-weight: bold;'>-{promotion.DiscountPercent}%</span>"
                : "";

            var voucherSection = !string.IsNullOrEmpty(promotion.VoucherCode)
                ? $@"
                    <div style='background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 5px;'>
                        <h3 style='color: #856404; margin: 0 0 10px 0;'>🎟️ Mã ưu đãi đặc biệt</h3>
                        <div style='background: white; padding: 10px; border: 2px dashed #ffc107; border-radius: 5px; text-align: center;'>
                            <code style='font-size: 24px; font-weight: bold; color: #856404; letter-spacing: 2px;'>{promotion.VoucherCode}</code>
                        </div>
                        <p style='margin: 10px 0 0 0; color: #856404; font-size: 14px;'>
                            Sao chép mã này và nhập khi thanh toán để nhận ưu đãi!
                        </p>
                    </div>
                "
                : "";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <table role='presentation' style='width: 100%; border-collapse: collapse;'>
        <tr>
            <td style='padding: 20px 0;'>
                <table role='presentation' style='width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                    
                    <!-- Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center;'>
                            <h1 style='color: white; margin: 0; font-size: 28px;'>🎁 Ưu Đãi Đặc Biệt Dành Cho Bạn!</h1>
                        </td>
                    </tr>

                    <!-- Greeting -->
                    <tr>
                        <td style='padding: 30px 30px 20px 30px;'>
                            <p style='font-size: 16px; color: #333; margin: 0;'>
                                Xin chào <strong>{customerName}</strong>,
                            </p>
                            <p style='font-size: 16px; color: #555; margin: 15px 0 0 0;'>
                                Chúng tôi có một sản phẩm tuyệt vời muốn giới thiệu đến bạn với mức giá đặc biệt!
                            </p>
                        </td>
                    </tr>

                    <!-- Product -->
                    <tr>
                        <td style='padding: 0 30px 30px 30px;'>
                            <table role='presentation' style='width: 100%; border: 2px solid #e0e0e0; border-radius: 10px; overflow: hidden;'>
                                <tr>
                                    <td style='padding: 20px; text-align: center;'>
                                        <img src='{promotion.ProductImage}' alt='{promotion.ProductName}' style='max-width: 250px; height: auto; border-radius: 8px;' />
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding: 0 20px 20px 20px;'>
                                        <h2 style='color: #333; margin: 0 0 10px 0; font-size: 22px;'>{promotion.ProductName}</h2>
                                        <p style='color: #666; margin: 0 0 15px 0; font-size: 14px;'>{promotion.ShortDescription}</p>
                                        
                                        <div style='margin: 15px 0;'>
                                            {discountText}
                                        </div>

                                        <div style='margin: 15px 0;'>
                                            {(promotion.DiscountPercent > 0
                                                ? $"<p style='margin: 0; color: #999; text-decoration: line-through;'>Giá gốc: {promotion.OriginalPrice:N0}₫</p>"
                                                : "")}
                                            <p style='margin: 5px 0 0 0;'>
                                                <span style='font-size: 32px; font-weight: bold; color: #ff4757;'>{promotion.SalePrice:N0}₫</span>
                                            </p>
                                        </div>

                                        {voucherSection}

                                        <div style='text-align: center; margin-top: 25px;'>
                                            <a href='{promotion.ProductUrl}' style='display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 40px; text-decoration: none; border-radius: 25px; font-weight: bold; font-size: 16px;'>
                                                🛒 Mua Ngay
                                            </a>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f8f9fa; padding: 20px 30px; text-align: center; border-top: 1px solid #e0e0e0;'>
                            <p style='margin: 0; color: #666; font-size: 14px;'>
                                © 2025 ECommerceMVC Store. All rights reserved.
                            </p>
                            <p style='margin: 10px 0 0 0; color: #999; font-size: 12px;'>
                                Bạn nhận được email này vì đã đăng ký nhận thông tin từ chúng tôi.
                            </p>
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
</body>
</html>
            ";
        }

        public async Task<bool> SendVoucherEmail(string toEmail, string customerName, string voucherCode, decimal discountPercent)
        {
            try
            {
                var message = new MimeMessage();

                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress(customerName, toEmail));
                message.Subject = $"🎟️ Mã giảm giá {discountPercent}% dành riêng cho bạn!";

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2>Xin chào {customerName}!</h2>
                        <p>Chúng tôi gửi bạn mã giảm giá đặc biệt:</p>
                        <div style='background: #f8f9fa; padding: 20px; text-align: center; border-radius: 10px;'>
                            <h1 style='color: #667eea; font-size: 36px; margin: 0;'>{voucherCode}</h1>
                            <p style='color: #666; margin: 10px 0 0 0;'>Giảm {discountPercent}%</p>
                        </div>
                        <p style='margin-top: 20px;'>Sử dụng mã này khi thanh toán để nhận ưu đãi!</p>
                    </div>
                ";

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    var smtpServer = _configuration["EmailSettings:SmtpServer"];
                    var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                    var smtpUser = _configuration["EmailSettings:SmtpUser"];
                    var smtpPassword = _configuration["EmailSettings:SmtpPassword"];

                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(smtpUser, smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"✅ Voucher email sent to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error sending voucher email to {toEmail}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendOrderConfirmationEmail(string toEmail, string customerName, int orderId)
        {
            try
            {
                var message = new MimeMessage();

                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress(customerName, toEmail));
                message.Subject = $"✅ Xác nhận đơn hàng #{orderId}";

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2>Xin chào {customerName}!</h2>
                        <p>Cảm ơn bạn đã đặt hàng. Đơn hàng #{orderId} của bạn đã được xác nhận.</p>
                        <p>Chúng tôi sẽ liên hệ với bạn sớm nhất!</p>
                    </div>
                ";

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    var smtpServer = _configuration["EmailSettings:SmtpServer"];
                    var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                    var smtpUser = _configuration["EmailSettings:SmtpUser"];
                    var smtpPassword = _configuration["EmailSettings:SmtpPassword"];

                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(smtpUser, smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation($"✅ Order confirmation sent to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error sending order confirmation to {toEmail}: {ex.Message}");
                return false;
            }
        }
    }
}