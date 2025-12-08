using System.Text;
using System.Text.RegularExpressions;

namespace ECommerceMVC.Services
{
    public interface ISEOService
    {
        string GenerateSlug(string text);
        string GenerateMetaDescription(string description, int maxLength = 160);
        string GenerateMetaKeywords(params string[] keywords);
    }

    public class SEOService : ISEOService
    {
        // Chuyển đổi tiếng Việt có dấu sang không dấu
        public string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Chuyển về chữ thường
            text = text.ToLowerInvariant();

            // Thay thế ký tự có dấu tiếng Việt
            text = text.Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
                       .Replace("ă", "a").Replace("ắ", "a").Replace("ằ", "a").Replace("ẳ", "a").Replace("ẵ", "a").Replace("ặ", "a")
                       .Replace("â", "a").Replace("ấ", "a").Replace("ầ", "a").Replace("ẩ", "a").Replace("ẫ", "a").Replace("ậ", "a")
                       .Replace("đ", "d")
                       .Replace("é", "e").Replace("è", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
                       .Replace("ê", "e").Replace("ế", "e").Replace("ề", "e").Replace("ể", "e").Replace("ễ", "e").Replace("ệ", "e")
                       .Replace("í", "i").Replace("ì", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
                       .Replace("ó", "o").Replace("ò", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
                       .Replace("ô", "o").Replace("ố", "o").Replace("ồ", "o").Replace("ổ", "o").Replace("ỗ", "o").Replace("ộ", "o")
                       .Replace("ơ", "o").Replace("ớ", "o").Replace("ờ", "o").Replace("ở", "o").Replace("ỡ", "o").Replace("ợ", "o")
                       .Replace("ú", "u").Replace("ù", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
                       .Replace("ư", "u").Replace("ứ", "u").Replace("ừ", "u").Replace("ử", "u").Replace("ữ", "u").Replace("ự", "u")
                       .Replace("ý", "y").Replace("ỳ", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y");

            // Xóa các ký tự đặc biệt, chỉ giữ lại chữ, số và dấu gạch ngang
            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
            
            // Thay thế khoảng trắng bằng dấu gạch ngang
            text = Regex.Replace(text, @"\s+", "-");
            
            // Xóa các dấu gạch ngang liên tiếp
            text = Regex.Replace(text, @"-+", "-");
            
            // Xóa dấu gạch ngang ở đầu và cuối
            text = text.Trim('-');

            return text;
        }

        // Tạo meta description
        public string GenerateMetaDescription(string description, int maxLength = 160)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            // Xóa HTML tags nếu có
            description = Regex.Replace(description, "<.*?>", string.Empty);
            
            // Cắt ngắn nếu quá dài
            if (description.Length > maxLength)
            {
                description = description.Substring(0, maxLength - 3) + "...";
            }

            return description;
        }

        // Tạo meta keywords
        public string GenerateMetaKeywords(params string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
                return string.Empty;

            return string.Join(", ", keywords.Where(k => !string.IsNullOrWhiteSpace(k)));
        }
    }
}