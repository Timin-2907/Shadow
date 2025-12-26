using MailKit.Net.Smtp;
using MimeKit;
using ECommerceMVC.ViewModels;

namespace ECommerceMVC.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPassword;

        public EmailService(IConfiguration config)
        {
            _config = config;
            _fromEmail = _config["EmailSettings:FromEmail"];
            _fromName = _config["EmailSettings:FromName"];
            _smtpServer = _config["EmailSettings:SmtpServer"];
            _smtpPort = int.Parse(_config["EmailSettings:SmtpPort"]);
            _smtpUser = _config["EmailSettings:SmtpUser"];
            _smtpPassword = _config["EmailSettings:SmtpPassword"];
        }

        public async Task<bool> SendProductPromotionEmail(string toEmail, string toName, EmailProductPromotion promotion)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_fromName, _fromEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = $"🎉 Ưu đãi đặc biệt: {promotion.ProductName}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = GeneratePromotionEmailHtml(promotion, toName)
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_smtpUser, _smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendVoucherEmail(string toEmail, string toName, string voucherCode, decimal discountPercent, DateTime expiryDate)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_fromName, _fromEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = $"🎁 Voucher {discountPercent}% dành riêng cho bạn!";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = GenerateVoucherEmailHtml(toName, voucherCode, discountPercent, expiryDate)
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_smtpServer, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_smtpUser, _smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendBulkPromotionEmail(List<string> emails, EmailProductPromotion promotion)
        {
            var tasks = emails.Select(email => SendProductPromotionEmail(email, "Khách hàng", promotion));
            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }

        private string GeneratePromotionEmailHtml(EmailProductPromotion promotion, string customerName)
        {
            var discountAmount = promotion.OriginalPrice - promotion.SalePrice;
            var baseUrl = _config["AppSettings:BaseUrl"] ?? "https://localhost:7148";
            var unsubscribeUrl = $"{baseUrl}/Newsletter/Unsubscribe?token={{TOKEN}}";

            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 20px auto; background: white; border-radius: 10px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 30px; }}
        .greeting {{ font-size: 18px; margin-bottom: 20px; color: #333; }}
        .product-card {{ border: 2px solid #e0e0e0; border-radius: 10px; padding: 20px; margin: 20px 0; }}
        .product-image {{ width: 100%; max-width: 400px; height: auto; border-radius: 8px; margin-bottom: 20px; display: block; margin-left: auto; margin-right: auto; }}
        .product-name {{ font-size: 24px; font-weight: bold; color: #333; margin-bottom: 10px; }}
        .product-description {{ color: #666; margin-bottom: 15px; line-height: 1.6; }}
        .price-section {{ background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0; }}
        .original-price {{ text-decoration: line-through; color: #999; font-size: 18px; }}
        .sale-price {{ color: #e74c3c; font-size: 32px; font-weight: bold; margin: 10px 0; }}
        .discount-badge {{ background: #e74c3c; color: white; padding: 5px 15px; border-radius: 20px; display: inline-block; font-weight: bold; }}
        .voucher-section {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 5px; }}
        .voucher-code {{ font-size: 24px; font-weight: bold; color: #856404; letter-spacing: 2px; text-align: center; padding: 10px; background: white; border: 2px dashed #ffc107; border-radius: 5px; margin: 10px 0; }}
        .cta-button {{ display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 40px; text-decoration: none; border-radius: 50px; font-size: 18px; font-weight: bold; margin: 20px 0; text-align: center; transition: transform 0.3s; }}
        .cta-button:hover {{ transform: translateY(-2px); }}
        .footer {{ background: #333; color: white; text-align: center; padding: 20px; font-size: 14px; }}
        .footer a {{ color: #667eea; text-decoration: none; }}
        .benefits {{ background: #e8f5e9; padding: 15px; border-radius: 8px; margin: 20px 0; }}
        .benefits ul {{ margin: 10px 0; padding-left: 20px; }}
        .benefits li {{ margin: 8px 0; color: #2e7d32; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 ƯU ĐÃI ĐẶC BIỆT</h1>
            <p style='margin: 10px 0 0 0; font-size: 16px;'>Chỉ dành riêng cho bạn!</p>
        </div>
        
        <div class='content'>
            <div class='greeting'>
                Xin chào <strong>{customerName}</strong>,
            </div>
            
            <p style='color: #666; line-height: 1.8;'>
                Chúng tôi rất vui được giới thiệu đến bạn sản phẩm đặc biệt với mức giá ưu đãi chưa từng có! 
                Đừng bỏ lỡ cơ hội sở hữu sản phẩm chất lượng cao với giá tốt nhất.
            </p>

            <div class='product-card'>
                <img src='{promotion.ProductImage}' alt='{promotion.ProductName}' class='product-image' />
                
                <div class='product-name'>{promotion.ProductName}</div>
                <div class='product-description'>{promotion.ShortDescription}</div>
                
                <div class='price-section'>
                    <div class='original-price'>Giá gốc: {promotion.OriginalPrice:N0}₫</div>
                    <div class='sale-price'>{promotion.SalePrice:N0}₫</div>
                    <span class='discount-badge'>Tiết kiệm {discountAmount:N0}₫ ({promotion.DiscountPercent}%)</span>
                </div>

                {(string.IsNullOrEmpty(promotion.VoucherCode) ? "" : $@"
                <div class='voucher-section'>
                    <p style='margin: 0 0 10px 0; font-weight: bold; color: #856404;'>
                        🎁 Sử dụng mã giảm giá để nhận ưu đãi:
                    </p>
                    <div class='voucher-code'>{promotion.VoucherCode}</div>
                    <p style='margin: 10px 0 0 0; font-size: 14px; color: #856404; text-align: center;'>
                        Sao chép mã và áp dụng khi thanh toán
                    </p>
                </div>
                ")}

                <div class='benefits'>
                    <strong style='color: #2e7d32;'>✨ Ưu đãi khi mua hàng:</strong>
                    <ul>
                        <li>Miễn phí vận chuyển toàn quốc</li>
                        <li>Bảo hành chính hãng 12 tháng</li>
                        <li>Đổi trả trong 7 ngày nếu không hài lòng</li>
                        <li>Tích điểm thành viên cho đơn hàng</li>
                    </ul>
                </div>

                <div style='text-align: center;'>
                    <a href='{promotion.ProductUrl}' class='cta-button'>
                        🛒 MUA NGAY
                    </a>
                </div>
            </div>

            <p style='color: #999; font-size: 14px; margin-top: 30px; text-align: center;'>
                ⏰ Ưu đãi có hạn, nhanh tay đặt hàng ngay hôm nay!
            </p>
        </div>

        <div class='footer'>
            <p style='margin: 10px 0;'><strong>ECommerceMVC</strong></p>
            <p style='margin: 5px 0;'>📧 Email: support@ecommercemvc.com</p>
            <p style='margin: 5px 0;'>📞 Hotline: 1900-xxxx</p>
            <p style='margin: 15px 0 5px 0;'>
                <a href='#'>Điều khoản</a> | 
                <a href='#'>Chính sách</a> | 
                <a href='#'>Liên hệ</a>
            </p>
            <p style='margin: 10px 0; font-size: 12px; color: #999;'>
                © 2024 ECommerceMVC. All rights reserved.
            </p>
            <p style='margin: 10px 0; font-size: 11px; color: #999;'>
                Bạn nhận được email này vì đã đăng ký nhận tin từ ECommerceMVC.<br>
                <a href='{unsubscribeUrl}' style='color: #999; text-decoration: underline;'>
                    Hủy đăng ký nhận tin
                </a>
            </p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateVoucherEmailHtml(string customerName, string voucherCode, decimal discountPercent, DateTime expiryDate)
        {
            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 20px auto; background: white; border-radius: 10px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%); color: white; padding: 40px; text-align: center; }}
        .content {{ padding: 40px; }}
        .voucher-card {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 15px; text-align: center; margin: 30px 0; }}
        .voucher-code {{ font-size: 36px; font-weight: bold; letter-spacing: 4px; margin: 20px 0; padding: 20px; background: white; color: #667eea; border-radius: 10px; border: 3px dashed #667eea; }}
        .discount {{ font-size: 48px; font-weight: bold; margin: 20px 0; }}
        .cta-button {{ display: inline-block; background: #ff6b6b; color: white; padding: 15px 40px; text-decoration: none; border-radius: 50px; font-size: 18px; font-weight: bold; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0; font-size: 32px;'>🎁 VOUCHER ĐẶC BIỆT</h1>
        </div>
        <div class='content'>
            <p style='font-size: 18px;'>Xin chào <strong>{customerName}</strong>,</p>
            <p>Chúng tôi gửi tặng bạn voucher giảm giá đặc biệt!</p>
            
            <div class='voucher-card'>
                <div class='discount'>GIẢM {discountPercent}%</div>
                <p style='margin: 10px 0;'>Mã giảm giá của bạn:</p>
                <div class='voucher-code'>{voucherCode}</div>
                <p style='margin: 20px 0 0 0;'>⏰ Có hiệu lực đến: {expiryDate:dd/MM/yyyy}</p>
            </div>

            <div style='text-align: center;'>
                <a href='https://yourwebsite.com/shop' class='cta-button'>🛍️ MUA SẮM NGAY</a>
            </div>

            <p style='color: #999; font-size: 14px; margin-top: 30px;'>
                * Áp dụng cho mọi đơn hàng. Không giới hạn số lần sử dụng trong thời gian có hiệu lực.
            </p>
        </div>
    </div>
</body>
</html>";
        }
    }
}