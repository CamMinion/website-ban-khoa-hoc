
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using QL_KhoaHoc.Models;
using Microsoft.AspNetCore.Http;  

namespace QL_KhoaHoc.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _apiBaseUrl = "http://localhost:5105/api/";

        // GET: Đăng ký form
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

      

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model, bool asGiangVien = false)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Lưu OTP như cũ
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                string json = JsonConvert.SerializeObject(new SendOTPModel { Email = model.Email });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync("Account/SendOTP", content);
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(data);
                    string otp = (string)result.otp;

                    // Lưu model + OTP + asGiangVien vào Session
                    HttpContext.Session.SetString("RegisterModel", JsonConvert.SerializeObject(model));
                    HttpContext.Session.SetString("OTP", otp);
                    HttpContext.Session.SetString("OTPExpiry", DateTime.Now.AddMinutes(5).ToString());
                    HttpContext.Session.SetString("AsGiangVien", asGiangVien.ToString());

                    return RedirectToAction("VerifyOTP");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Lỗi gửi OTP: " + await response.Content.ReadAsStringAsync());
                    return View(model);
                }
            }
        }


        // GET: Xác thực OTP
        [HttpGet]
        public IActionResult VerifyOTP()
        {
            if (HttpContext.Session.GetString("RegisterModel") == null)
            {
                return RedirectToAction("Register");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(VerifyOTPModel model)
        {
            string storedOTP = HttpContext.Session.GetString("OTP");
            string expiryStr = HttpContext.Session.GetString("OTPExpiry");
            bool AsGiangVien = bool.Parse(HttpContext.Session.GetString("AsGiangVien") ?? "false");

            if (string.IsNullOrEmpty(storedOTP) || string.IsNullOrEmpty(expiryStr) || DateTime.Parse(expiryStr) < DateTime.Now)
            {
                ModelState.AddModelError(string.Empty, "OTP hết hạn hoặc không tồn tại.");
                return View(model);
            }

            if (model.OTP != storedOTP)
            {
                ModelState.AddModelError(string.Empty, "OTP sai.");
                return View(model);
            }

            // Gọi API Register
            string registerJson = HttpContext.Session.GetString("RegisterModel");
            var registerModel = JsonConvert.DeserializeObject<RegisterModel>(registerJson);

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                var payload = new
                {
                    TenDN = registerModel.TenDN,
                    Email = registerModel.Email,
                    MatKhau = registerModel.MatKhau,
                    NhapLaiMatKhau = registerModel.NhapLaiMatKhau,
                    AsGiangVien = AsGiangVien
                };

                string json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync("Account/Register", content);

                if (response.IsSuccessStatusCode)
                {
                    HttpContext.Session.Clear();
                    TempData["Success"] = "Đăng ký thành công.";
                    return RedirectToAction("Login");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Lỗi đăng ký: " + await response.Content.ReadAsStringAsync());
                    return View(model);
                }
            }
        }


        // GET: Đăng nhập form
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Đăng nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                string json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync("Account/Login", content);
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(data);
                    // 1. Chuyển đổi giá trị dynamic sang kiểu dữ liệu cụ thể
                    int maTK = (int)result.maTK;
                    string tenDN = result.tenDN.ToString(); // Hoặc (string)result.TenDN

                    // 2. Bây giờ truyền các biến đã được gõ (typed) vào hàm session
                    //    HttpContext (với H viết hoa) là đúng khi ở trong Controller
                    HttpContext.Session.SetInt32("MaTK", maTK);
                    HttpContext.Session.SetString("TenDN", tenDN);


                    // --- ĐOẠN CODE CẦN THÊM VÀO ---
                    // Kiểm tra xem user có phải là Giảng Viên không để lưu vào Session
                    bool laGiangVien = false;
                    using (var clientCheck = new HttpClient())
                    {
                        clientCheck.BaseAddress = new Uri(_apiBaseUrl);
                        var resCheck = await clientCheck.GetAsync($"Account/IsGiangVien/{maTK}");
                        if (resCheck.IsSuccessStatusCode)
                        {
                            string dataCheck = await resCheck.Content.ReadAsStringAsync();
                            var resultCheck = JsonConvert.DeserializeObject<dynamic>(dataCheck);
                            laGiangVien = resultCheck.laGiangVien;
                        }
                    }
                    // Lưu trạng thái giảng viên (1: Có, 0: Không)
                    HttpContext.Session.SetInt32("LaGiangVien", laGiangVien ? 1 : 0);

                    
                    int? maHV = await GetMaHVFromMaTK(maTK);

                    if (maHV.HasValue)
                    {
                        // 3. Lưu MaHV vào Session
                        HttpContext.Session.SetInt32("MaHV", maHV.Value);

                        // =========================================================================
                        // [MỚI] KIỂM TRA: NẾU CHƯA CHỌN SỞ THÍCH -> CHUYỂN HƯỚNG SANG TRANG CHỌN
                        // =========================================================================
                        bool hasInterests = await CheckHasInterests(maHV.Value);
                        if (!hasInterests)
                        {
                            // Lưu lại cờ "BatDau" nếu cần, hoặc bỏ qua nếu không dùng nữa
                            // Chuyển hướng sang trang Chọn Sở Thích
                            return RedirectToAction("ChonSoThich");
                        }
                        // =========================================================================

                        using (var profileClient = new HttpClient())
                        {
                            profileClient.BaseAddress = new Uri(_apiBaseUrl);
                            // Gọi API GetProfile mà bạn đã viết trong HocVienController (API)
                            var profileResponse = await profileClient.GetAsync($"HocVien/GetProfile/{maHV.Value}");

                            if (profileResponse.IsSuccessStatusCode)
                            {
                                var profileData = await profileResponse.Content.ReadAsStringAsync();
                                var profileObj = JsonConvert.DeserializeObject<dynamic>(profileData);

                                // Lấy dữ liệu từ JSON trả về
                                string userEmail = (string)profileObj.email;
                                string userAvatar = (string)profileObj.anhDaiDien;

                                // Lưu vào Session để Layout sử dụng
                                if (!string.IsNullOrEmpty(userEmail))
                                {
                                    HttpContext.Session.SetString("Email", userEmail);
                                }

                                if (!string.IsNullOrEmpty(userAvatar))
                                {
                                    HttpContext.Session.SetString("AnhDaiDien", userAvatar);
                                }
                            }
                        }
                    }
                    int? batDau = HttpContext.Session.GetInt32("BatDau");
                    if(batDau == 1)
                    {
                        // Xóa session "BatDau" sau khi sử dụng
                        HttpContext.Session.Remove("BatDau");
                        return RedirectToAction("Index", "GiangVien");  // Vào trang tạo khóa học
                    }
                    return RedirectToAction("Index", "Home");  // Vào trang chủ
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Đăng nhập thất bại: " + await response.Content.ReadAsStringAsync());
                    return View(model);
                }
            }
        }

        // === HÀM HỖ TRỢ MỚI (để gọi API bạn vừa tạo) ===
        private async Task<int?> GetMaHVFromMaTK(int maTK)
        {
            // (Bạn cần khai báo _apiBaseUrl trong controller này, giống như KhoaHocController)
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);

                // Gọi API mới: api/HocVien/GetHocVienByMaTK/{maTK}
                HttpResponseMessage response = await httpClient.GetAsync($"HocVien/GetHocVienByMaTK/{maTK}");

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    // Đọc kết quả { maHV: 123 } hoặc { maHV: null }
                    var result = JsonConvert.DeserializeObject<dynamic>(data);

                    if (result.maHV != null)
                    {
                        return (int)result.maHV;
                    }
                }
                return null; // Trả về null nếu API lỗi hoặc không tìm thấy
            }
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public async Task<JsonResult> IsGiangVien()
        {
            int? maTK = HttpContext.Session.GetInt32("MaTK"); // Lấy trực tiếp từ session
            if (maTK == null)
                return Json(new { success = false, laGiangVien = false });

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                HttpResponseMessage response = await httpClient.GetAsync($"Account/IsGiangVien/{maTK}");

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(data);
                    bool laGiangVien = result.laGiangVien;
                    return Json(new { success = true, laGiangVien });
                }
                else
                {
                    return Json(new { success = false, laGiangVien = false });
                }
            }
        }

        // --- [PHẦN MỚI] XỬ LÝ SỞ THÍCH ---

        // Hàm hỗ trợ gọi API kiểm tra
        private async Task<bool> CheckHasInterests(int maHV)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                var response = await httpClient.GetAsync($"HocVien/HasInterests/{maHV}");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(data);
                    return (bool)result.hasInterests;
                }
                return false;
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChonSoThich()
        {
            // Lấy danh sách danh mục con từ API để hiển thị checkbox
            List<DanhMucCon> listDanhMuc = new List<DanhMucCon>();
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                var response = await httpClient.GetAsync("DanhMucCon"); // Đảm bảo bạn có API này
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    listDanhMuc = JsonConvert.DeserializeObject<List<DanhMucCon>>(data);
                }
            }
            return View(listDanhMuc);
        }

        [HttpPost]
        public async Task<IActionResult> LuuSoThich(List<int> selectedCategories)
        {
            var maHV = HttpContext.Session.GetInt32("MaHV");
            if (maHV == null) return RedirectToAction("Login");

            if (selectedCategories != null && selectedCategories.Count > 0)
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(_apiBaseUrl);
                    var model = new { MaHV = maHV.Value, MaDMCons = selectedCategories };
                    string json = JsonConvert.SerializeObject(model);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    await httpClient.PostAsync("HocVien/SaveInterests", content);
                }
            }

            // Lưu xong -> Về trang chủ
            return RedirectToAction("Index", "Home");
        }


        // Trong AccountController.cs (Web)

        [HttpGet]
        public async Task<IActionResult> MyCertificates()
        {
            var maHV = HttpContext.Session.GetInt32("MaHV");
            if (maHV == null) return RedirectToAction("Login");

            var listCert = new List<ChungChi>(); // Nhớ khai báo class ViewModel tương tự bên API

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                var response = await httpClient.GetAsync($"HocVien/GetMyCertificates/{maHV}");

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    listCert = JsonConvert.DeserializeObject<List<ChungChi>>(data);
                }
            }

            return View(listCert);
        }

    }
}
