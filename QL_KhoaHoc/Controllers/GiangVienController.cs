using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using QL_KhoaHoc.Models;
using QL_KhoaHoc.Services;
using System.Configuration;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;

namespace QL_KhoaHoc.Controllers
{


  
    public class GiangVienController : Controller
    {
        private readonly OcrService _ocrService; // Khai báo Service
        private readonly IConfiguration _configuration;
        // Inject OcrService vào Constructor
        public GiangVienController(OcrService ocrService, IConfiguration configuration)
        {
            _ocrService = ocrService;
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult DangKyGiangVien()
        {
            return View();
        }
        private readonly string _apiBaseUrl = "http://localhost:5105/api/";
        [HttpPost]
        public async Task<IActionResult> BatDau()
        {

            // ✅ Đặt session = 1 khi nhấn nút "Bắt đầu"
            HttpContext.Session.SetInt32("BatDau", 1);

            int? maTK = HttpContext.Session.GetInt32("MaTK");
            if (maTK == null)
            {
                // Chưa đăng nhập → redirect Login
                return RedirectToAction("Login", "Account");
            }

            bool laGiangVien = false;
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                var response = await httpClient.GetAsync($"Account/IsGiangVien/{maTK}");
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(data);
                    laGiangVien = result.laGiangVien;
                }
            }
           

            if (!laGiangVien)
            {
                // Chưa là giảng viên → insert MATK vào bảng GIANGVIEN
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(_apiBaseUrl);
                    var response = await httpClient.PostAsync($"Account/ChuyenThanhGV/{maTK}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        TempData["Error"] = "Có lỗi xảy ra khi trở thành giảng viên.";
                        return RedirectToAction("GioiThieu");
                    }
                    else
                    {
                        HttpContext.Session.SetInt32("LaGiangVien", 1);
                    }    
                }
            }

