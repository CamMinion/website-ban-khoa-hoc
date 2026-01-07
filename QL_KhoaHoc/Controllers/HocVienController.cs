using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QL_KhoaHoc.Models;
using System.Text;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace QL_KhoaHoc.Controllers
{

    public class HocVienController : Controller
    {
        private readonly string _apiBaseUrl = "http://localhost:5105/api/";
        private readonly IConfiguration _configuration;  // Inject để đọc config

        public HocVienController(IConfiguration configuration)  // Constructor để inject
        {
            _configuration = configuration;
        }
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // 1. Lấy MaHV từ Session (đã lưu khi Login)
            var maHV = HttpContext.Session.GetInt32("MaHV");

            // Nếu chưa đăng nhập hoặc không phải học viên -> Đá về Login
            if (maHV == null)
            {
                return RedirectToAction("Login");
            }

            HocVien model = null;

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);

                // 2. Gọi API lấy thông tin chi tiết (Bạn cần viết API này ở Backend)
                // Giả sử đường dẫn API là: api/HocVien/GetProfile/{maHV}
                HttpResponseMessage response = await httpClient.GetAsync($"HocVien/GetProfile/{maHV}");

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    model = JsonConvert.DeserializeObject<HocVien>(data);
                }
            }

            // Nếu API lỗi hoặc không tìm thấy data, khởi tạo model rỗng để tránh lỗi View
            if (model == null)
            {
                TempData["ErrorMessage"] = "Không thể tải thông tin học viên.";
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }


        // 1. Action Gửi OTP (ĐÃ SỬA: Thêm check trùng Email)
        [HttpPost]
        public async Task<IActionResult> SendOtpForEmailChange(string newEmail)
        {
            if (string.IsNullOrEmpty(newEmail))
            {
                return Json(new { success = false, message = "Vui lòng nhập email mới." });
            }

            // Lấy email hiện tại để đảm bảo không check trùng với chính mình (nếu cần)
            string currentEmail = HttpContext.Session.GetString("Email");
            if (newEmail.Equals(currentEmail, StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Email mới không được trùng với email hiện tại." });
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);

                // =================================================================
                // [MỚI] BƯỚC 1: GỌI API KIỂM TRA EMAIL ĐÃ TỒN TẠI CHƯA
                // =================================================================
                var checkRes = await httpClient.GetAsync($"Account/CheckEmailExists?email={newEmail}");
                if (checkRes.IsSuccessStatusCode)
                {
                    var checkData = await checkRes.Content.ReadAsStringAsync();
                    var checkResult = JsonConvert.DeserializeObject<dynamic>(checkData);

                    // Nếu API trả về exists = true nghĩa là Email đã có người dùng
                    if ((bool)checkResult.exists)
                    {
                        return Json(new { success = false, message = "Email này đã được sử dụng bởi tài khoản khác." });
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Lỗi kiểm tra email server." });
                }
                // =================================================================

                // BƯỚC 2: GỬI EMAIL OTP (Code cũ giữ nguyên)
                var payload = new { Email = newEmail };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("Account/SendOTP", content);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(data);
                    string otp = result.otp;

                    // Lưu OTP
                    HttpContext.Session.SetString("EmailUpdateOTP", otp);
                    HttpContext.Session.SetString("PendingNewEmail", newEmail);

                    return Json(new { success = true, message = "Mã OTP đã được gửi đến email mới." });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể gửi email. Vui lòng thử lại." });
                }
            }
        }


        // 2. Cập nhật Action UpdateProfile (Kiểm tra OTP)
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(HocVien model, string otpInput)
        {
            var maHV = HttpContext.Session.GetInt32("MaHV");
            if (maHV == null) return RedirectToAction("Login", "Account");

            // Lấy thông tin cũ để so sánh
            string currentEmail = HttpContext.Session.GetString("Email"); // Email hiện tại trong session
            string emailToUpdate = model.Email;

            // LOGIC KIỂM TRA OTP NẾU ĐỔI EMAIL
            if (model.Email != currentEmail)
            {
                // 1. Kiểm tra xem người dùng có nhập OTP không
                if (string.IsNullOrEmpty(otpInput))
                {
                    TempData["ErrorMessage"] = "Bạn đã thay đổi email, vui lòng nhập mã OTP xác thực.";
                    return RedirectToAction("Profile");
                }

                // 2. Lấy OTP và Email đang chờ từ Session
                string sessionOTP = HttpContext.Session.GetString("EmailUpdateOTP");
                string sessionPendingEmail = HttpContext.Session.GetString("PendingNewEmail");

                // 3. So sánh
                if (sessionOTP != otpInput || sessionPendingEmail != model.Email)
                {
                    TempData["ErrorMessage"] = "Mã OTP không chính xác hoặc Email đã bị thay đổi.";
                    return RedirectToAction("Profile");
                }

                // Nếu đúng OTP -> Cho phép cập nhật -> Xóa Session OTP
                HttpContext.Session.Remove("EmailUpdateOTP");
                HttpContext.Session.Remove("PendingNewEmail");
            }

            // --- (Phần code gọi API Update cũ giữ nguyên) ---
            var updateModel = new
            {
                MaHV = maHV.Value,
                SoDienThoai = model.SoDienThoai,
                Email = model.Email
            };

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                var content = new StringContent(JsonConvert.SerializeObject(updateModel), Encoding.UTF8, "application/json");
                var response = await httpClient.PutAsync("HocVien/UpdateProfile", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseContent);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    // Cập nhật lại Session Email chính thức
                    HttpContext.Session.SetString("Email", model.Email);
                }
                else
                {
                    TempData["ErrorMessage"] = result.message ?? "Cập nhật thất bại.";
                }
            }

            return RedirectToAction("Profile");
        }


        // Action xử lý upload ảnh (Gọi bằng Ajax)
        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile fileAvatar)
        {
            var maHV = HttpContext.Session.GetInt32("MaHV");
            if (maHV == null) return Json(new { success = false, message = "Vui lòng đăng nhập." });

            if (fileAvatar == null || fileAvatar.Length == 0)
            {
                return Json(new { success = false, message = "Chưa chọn file ảnh." });
            }

            try
            {
                // 1. Upload lên Cloudinary
                var cloudName = _configuration["Cloudinary:CloudName"];
                var apiKey = _configuration["Cloudinary:ApiKey"];
                var apiSecret = _configuration["Cloudinary:ApiSecret"];
                var cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));

                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(fileAvatar.FileName, fileAvatar.OpenReadStream()),
                    Folder = "user-avatars", // Thư mục trên Cloudinary
                    Transformation = new Transformation().Width(300).Height(300).Crop("fill") // Cắt ảnh vuông 300x300
                };

                var uploadResult = await cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string newAvatarUrl = uploadResult.SecureUri.ToString();

                    // 2. Gọi API cập nhật Database
                    var updateModel = new
                    {
                        MaHV = maHV.Value,
                        AnhDaiDien = newAvatarUrl // Chỉ gửi trường cần update
                    };

                    using (var httpClient = new HttpClient())
                    {
                        httpClient.BaseAddress = new Uri(_apiBaseUrl);
                        var content = new StringContent(JsonConvert.SerializeObject(updateModel), Encoding.UTF8, "application/json");

                        var response = await httpClient.PutAsync("HocVien/UpdateProfile", content);

                        if (response.IsSuccessStatusCode)
                        {
                            // Cập nhật Session để hiển thị ngay trên Header
                            HttpContext.Session.SetString("AnhDaiDien", newAvatarUrl);

                            return Json(new { success = true, imageUrl = newAvatarUrl, message = "Đổi ảnh đại diện thành công!" });
                        }
                    }
                }

                return Json(new { success = false, message = "Lỗi khi lưu vào CSDL." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }
    }
}
