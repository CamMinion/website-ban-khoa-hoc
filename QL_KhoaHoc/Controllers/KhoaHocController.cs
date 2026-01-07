

using QL_KhoaHoc.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic; // Cần cho List
using Microsoft.Extensions.Configuration;  // Để đọc config Cloudinary
using CloudinaryDotNet;  // Cloudinary SDK
using CloudinaryDotNet.Actions;  // Cho VideoUploadParams

namespace QL_KhoaHoc.Controllers
{

    public class MarkCompleteModel
    {
        public int MaHV { get; set; }
        public int MaKH { get; set; }
        public string Type { get; set; } // BG, TN, BT
        public int Id { get; set; }
    }

    public class TienDo
    {

        public int MaHV { get; set; }
        public int MaKH { get; set; }
        public string Type { get; set; } // "BG", "TN", "BT"
        public int Id { get; set; }

    }

    public class DanhGia
    {
        public int MaHV { get; set; }
        public int MaKH { get; set; }
        public int SoSao { get; set; }
        public string NoiDung { get; set; }
    }

  
    public class KhoaHocController : Controller
    {
        private readonly string _apiBaseUrl = "http://localhost:5105/api/";
        private readonly IConfiguration _configuration;  // Inject để đọc config

        public KhoaHocController(IConfiguration configuration)  // Constructor để inject
        {
            _configuration = configuration;
        }

        // ========== HÀM MỚI: Dùng để gọi API lấy danh mục ==========
        private async Task<List<DanhMucCon>> GetDanhMucConListAsync()
        {
            var danhMucConList = new List<DanhMucCon>();
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync("DanhMucCon"); // Gọi API mới
                    if (response.IsSuccessStatusCode)
                    {
                        string data = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<List<DanhMucCon>>(data);
                        if (result != null)
                        {
                            danhMucConList = result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Ghi lại lỗi (nếu cần)
                    Console.WriteLine($"Lỗi khi gọi API DanhMucCon: {ex.Message}");
                }
            }
            return danhMucConList;
        }
        // =========================================================


        //public async Task<IActionResult> ChitietKH(int id)
        //{
        //    // ... (Code cũ của bạn, giữ nguyên)
        //    KhoaHoc? kh = null;
        //    using (var httpClient = new HttpClient())
        //    {
        //        httpClient.BaseAddress = new Uri(_apiBaseUrl);
        //        HttpResponseMessage response = await httpClient.GetAsync("KhoaHoc/" + id);
        //        if (response.IsSuccessStatusCode)
        //        {
        //            string data = await response.Content.ReadAsStringAsync();
        //            var result = JsonConvert.DeserializeObject<KhoaHoc>(data);
        //            if (result != null)
        //            {
        //                kh = result;
        //            }
        //        }
        //    }
        //    return View(kh);
        //}

        public async Task<IActionResult> ChitietKH(int id)
        {
            // Đổi thành KhoaHocCreateModel để hứng được cả list ChuongMucs
            KhoaHocCreateModel? kh = null;

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                HttpResponseMessage response = await httpClient.GetAsync("KhoaHoc/" + id);
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    // QUAN TRỌNG: Deserialize về KhoaHocCreateModel
                    var result = JsonConvert.DeserializeObject<KhoaHocCreateModel>(data);
                    if (result != null)
                    {
                        kh = result;
                    }
                }
            }
            // Nếu null thì trả về trang lỗi hoặc xử lý tùy ý
            if (kh == null) return NotFound();