            // Sau khi đã là giảng viên → vào trang tạo khóa học
            return RedirectToAction("TaoKhoaHoc", "KhoaHoc");
        }


        // Trong QL_KhoaHoc/Controllers/GiangVienController.cs
        // GET: Hiển thị trang thiết lập hồ sơ (ĐÃ CẬP NHẬT)
        [HttpGet]
        public async Task<IActionResult> ThietLapHoSo()
        {
            // 1. Kiểm tra session
            int? maTK = HttpContext.Session.GetInt32("MaTK");
            if (maTK == null)
            {
                return RedirectToAction("Login", "Account");
            }

            GiangVien model = new GiangVien();

            // 2. Gọi API để lấy dữ liệu cũ
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                var response = await httpClient.GetAsync($"GiangVien/GetProfile/{maTK}");

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    model = JsonConvert.DeserializeObject<GiangVien>(data);
                }
                else
                {
                    // Nếu API lỗi hoặc chưa có dữ liệu (lần đầu), 
                    // có thể lấy Tên + Email từ Session hoặc bảng Account để điền sẵn cho tiện (Optional)
                    model.HoTen = HttpContext.Session.GetString("TenDN");
                }
            }

            // 3. Trả về View kèm Model có dữ liệu
            return View(model);
        }

        // POST: Xử lý lưu hồ sơ

        [HttpPost]
        public async Task<IActionResult> LuuHoSoGiangVien(GiangVien model)
        {
            int? maTK = HttpContext.Session.GetInt32("MaTK");
            if (maTK == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) return View("ThietLapHoSo", model);

            model.MaTK = maTK.Value;

            // 1. Cấu hình Cloudinary (Lấy từ appsettings.json của Web)
            var cloudName = _configuration["Cloudinary:CloudName"];
            var apiKey = _configuration["Cloudinary:ApiKey"];
            var apiSecret = _configuration["Cloudinary:ApiSecret"];
            var cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));

            // ==========================================
            // BƯỚC 2: OCR VÀ UPLOAD ẢNH MẶT TRƯỚC
            // ==========================================
            if (model.FileMatTruoc != null && model.FileMatTruoc.Length > 0)
            {
                // A. OCR quét tên
                string nameFromID = await _ocrService.ExtractNameFromIdCardAsync(model.FileMatTruoc);

                if (string.IsNullOrEmpty(nameFromID))
                {
                    ModelState.AddModelError("FileMatTruoc", "Không đọc được tên trên CCCD. Vui lòng chụp rõ nét.");
                    return View("ThietLapHoSo", model);
                }

                // B. So sánh tên với chủ tài khoản ngân hàng
                if (string.IsNullOrEmpty(model.TenChuThe))
                {
                    ModelState.AddModelError("TenChuThe", "Vui lòng nhập tên chủ tài khoản ngân hàng trước.");
                    return View("ThietLapHoSo", model);
                }

                if (!_ocrService.CompareNames(nameFromID, model.TenChuThe))
                {
                    ModelState.AddModelError("TenChuThe", $"Tên trên CCCD ({nameFromID}) KHÔNG KHỚP với tên tài khoản ({model.TenChuThe}).");
                    return View("ThietLapHoSo", model);
                }

                // C. Nếu khớp -> Upload lên Cloudinary
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(model.FileMatTruoc.FileName, model.FileMatTruoc.OpenReadStream()),
                    Folder = "identity-docs"
                };
                var uploadResult = await cloudinary.UploadAsync(uploadParams);

                // Gán Link ảnh vào Model để gửi sang API
                model.CCCD_MatTruoc = uploadResult.SecureUri.ToString();
            }

            // ==========================================
            // BƯỚC 3: UPLOAD ẢNH MẶT SAU (KHÔNG CẦN OCR)
            // ==========================================
            if (model.FileMatSau != null && model.FileMatSau.Length > 0)
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(model.FileMatSau.FileName, model.FileMatSau.OpenReadStream()),
                    Folder = "identity-docs"
                };
                var uploadResult = await cloudinary.UploadAsync(uploadParams);

                // Gán Link ảnh vào Model
                model.CCCD_MatSau = uploadResult.SecureUri.ToString();
            }

            // ==========================================
            // BƯỚC 4: GỬI JSON SANG API ĐỂ LƯU DB
            // ==========================================
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);

                // Vì ảnh đã upload xong và lấy được Link rồi, ta chỉ cần gửi JSON thuần
                string json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("GiangVien/UpdateProfile", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Cập nhật hồ sơ và xác minh danh tính thành công!";
                    return RedirectToAction("ThietLapHoSo"); // Hoặc trang Index
                }
                else
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", "Lỗi từ API: " + errorMsg);
                    return View("ThietLapHoSo", model);
                }
            }
        }
        [HttpGet]
        public async Task<IActionResult> ThuNhap(DateTime? fromDate, DateTime? toDate)
        {
            var maGV = HttpContext.Session.GetInt32("MaGV");
            if (maGV == null) return RedirectToAction("Login", "Account");

            List<ThongKeThuNhapGV> listStats = new List<ThongKeThuNhapGV>();

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);

                // Tạo query string
                string queryParams = $"?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";
                if (fromDate == null) queryParams = ""; // Nếu không chọn thì lấy tất cả

                var response = await httpClient.GetAsync($"GiangVien/GetRevenueStats/{maGV}{queryParams}");

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    listStats = JsonConvert.DeserializeObject<List<ThongKeThuNhapGV>>(data);
                }
            }

            // Truyền lại ngày đã chọn ra View để giữ trạng thái input
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(listStats);
        }

        // Trong GiangVienController.cs (Web)

        [HttpGet]
        [AllowAnonymous] // Cho phép ai cũng xem được, không cần đăng nhập
        public async Task<IActionResult> Info(int id)
        {
            var viewModel = new GiangVienPublicViewModel();

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);

                // 1. Lấy thông tin giảng viên
                var resInfo = await httpClient.GetAsync($"GiangVien/GetPublicInfo/{id}");
                if (resInfo.IsSuccessStatusCode)
                {
                    string dataInfo = await resInfo.Content.ReadAsStringAsync();
                    viewModel = JsonConvert.DeserializeObject<GiangVienPublicViewModel>(dataInfo);
                }
                else
                {
                    return NotFound(); // Không tìm thấy GV
                }

                // 2. Lấy danh sách khóa học
                var resCourses = await httpClient.GetAsync($"KhoaHoc/GetPublicCoursesByInstructor/{id}");
                if (resCourses.IsSuccessStatusCode)
                {
                    string dataCourses = await resCourses.Content.ReadAsStringAsync();
                    viewModel.KhoaHocs = JsonConvert.DeserializeObject<List<KhoaHoc>>(dataCourses);
                }

                // Tính toán sơ bộ thống kê
                if (viewModel.KhoaHocs != null)
                {
                    viewModel.TongHocVien = viewModel.KhoaHocs.Sum(k => k.SoDangKy);
                    viewModel.TongDanhGia = viewModel.KhoaHocs.Sum(k => k.SoDanhGia);
                }
            }

            return View(viewModel);
        }
    }
}
