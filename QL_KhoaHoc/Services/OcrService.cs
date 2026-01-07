using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QL_KhoaHoc.Services // Thay bằng namespace thực tế của bạn
{
    public class OcrService
    {
        private readonly string _credentialPath;

        public OcrService()
        {
            // CÁCH 1: (Khuyên dùng) File nằm ở thư mục gốc của ứng dụng khi chạy (bin/Debug/...)
            // Yêu cầu: Click chuột phải vào file json -> Properties -> Copy to Output Directory: Copy if newer
            ///_credentialPath = Path.Combine(Directory.GetCurrentDirectory(), "google-vision-key.json");
            _credentialPath = @"D:\KhoaLuan\Code\QL_KhoaHoc\QL_KhoaHoc\google-vision-key.json";
            // CÁCH 2: Nếu bạn CỐ TÌNH để file trong thư mục Services và muốn trỏ cứng vào đó (Không khuyến khích)
            // _credentialPath = Path.Combine(Directory.GetCurrentDirectory(), "Services", "google-vision-key.json");

            // Kiểm tra file có tồn tại không để báo lỗi sớm
            if (!File.Exists(_credentialPath))
            {
                throw new FileNotFoundException($"Không tìm thấy file cấu hình Google Vision tại: {_credentialPath}");
            }
        }

        public async Task<string> ExtractNameFromIdCardAsync(IFormFile file)
        {
            try
            {
                // 1. Cấu hình biến môi trường trỏ đến file Key
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _credentialPath);

                // Tạo client kết nối Google Vision
                var client = await ImageAnnotatorClient.CreateAsync();

                // 2. Chuyển IFormFile sang Image của Google
                using var stream = file.OpenReadStream();
                var image = await Image.FromStreamAsync(stream);

                // 3. Gọi API quét text
                var response = await client.DetectTextAsync(image);

                // Kiểm tra kết quả
                if (response == null || response.Count == 0) return null;

                // response[0] chứa toàn bộ văn bản trong ảnh
                string fullText = response[0].Description;

                // [THÊM DÒNG NÀY ĐỂ DEBUG]
                Console.WriteLine("=== LOG OCR GOOGLE ===");
                Console.WriteLine(fullText);
                Console.WriteLine("======================");

                // 4. Phân tích logic để lấy tên (Parsing)
                // Logic: Tìm dòng chứa "Họ và tên", dòng tiếp theo thường là Tên
                var lines = fullText.Split('\n').Select(l => l.Trim()).ToList();

                for (int i = 0; i < lines.Count; i++)
                {
                    string currentLine = lines[i].ToLower();

                    // Tìm các từ khóa nhận diện dòng tiêu đề tên
                    if (currentLine.Contains("họ và tên") ||
                        currentLine.Contains("full name") ||
                        currentLine.Contains("họ tên"))
                    {
                        // Kiểm tra dòng tiếp theo (i + 1)
                        if (i + 1 < lines.Count)
                        {
                            string potentialName = lines[i + 1];

                            // Logic lọc nhiễu:
                            // 1. Tên trên CCCD thường viết HOA TOÀN BỘ
                            // 2. Không chứa số
                            // 3. Độ dài hợp lý (> 3 ký tự)
                            if (IsAllUpper(potentialName) && !potentialName.Any(char.IsDigit) && potentialName.Length > 3)
                            {
                                return potentialName; // Trả về "NGUYỄN VĂN A"
                            }
                        }
                    }
                }

                return null; // Không tìm thấy
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                Console.WriteLine("Lỗi OCR: " + ex.Message);
                return null;
            }
        }

        // Hàm so sánh tên (Logic chính xác)
        public bool CompareNames(string ocrName, string bankName)
        {
            if (string.IsNullOrEmpty(ocrName) || string.IsNullOrEmpty(bankName)) return false;

            // Bước 1: Chuẩn hóa cả 2 chuỗi về dạng chuẩn
            // VIẾT HOA - KHÔNG DẤU - KHÔNG KHOẢNG TRẮNG THỪA
            string cleanOcr = RemoveSign4VietnameseString(ocrName).ToUpper().Trim();
            string cleanBank = RemoveSign4VietnameseString(bankName).ToUpper().Trim();

            // Loại bỏ khoảng trắng kép nếu có (Ví dụ: "NGUYEN  VAN" -> "NGUYEN VAN")
            cleanOcr = System.Text.RegularExpressions.Regex.Replace(cleanOcr, @"\s+", " ");
            cleanBank = System.Text.RegularExpressions.Regex.Replace(cleanBank, @"\s+", " ");

            // Bước 2: So sánh
            // Dùng Contains hoặc Equals
            // Contains giúp xử lý trường hợp OCR đọc thiếu/thừa một chút (ví dụ dấu chấm)
            return cleanOcr == cleanBank;
        }

        // Helper: Kiểm tra chuỗi có phải viết hoa toàn bộ không
        private bool IsAllUpper(string input)
        {
            // Bỏ qua khoảng trắng, các ký tự còn lại phải là chữ hoa
            return input.Where(c => !char.IsWhiteSpace(c))
                        .All(c => !char.IsLetter(c) || char.IsUpper(c));
        }

        // Helper: Bỏ dấu tiếng Việt
        public static string RemoveSign4VietnameseString(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            string[] VietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };

            for (int i = 1; i < VietnameseSigns.Length; i++)
            {
                for (int j = 0; j < VietnameseSigns[i].Length; j++)
                    str = str.Replace(VietnameseSigns[i][j], VietnameseSigns[0][i - 1]);
            }
            return str;
        }
    }
}