            return View(kh);
        }

       

        // Sửa TaoKhoaHoc để hỗ trợ edit
        [HttpGet]
        public async Task<IActionResult> TaoKhoaHoc(int? id = null)
        {
            ViewBag.DanhMucConList = await GetDanhMucConListAsync();

            if (id.HasValue)
            {
                KhoaHocCreateModel model = null;
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(_apiBaseUrl);
                    HttpResponseMessage response = await httpClient.GetAsync($"KhoaHoc/{id.Value}");
                    if (response.IsSuccessStatusCode)
                    {
                        string data = await response.Content.ReadAsStringAsync();
                        // Deserialize TRỰC TIẾP sang KhoaHocCreateModel (API mới đã trả về đầy đủ)
                        model = JsonConvert.DeserializeObject<KhoaHocCreateModel>(data);
                    }
                }
                if (model == null) return NotFound();
                return View(model);
            }
            return View(new KhoaHocCreateModel());
        }


        // Hàm phụ trợ kiểm tra hồ sơ (Thêm vào trong KhoaHocController)
        private async Task<bool> CheckProfileComplete(int maTK)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                // Gọi API lấy thông tin profile
                var response = await httpClient.GetAsync($"GiangVien/GetProfile/{maTK}");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    var profile = JsonConvert.DeserializeObject<dynamic>(data); // Dùng dynamic cho nhanh gọn

                    // Kiểm tra các trường quan trọng (Ví dụ: Số tài khoản và Tên ngân hàng)
                    if (string.IsNullOrEmpty((string)profile.stk) || string.IsNullOrEmpty((string)profile.tenNH))
                    {
                        return false; // Chưa đủ
                    }
                    return true; // Đã đủ
                }
                return false; // Lỗi hoặc không tìm thấy -> coi như chưa đủ
            }
        }

        // Action để xử lý POST tạo mới (ĐÃ CẬP NHẬT VỚI UPLOAD CLOUDINARY)
        [HttpPost]
        public async Task<IActionResult> TaoKhoaHoc(KhoaHocCreateModel model, string action)
        {
            // Tải danh sách danh mục LÊN ĐẦU TIÊN
            // để nếu có lỗi validation và return View(model),
            // dropdown vẫn có dữ liệu.
            ViewBag.DanhMucConList = await GetDanhMucConListAsync();

            // === LOGIC XỬ LÝ LƯU NHÁP (Giữ nguyên) ===
            if (action == "TaoKhoaHoc" && !ModelState.IsValid)
            {
                return View(model);
            }
            if (action == "saveDraft")
            {
                ModelState.Remove("GiaTien");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var maGV = HttpContext.Session.GetInt32("MaGV");
            if (maGV == null)
            {
                ModelState.AddModelError(string.Empty, "Bạn cần đăng nhập để tạo khóa học.");
                return View(model);
            }
            model.MaGiangVien = maGV.Value;  // Gán vào model để API nhận








            //// === GÁN TRẠNG THÁI DỰA VÀO NÚT BẤM ===
            //if (action == "submitReview")
            //{
            //    model.TrangThai = "ChoDuyet"; // Hoặc tên trạng thái bạn muốn
            //}
            //else
            //{
            //    model.TrangThai = "Nhap";
            //}

            int? maTK = HttpContext.Session.GetInt32("MaTK");

            // === KIỂM TRA HỒ SƠ NẾU GỬI XÉT DUYỆT ===
            if (action == "TaoKhoaHoc")
            {
                // Kiểm tra xem hồ sơ đã đủ chưa
                bool isProfileComplete = await CheckProfileComplete(maTK.Value);
                if (!isProfileComplete)
                {
                    // Thêm lỗi vào ModelState để hiển thị ra View
                    ModelState.AddModelError(string.Empty, "Hồ sơ của bạn chưa đủ điều kiện để kiếm tiền trên T&C Academy. Vui lòng cập nhật thông tin thanh toán trong phần 'Thiết lập hồ sơ'.");

                    // Trả về View ngay lập tức, không lưu
                    return View(model);
                }

                model.TrangThai = "ChoDuyet";
            }
            else
            {
                model.TrangThai = "Nhap";
            }



            // =========================================

            // ======== XỬ LÝ UPLOAD VIDEO & TÀI LIỆU LÊN CLOUDINARY ========
            var cloudName = _configuration["Cloudinary:CloudName"];
            var apiKey = _configuration["Cloudinary:ApiKey"];
            var apiSecret = _configuration["Cloudinary:ApiSecret"];
            var cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));

            // =======================================================================
            // [MỚI] XỬ LÝ UPLOAD ẢNH BÌA / VIDEO GIỚI THIỆU
            // =======================================================================
            if (model.AnhBiaFile != null && model.AnhBiaFile.Length > 0)
            {
                var file = model.AnhBiaFile;
                RawUploadResult uploadResult = null;

                // 1. Nếu là VIDEO
                if (file.ContentType.StartsWith("video/"))
                {
                    var uploadParams = new VideoUploadParams()
                    {
                        File = new FileDescription(file.FileName, file.OpenReadStream()),
                        Folder = "khoa-hoc-intro", // Folder riêng cho video intro
                        EagerTransforms = new List<Transformation>()
            {
                // Tạo sẵn ảnh thumbnail từ video để hiển thị khi chưa play
                //new Transformation().Width(500).Height(300).Crop("fill").Format("jpg")
                // Dùng FetchFormat thay cho Format
                new Transformation().Width(500).Height(300).Crop("fill").FetchFormat("jpg")
            }
                    };
                    uploadResult = await cloudinary.UploadAsync(uploadParams);
                }
                // 2. Nếu là ẢNH
                else if (file.ContentType.StartsWith("image/"))
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.FileName, file.OpenReadStream()),
                        Folder = "khoa-hoc-thumbnails"
                    };
                    uploadResult = await cloudinary.UploadAsync(uploadParams);
                }
                else
                {
                    ModelState.AddModelError("AnhBiaFile", "Chỉ chấp nhận file Ảnh hoặc Video.");
                    return View(model);
                }

                if (uploadResult != null && uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Lưu URL vào model để gửi sang API lưu xuống DB
                    model.AnhBia = uploadResult.SecureUri.ToString();
                }
                else
                {
                    ModelState.AddModelError("AnhBiaFile", "Lỗi upload ảnh bìa/video.");
                    return View(model);
                }
            }
            // =======================================================================

            foreach (var chuong in model.ChuongMucs ?? new List<ChuongMucCreateModel>())
            {
                foreach (var baiGiang in chuong.BaiGiangs ?? new List<BaiGiangCreateModel>())
                {
                    // 1. UPLOAD VIDEO (Code cũ của bạn - Giữ nguyên)
                    if (baiGiang.VideoFile != null && baiGiang.VideoFile.Length > 0)
                    {
                        if (!baiGiang.VideoFile.ContentType.StartsWith("video/"))
                        {
                            ModelState.AddModelError("", "File video không hợp lệ.");
                            return View(model);
                        }
                        var uploadParams = new VideoUploadParams()
                        {
                            File = new FileDescription(baiGiang.VideoFile.FileName, baiGiang.VideoFile.OpenReadStream()),
                            Folder = "bai-giang-videos"
                        };
                        var uploadResult = await cloudinary.UploadAsync(uploadParams);
                        if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            baiGiang.Video = uploadResult.SecureUri.ToString();
                        }
                    }

                    // 2. [THÊM MỚI] UPLOAD TÀI LIỆU (Word, PDF, Excel...)
                    if (baiGiang.TaiLieuFile != null && baiGiang.TaiLieuFile.Length > 0)
                    {
                        // Sử dụng RawUploadParams cho các file tài liệu chung
                        var rawParams = new RawUploadParams()
                        {
                            File = new FileDescription(baiGiang.TaiLieuFile.FileName, baiGiang.TaiLieuFile.OpenReadStream()),
                            Folder = "tai-lieu-khoa-hoc", // Tạo folder riêng trên Cloudinary
                            UseFilename = true,
                            UniqueFilename = true
                        };

                        var rawResult = await cloudinary.UploadAsync(rawParams);

                        if (rawResult.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // Lưu URL vào thuộc tính FileTaiXuong
                            baiGiang.FileTaiXuong = rawResult.SecureUri.ToString();

                            // Xóa file stream để tránh lỗi khi serialize JSON gửi sang API
                            baiGiang.TaiLieuFile = null;
                        }
                        else
                        {
                            ModelState.AddModelError("", "Upload tài liệu thất bại: " + rawResult.Error?.Message);
                            return View(model);
                        }
                    }
                }

                // ... (Đoạn xử lý Bài Giảng ở trên giữ nguyên) ...

                // 3. [THÊM MỚI] XỬ LÝ UPLOAD FILE HƯỚNG DẪN BÀI TẬP
                foreach (var baiTap in chuong.BaiTaps ?? new List<BaiTapCreateModel>())
                {
                    if (baiTap.HuongDanFile != null && baiTap.HuongDanFile.Length > 0)
                    {
                        var rawParams = new RawUploadParams()
                        {
                            File = new FileDescription(baiTap.HuongDanFile.FileName, baiTap.HuongDanFile.OpenReadStream()),
                            Folder = "tai-lieu-bai-tap",
                            UseFilename = true,
                            UniqueFilename = true
                        };

                        var rawResult = await cloudinary.UploadAsync(rawParams);

                        if (rawResult.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            // Lưu URL file vào trường HuongDan (Ghi đè text cũ nếu có)
                            baiTap.HuongDan = rawResult.SecureUri.ToString();

                            // Xóa stream
                            baiTap.HuongDanFile = null;
                        }
                        else
                        {
                            ModelState.AddModelError("", "Upload file hướng dẫn thất bại: " + rawResult.Error?.Message);
                            return View(model);
                        }
                    }
                }
            }
            // ===================================================

            // Nếu vượt qua, tiếp tục gọi API như cũ (model giờ có Video là URL)
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                string json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                if (model.MaKhoaHoc > 0) // Edit
                {
                    response = await httpClient.PutAsync($"KhoaHoc/{model.MaKhoaHoc}", content);
                }
                else // Create
                {
                    response = await httpClient.PostAsync("KhoaHoc/", content);
                }

                if (response.IsSuccessStatusCode)
                {
                    if (action == "saveDraft")
                    {
                        TempData["SuccessMessage"] = "Đã lưu bản nháp thành công! Bạn có thể chỉnh sửa tiếp tại mục 'Bản nháp'.";
                        // Điều hướng về trang Bản Nháp
                        return RedirectToAction("QuanLyBanNhap");
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Gửi xét duyệt thành công! Khóa học đang chờ Admin phê duyệt.";
                        // Điều hướng về trang Danh sách khóa học (Tab Chờ duyệt)
                        return RedirectToAction("DanhSachKH");
                    }
                }


                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", error);
                    return View(model);
                }
            }


        }

        public async Task<IActionResult> QuanLyBanNhap()
        {
            var maGV = HttpContext.Session.GetInt32("MaGV"); // Giả sử MaTK là MaGiangVien
            if (maGV == null) return RedirectToAction("Login");

            List<KhoaHoc> drafts = new List<KhoaHoc>();
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                HttpResponseMessage response = await httpClient.GetAsync($"KhoaHoc/drafts/{maGV.Value}");
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    drafts = JsonConvert.DeserializeObject<List<KhoaHoc>>(data);
                }
            }
            return View(drafts); // View QuanLyBanNhap.cshtml liệt kê list
        }


        [HttpPost]
        public async Task<IActionResult> ThemVaoGioHang(int maKH)
        {
            // Bước 1: Lấy MaHV từ Session (như bạn nói đã có)
            var maHV = HttpContext.Session.GetInt32("MaHV");

            // Bước 2: Kiểm tra xem user đã đăng nhập chưa (MaHV có null không)
            if (maHV == null)
            {
                // Nếu chưa đăng nhập, chuyển đến trang đăng nhập
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thêm vào giỏ hàng.";
                // Sửa "Login" và "Account" cho đúng với Controller đăng nhập của bạn
                return RedirectToAction("Login", "Account");
            }

            if (maKH <= 0)
            {
                TempData["ErrorMessage"] = "Khóa học không hợp lệ.";
                return RedirectToAction("ChiTietKH", new { id = maKH });
            }

            // Bước 3: Chuẩn bị dữ liệu để gọi API
            var model = new
            {
                MaHV = maHV.Value,
                MaKH = maKH
            };

            // Bước 4: Gọi API "ThemVaoGioHang"
            using (var httpClient = new HttpClient())
            {
                // Giả sử _apiBaseUrl đã có trong controller của bạn
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                string json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Gọi API mới bạn vừa tạo: api/GioHang/ThemVaoGioHang
                HttpResponseMessage response = await httpClient.PostAsync("GioHang/ThemVaoGioHang", content);

                if (response.IsSuccessStatusCode)
                {
                    // Đọc thông báo từ API (ví dụ: "Thêm thành công" hoặc "Đã có trong giỏ")
                    var responseData = await response.Content.ReadAsStringAsync();
                    var apiResult = JsonConvert.DeserializeObject<dynamic>(responseData);
                    TempData["SuccessMessage"] = (string)apiResult.message;
                }
                else
                {
                    // Lấy lỗi từ API
                    string error = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi: {error}";
                }
            }

            // Bước 5: Quay lại trang chi tiết khóa học
            return RedirectToAction("ChiTietKH", new { id = maKH });
        }

        // 1. Action vào màn hình Học
        [HttpGet]
        public async Task<IActionResult> Hoc(int id, bool isPreview = false)
        {
            KhoaHocCreateModel model = null;

            // Gọi API lấy chi tiết khóa học
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                var response = await httpClient.GetAsync($"KhoaHoc/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    model = JsonConvert.DeserializeObject<KhoaHocCreateModel>(data);
                }
            }
            if (model == null) return NotFound();

            // Lấy thông tin User
            var maHV = HttpContext.Session.GetInt32("MaHV"); // Lấy ID học viên
            var maGV = HttpContext.Session.GetInt32("MaGV"); // Lấy ID giảng viên (nếu có)

            // Xác định quyền
            bool isAuthor = (maGV != null && maGV == model.MaGiangVien);

            // Nếu là tác giả -> Tự động bật Preview
            if (isAuthor) isPreview = true;

            // Nếu không phải tác giả và chưa đăng nhập -> Bắt login
            if (!isAuthor && maHV == null) return RedirectToAction("Login", "Account");

            // Lấy ID người dùng hiện tại để load tiến độ (GV dùng MaGV để test, HV dùng MaHV)
            // Lưu ý: Trong bảng TIENDOHOCTAP cột là MAHV, nếu GV test thì bạn cần đảm bảo GV có ID tương ứng hoặc sửa logic API chút xíu.
            // Ở đây giả định dùng chung ID int cho đơn giản.
            int currentUserId = maHV ?? (maGV ?? 0);

            // Gọi API lấy tiến độ đã học
            ViewBag.CompletedItems = new List<dynamic>();
            if (currentUserId > 0)
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(_apiBaseUrl);
                    var res = await httpClient.GetAsync($"KhoaHoc/GetTienDo/{currentUserId}/{id}");
                    if (res.IsSuccessStatusCode)
                    {
                        var data = await res.Content.ReadAsStringAsync();
                        ViewBag.CompletedItems = JsonConvert.DeserializeObject<List<dynamic>>(data);
                    }
                }
            }

            ViewBag.IsPreview = isPreview;
            ViewBag.IsAuthor = isAuthor;
            ViewBag.CurrentUserId = currentUserId;

            return View(model);
        }

        // 2. Action Reset Tiến Độ (Cho Giảng viên)
        [HttpPost]
        public async Task<IActionResult> ResetProgress(int maKH)
        {
            // Lấy ID hiện tại (Ưu tiên MaGV vì tính năng này cho GV)
            var userId = HttpContext.Session.GetInt32("MaGV") ?? HttpContext.Session.GetInt32("MaHV");

            if (userId == null) return Json(new { success = false, message = "Chưa đăng nhập" });

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                var response = await httpClient.DeleteAsync($"KhoaHoc/ResetTienDo/{userId}/{maKH}");
                return Json(new { success = response.IsSuccessStatusCode });
            }
        }

        // Thêm vào AccountController.cs

        [HttpGet]
        public async Task<IActionResult> MyCourses()
        {
            // 1. Lấy Mã Học Viên từ Session
            var maHV = HttpContext.Session.GetInt32("MaHV");

            // Nếu chưa đăng nhập hoặc không phải học viên -> Đá về Login
            if (maHV == null)
            {
                return RedirectToAction("Login");
            }

            List<KhoaHoc> listKhoaHoc = new List<KhoaHoc>();

            using (var httpClient = new HttpClient())
            {
                // Lưu ý: Đảm bảo _apiBaseUrl đã được khai báo trong AccountController
                // Nếu chưa có, khai báo: private readonly string _apiBaseUrl = "http://localhost:5105/api/";
                httpClient.BaseAddress = new Uri(_apiBaseUrl);

                // 2. Gọi API GetKhoaHocDaMua bên Backend
                HttpResponseMessage response = await httpClient.GetAsync($"KhoaHoc/GetKhoaHocDaMua/{maHV}");

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    listKhoaHoc = JsonConvert.DeserializeObject<List<KhoaHoc>>(data);
                }
            }

            // 3. Trả về View kèm danh sách khóa học
            return View(listKhoaHoc);
        }



        //// === THÊM ACTION NÀY ĐỂ XỬ LÝ HOÀN THÀNH BÀI HỌC ===
        //[HttpPost]
        //public async Task<IActionResult> MarkCompleted([FromBody] MarkCompleteModel model)
        //{
        //    // Kiểm tra session để đảm bảo bảo mật (người đang login đúng là MaHV gửi lên)
        //    var sessionMaHV = HttpContext.Session.GetInt32("MaHV");
        //    if (sessionMaHV == null || sessionMaHV != model.MaHV)
        //    {
        //        return Json(new { success = false, message = "Thông tin xác thực không hợp lệ." });
        //    }

        //    using (var httpClient = new HttpClient())
        //    {
        //        httpClient.BaseAddress = new Uri(_apiBaseUrl);

        //        // Serialize dữ liệu để gửi sang API
        //        string json = JsonConvert.SerializeObject(model);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //        // Gọi API Backend: api/KhoaHoc/MarkCompleted
        //        // Đảm bảo bên API Controller của bạn đã có Endpoint này
        //        HttpResponseMessage response = await httpClient.PostAsync("KhoaHoc/MarkCompleted", content);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            return Json(new { success = true });
        //        }
        //        else
        //        {
        //            // Có thể log lỗi ở đây
        //            return Json(new { success = false, message = "Lỗi từ API" });
        //        }
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> MarkCompleted([FromBody] TienDo model)
        {
            // 1. Kiểm tra Session để bảo mật
            var sessionMaHV = HttpContext.Session.GetInt32("MaHV");
            if (sessionMaHV == null || sessionMaHV != model.MaHV)
            {
                // Nếu user đang login khác với user gửi request -> chặn
                return Json(new { success = false, message = "Phiên đăng nhập không hợp lệ." });
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);

                // 2. Serialize model gửi sang API
                string json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Gọi API
                HttpResponseMessage response = await httpClient.PostAsync("KhoaHoc/MarkCompleted", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true });
                }
                else
                {
                    // Đọc lỗi từ API trả về để debug
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = "Lỗi API: " + errorContent });
                }
            }
        }

        //[HttpPost]
        //public async Task<IActionResult> SubmitReview([FromBody] DanhGia model)
        //{
        //    // 1. Kiểm tra session để bảo mật
        //    var sessionMaHV = HttpContext.Session.GetInt32("MaHV");
        //    if (sessionMaHV == null || sessionMaHV != model.MaHV)
        //    {
        //        return Json(new { success = false, message = "Bạn cần đăng nhập lại." });
        //    }

        //    // 2. Gọi sang API
        //    using (var httpClient = new HttpClient())
        //    {
        //        httpClient.BaseAddress = new Uri(_apiBaseUrl);
        //        string json = JsonConvert.SerializeObject(model);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //        HttpResponseMessage response = await httpClient.PostAsync("KhoaHoc/LuuDanhGia", content);

        //        if (response.IsSuccessStatusCode)
        //        {
        //            return Json(new { success = true });
        //        }
        //        else
        //        {
        //            var error = await response.Content.ReadAsStringAsync();
        //            return Json(new { success = false, message = "Lỗi API: " + error });
        //        }
        //    }
        //}


        [HttpPost]
        public async Task<IActionResult> SubmitReview([FromBody] DanhGia model)
        {
            // 1. Kiểm tra session để bảo mật
            var sessionMaHV = HttpContext.Session.GetInt32("MaHV");
            if (sessionMaHV == null || sessionMaHV != model.MaHV)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập lại." });
            }

            // 2. Gọi sang API
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                string json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync("KhoaHoc/LuuDanhGia", content);

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = "Lỗi API: " + error });
                }
            }
        }

        // Action gọi API Gợi ý khóa học
        [HttpGet]
        public async Task<IActionResult> GetGoiYKhoaHoc()
        {
            var maHV = HttpContext.Session.GetInt32("MaHV");

            // Nếu chưa đăng nhập, trả về danh sách rỗng hoặc logic mặc định
            if (maHV == null)
            {
                return Json(new List<object>());
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                try
                {
                    // Gọi API Backend: api/KhoaHoc/GoiYKhoaHoc/{maHV}
                    HttpResponseMessage response = await httpClient.GetAsync($"KhoaHoc/GoiYKhoaHoc/{maHV}");

                    if (response.IsSuccessStatusCode)
                    {
                        string data = await response.Content.ReadAsStringAsync();
                        // Trả về JSON nguyên bản từ API để View tự xử lý
                        return Content(data, "application/json");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Lỗi gọi API gợi ý: " + ex.Message);
                }
            }
            return Json(new List<object>());
        }


        [HttpPost]
        public async Task<IActionResult> LuuKetQua([FromBody] KetQuaLamBai model)
        {
            var maHV = HttpContext.Session.GetInt32("MaHV");
            // Nếu GV test thì lấy MaGV, ưu tiên MaHV
            var userId = maHV ?? HttpContext.Session.GetInt32("MaGV");

            if (userId == null) return Json(new { success = false, message = "Chưa đăng nhập" });

            model.MaHV = userId.Value;

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                string json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("KhoaHoc/LuuKetQuaLamBai", content);

                return Json(new { success = response.IsSuccessStatusCode });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetKetQua(string type, int id)
        {
            var userId = HttpContext.Session.GetInt32("MaHV") ?? HttpContext.Session.GetInt32("MaGV");
            if (userId == null) return Json(null);

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                var response = await httpClient.GetAsync($"KhoaHoc/GetKetQuaLamBai/{userId}/{type}/{id}");

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    return Content(data, "application/json");
                }
            }
            return Json(null);
        }


        // [GET] Tìm kiếm khóa học (Hỗ trợ cả Từ khóa và Danh mục)
        [HttpGet]
        public async Task<IActionResult> Search(string? keyword, int? category)
        {
            // 1. Kiểm tra: Nếu không có từ khóa VÀ không có danh mục -> Về trang chủ
            if (string.IsNullOrEmpty(keyword) && category == null)
            {
                return RedirectToAction("Index", "Home");
            }

            List<KhoaHoc> searchResults = new List<KhoaHoc>();

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);

                // 2. Xây dựng URL gọi API linh động
                // API mong đợi: KhoaHoc/Search?keyword=...&categoryId=...
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(keyword))
                    queryParams.Add($"keyword={keyword}");

                if (category.HasValue)
                    queryParams.Add($"categoryId={category}"); // Map 'category' (Web) sang 'categoryId' (API)

                string queryString = string.Join("&", queryParams);
                string url = $"KhoaHoc/Search?{queryString}";

                // 3. Gọi API
                HttpResponseMessage response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    searchResults = JsonConvert.DeserializeObject<List<KhoaHoc>>(data);
                }
            }

            // 4. Truyền dữ liệu lại View để hiển thị
            ViewBag.Keyword = keyword;       // Để hiển thị lại trong ô tìm kiếm
            ViewBag.CategoryId = category;   // Để (nếu cần) highlight menu đang chọn

            // Trả về View Search (đảm bảo bạn đã tạo file Search.cshtml)
            return View("Search", searchResults);
        }

        // Trong KhoaHocController.cs (Web)

[HttpGet]
public async Task<IActionResult> DanhSachKH()
{
    // Lấy MaGV từ Session (đã set khi login hoặc vào dashboard)
    var maGV = HttpContext.Session.GetInt32("MaGV");
    if (maGV == null) return RedirectToAction("Login", "Account");

    List<KhoaHoc> listKhoaHoc = new List<KhoaHoc>();

    using (var httpClient = new HttpClient())
    {
        httpClient.BaseAddress = new Uri(_apiBaseUrl);
        var response = await httpClient.GetAsync($"KhoaHoc/GetCoursesByInstructor/{maGV}");

        if (response.IsSuccessStatusCode)
        {
            string data = await response.Content.ReadAsStringAsync();
            listKhoaHoc = JsonConvert.DeserializeObject<List<KhoaHoc>>(data);
        }
    }

    return View(listKhoaHoc);
}
    }
}