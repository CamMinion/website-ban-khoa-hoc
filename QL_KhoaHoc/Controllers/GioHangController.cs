using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using QL_KhoaHoc.Models;
using System;
using System.Linq;
using System.Text;
using Google.Api;

// NẾU bạn để VnPayLibrary ở thư mục Services, hãy mở comment dòng dưới:
//using QL_KhoaHoc.Services; 

namespace QL_KhoaHoc.Controllers
{
    public class GioHangController : Controller
    {
        private const string CART_DISCOUNT_CODE = "CartDiscountCode";
        private readonly string _apiBaseUrl = "http://localhost:5105/api/"; // PORT API CỦA BẠN
        private readonly IConfiguration _configuration;

        public GioHangController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ===================================================================
        // 1. GỬI YÊU CẦU THANH TOÁN SANG VNPAY
        // ===================================================================
        [HttpPost]
        public async Task<IActionResult> TienHanhThanhToan()
        {
            var maHV = HttpContext.Session.GetInt32("MaHV");
            if (maHV == null) return RedirectToAction("Login", "Account");

            // 1. Lấy giỏ hàng
            var viewModel = await GetGioHangViewModel(maHV.Value);
            if (!viewModel.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống.";
                return RedirectToAction("XemGioHang");
            }

            // 2. Gọi API tạo Hóa đơn (Trạng thái: Chưa thanh toán)
            int newMaHD = 0;
            var apiModel = new
            {
                MaHV = maHV.Value,
                MaGG = viewModel.AppliedDiscount?.MAGG,
                // [MỚI] Gửi thông tin điểm và tổng giảm giá (bao gồm cả coupon + điểm)
                DiemMuonDung = viewModel.PointsToUse,
                ThanhTien = viewModel.TotalPayable,
                TongTienGiam = viewModel.DiscountAmount + viewModel.PointDiscountAmount
            };

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                string json = JsonConvert.SerializeObject(apiModel);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync("HoaDon/TaoHoaDon", content);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Lỗi khi tạo hóa đơn.";
                    return RedirectToAction("XemGioHang");
                }
                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(data);
                newMaHD = result.maHD;
            }

            // 3. Xóa giỏ hàng trong Session (vì đã chốt đơn)
            HttpContext.Session.Remove(CART_DISCOUNT_CODE);

            // 4. TẠO URL THANH TOÁN VNPAY
            // TODO: Sửa port 7214 thành port thực tế của Web MVC bạn đang chạy
            string vnp_Returnurl = "https://localhost:7213/GioHang/PaymentCallback";
            string vnp_Url = _configuration["VnPay:BaseUrl"];
            string vnp_TmnCode = _configuration["VnPay:TmnCode"];
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];

            if (string.IsNullOrEmpty(vnp_TmnCode) || string.IsNullOrEmpty(vnp_HashSecret))
            {
                TempData["ErrorMessage"] = "Cấu hình VNPAY chưa đúng (kiểm tra appsettings.json).";
                return RedirectToAction("XemGioHang");
            }

            VnPayLibrary vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            //vnpay.AddRequestData("vnp_Amount", ((long)viewModel.TotalPayable * 100).ToString()); // Nhân 100 theo quy tắc VNPAY
            // CODE MỚI (Làm tròn chính xác trước khi ép kiểu)
            long amount = (long)Math.Round(viewModel.TotalPayable * 100, 0, MidpointRounding.AwayFromZero);
            vnpay.AddRequestData("vnp_Amount", amount.ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan hoa don:" + newMaHD);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", newMaHD.ToString()); // Mã tham chiếu là Mã Hóa Đơn

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            // Chuyển hướng người dùng sang VNPAY
            return Redirect(paymentUrl);
        }

        // ===================================================================
        // 2. XỬ LÝ KHI VNPAY TRẢ VỀ (CALLBACK)
        // ===================================================================
        //[HttpGet]
        //public async Task<IActionResult> PaymentCallback()
        //{
        //    var query = Request.Query;
        //    if (query.Count == 0) return RedirectToAction("XemGioHang");

        //    string vnp_HashSecret = _configuration["VnPay:HashSecret"];
        //    VnPayLibrary vnpay = new VnPayLibrary();

        //    // Lấy toàn bộ dữ liệu trả về
        //    foreach (var key in query.Keys)
        //    {
        //        if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
        //        {
        //            vnpay.AddResponseData(key, query[key]);
        //        }
        //    }

        //    // Lấy thông tin quan trọng
        //    long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef")); // Mã Hóa Đơn
        //    string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode"); // 00 = Thành công
        //    string vnp_SecureHash = query["vnp_SecureHash"]; // Chữ ký để kiểm tra

        //    // Kiểm tra chữ ký bảo mật
        //    bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

        //    if (checkSignature)
        //    {
        //        if (vnp_ResponseCode == "00")
        //        {
        //            // --- THANH TOÁN THÀNH CÔNG ---

        //            // Gọi API để cập nhật trạng thái hóa đơn thành "Đã thanh toán"
        //            using (var httpClient = new HttpClient())
        //            {
        //                httpClient.BaseAddress = new Uri(_apiBaseUrl);
        //                await httpClient.PostAsync($"HoaDon/CapNhatThanhToan/{orderId}", null);
        //            }

        //            ViewBag.Message = "Giao dịch thành công!";
        //            ViewBag.MaHD = orderId;

        //            // Chuyển đến trang thông báo thành công
        //            return View("ThanhToanThanhCong");
        //        }
        //        else
        //        {
        //            // Thanh toán thất bại / Hủy
        //            TempData["ErrorMessage"] = $"Lỗi thanh toán VNPAY: Mã lỗi {vnp_ResponseCode}";
        //            return RedirectToAction("XemGioHang");
        //        }
        //    }
        //    else
        //    {
        //        // Sai chữ ký (Có dấu hiệu giả mạo)
        //        TempData["ErrorMessage"] = "Có lỗi xảy ra (Sai chữ ký VNPAY).";
        //        return RedirectToAction("XemGioHang");
        //    }
        //}

        // Trong GioHangController.cs

        // Trong GioHangController.cs

        // Trong GioHangController.cs


        // Action áp dụng điểm
        [HttpPost]
        public IActionResult ApplyPoints(int points)
        {
            // Lưu số điểm muốn dùng vào Session tạm
            HttpContext.Session.SetInt32("PointsToUse", points);
            return RedirectToAction("XemGioHang");
        }

        [HttpPost]
        public IActionResult RemovePoints()
        {
            HttpContext.Session.Remove("PointsToUse");
            return RedirectToAction("XemGioHang");
        }

        //[HttpGet]
        //public async Task<IActionResult> PaymentCallback()
        //{
        //    var query = Request.Query;
        //    if (query.Count == 0) return RedirectToAction("XemGioHang");

        //    // Khối TRY-CATCH để bắt lỗi và ngăn chặn ERR_EMPTY_RESPONSE
        //    try
        //    {
        //        string vnp_HashSecret = _configuration["VnPay:HashSecret"];

        //        // KIỂM TRA KHẨN CẤP LỖI CẤU HÌNH
        //        if (string.IsNullOrEmpty(vnp_HashSecret))
        //        {
        //            return BadRequest("LỖI CẤU HÌNH: Thiếu HashSecret VNPAY. Kiểm tra appsettings.json.");
        //        }

        //        VnPayLibrary vnpay = new VnPayLibrary();

        //        // Lấy toàn bộ dữ liệu trả về từ VNPAY
        //        foreach (var key in query.Keys)
        //        {
        //            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
        //            {
        //                vnpay.AddResponseData(key, query[key]);
        //            }
        //        }

        //        // Lấy thông tin quan trọng
        //        long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
        //        string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        //        string vnp_SecureHash = query["vnp_SecureHash"];

        //        // Kiểm tra chữ ký bảo mật
        //        bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

        //        if (checkSignature)
        //        {
        //            if (vnp_ResponseCode == "00")
        //            {
        //                // Gọi API để cập nhật trạng thái hóa đơn thành "Đã thanh toán"
        //                using (var httpClient = new HttpClient())
        //                {
        //                    httpClient.BaseAddress = new Uri(_apiBaseUrl);
        //                    HttpResponseMessage apiResponse = await httpClient.PostAsync($"HoaDon/CapNhatThanhToan/{orderId}", null);

        //                    if (!apiResponse.IsSuccessStatusCode)
        //                    {
        //                        // Lỗi API không cập nhật được DB
        //                        return BadRequest("LỖI DB: Không cập nhật được trạng thái hóa đơn.");
        //                    }
        //                }

        //                ViewBag.Message = "Giao dịch thành công!";
        //                ViewBag.MaHD = orderId;
        //                return View("ThanhToanThanhCong");
        //            }
        //            else
        //            {
        //                // Thanh toán thất bại
        //                TempData["ErrorMessage"] = $"Giao dịch thất bại. Mã lỗi VNPAY: {vnp_ResponseCode}";
        //                return RedirectToAction("XemGioHang");
        //            }
        //        }
        //        else
        //        {
        //            // Sai chữ ký
        //            TempData["ErrorMessage"] = "Lỗi: Sai chữ ký VNPAY. Giao dịch bị nghi ngờ giả mạo.";
        //            return RedirectToAction("XemGioHang");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Ghi lỗi ra console để debug
        //        Console.WriteLine($"FATAL VNPAY CALLBACK ERROR: {ex.Message} --- Stack: {ex.StackTrace}");
        //        // TRẢ VỀ LỖI RÕ RÀNG NHẤT
        //        return BadRequest($"LỖI CHƯƠNG TRÌNH: {ex.Message}. Vui lòng kiểm tra Console/Visual Studio.");
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            var query = Request.Query;
            if (query.Count == 0) return RedirectToAction("XemGioHang");

            try
            {
                string vnp_HashSecret = _configuration["VnPay:HashSecret"];
                if (string.IsNullOrEmpty(vnp_HashSecret)) return BadRequest("LỖI CẤU HÌNH VNPAY");

                VnPayLibrary vnpay = new VnPayLibrary();
                foreach (var key in query.Keys)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(key, query[key]);
                    }
                }

                long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_SecureHash = query["vnp_SecureHash"];
                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00")
                    {
                        using (var httpClient = new HttpClient())
                        {
                            httpClient.BaseAddress = new Uri(_apiBaseUrl);

                            // 1. Cập nhật DB
                            HttpResponseMessage apiResponse = await httpClient.PostAsync($"HoaDon/CapNhatThanhToan/{orderId}", null);
                            if (!apiResponse.IsSuccessStatusCode) return BadRequest("Lỗi cập nhật DB.");

                            // 2. Gửi lệnh gửi mail (Không cần Session Email)
                            var mailRequest = new { MaHD = orderId, EmailNguoiNhan = "" };
                            var content = new StringContent(JsonConvert.SerializeObject(mailRequest), Encoding.UTF8, "application/json");

                            _ = httpClient.PostAsync("HoaDon/GuiMailThanhToanThanhCong", content);
                        }

                        ViewBag.Message = "Giao dịch thành công!";
                        ViewBag.MaHD = orderId;
                        return View("ThanhToanThanhCong");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Giao dịch thất bại: {vnp_ResponseCode}";
                        return RedirectToAction("XemGioHang");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi chữ ký VNPAY.";
                    return RedirectToAction("XemGioHang");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi: {ex.Message}");
            }
        }
        // ===================================================================
        // 3. CÁC HÀM HỖ TRỢ GIỎ HÀNG (GIỮ NGUYÊN TỪ CODE CŨ)
        // ===================================================================

        public async Task<IActionResult> XemGioHang()
        {
            var maHV = HttpContext.Session.GetInt32("MaHV");
            if (maHV == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }
            var viewModel = await GetGioHangViewModel(maHV.Value);
            return View(viewModel);
        }

        private async Task<List<GiamGia>> GetAvailableDiscountsFromApi(List<int> maKhoaHocIds)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                string json = JsonConvert.SerializeObject(maKhoaHocIds);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync("GiamGia/GetAvailable", content);

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<GiamGia>>(data) ?? new List<GiamGia>();
                }
            }
            return new List<GiamGia>();
        }

        [HttpPost]
        public async Task<IActionResult> ApplyDiscount(string maCode)
        {
            var maHV = HttpContext.Session.GetInt32("MaHV");
            if (maHV == null) return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(maCode))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã giảm giá.";
                return RedirectToAction("XemGioHang");
            }

            GiamGia discount = await GetDiscountFromApi(maCode);

            if (discount == null)
            {
                TempData["ErrorMessage"] = "Mã không hợp lệ, đã hết hạn hoặc không tồn tại.";
                return RedirectToAction("XemGioHang");
            }

            if (discount.MAKH != null)
            {
                var cartItems = await GetCartItems(maHV.Value);
                bool itemExists = cartItems.Any(i => i.MaKH == discount.MAKH);
                if (!itemExists)
                {
                    TempData["ErrorMessage"] = $"Mã này chỉ áp dụng cho khóa học (ID: {discount.MAKH}) không có trong giỏ của bạn.";
                    return RedirectToAction("XemGioHang");
                }
            }

            HttpContext.Session.SetString(CART_DISCOUNT_CODE, discount.MACODE);
            TempData["SuccessMessage"] = $"Áp dụng mã '{discount.MACODE}' thành công!";

            return RedirectToAction("XemGioHang");
        }

        [HttpPost]
        public IActionResult RemoveDiscount()
        {
            HttpContext.Session.Remove(CART_DISCOUNT_CODE);
            TempData["SuccessMessage"] = "Đã xóa mã giảm giá.";
            return RedirectToAction("XemGioHang");
        }

        private async Task<List<ChiTietGioHang>> GetCartItems(int maHV)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                HttpResponseMessage response = await httpClient.GetAsync($"GioHang/GetCartByMaHV/{maHV}");
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<ChiTietGioHang>>(data) ?? new List<ChiTietGioHang>();
                }
            }
            return new List<ChiTietGioHang>();
        }

        private async Task<GiamGia> GetDiscountFromApi(string maCode)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                HttpResponseMessage response = await httpClient.GetAsync($"GiamGia/GetByCode/{maCode}");
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<GiamGia>(data);
                }
            }
            return null;
        }

        private float CalculateDiscountAmount(GiamGia discount, List<ChiTietGioHang> items)
        {
            float discountableAmount = 0;

            if (discount.MAKH != null)
            {
                var item = items.FirstOrDefault(i => i.MaKH == discount.MAKH);
                discountableAmount = item?.TongTien ?? 0;
            }
            else
            {
                discountableAmount = items.Sum(i => i.TongTien ?? 0);
            }

            if (discount.PHANTRAM > 0)
            {
                return (discountableAmount * (float)discount.PHANTRAM) / 100;
            }
            else
            {
                return (float)discount.GIAMTIEN;
            }
        }

        private async Task<GioHangViewModel> GetGioHangViewModel(int maHV)
        {
            var viewModel = new GioHangViewModel();
            viewModel.Items = await GetCartItems(maHV);
            viewModel.Subtotal = viewModel.Items.Sum(item => item.TongTien ?? 0);

            // Lấy danh sách mã giảm giá (Code cũ giữ nguyên)
            List<int> maKhTrongGio = viewModel.Items.Select(i => i.MaKH).ToList();
            var allDiscounts = await GetAvailableDiscountsFromApi(maKhTrongGio);
            var now = DateTime.Now;
            viewModel.AvailableDiscounts = allDiscounts
                .OrderByDescending(x => x.NGAYKETTHUC >= now && (x.SOLANSUDUNG == null || x.SOLANSUDUNG > 0))
                .ThenBy(x => x.NGAYKETTHUC)
                .ToList();

            // --- XỬ LÝ MÃ ĐANG ÁP DỤNG ---
            string savedCode = HttpContext.Session.GetString(CART_DISCOUNT_CODE);
            if (!string.IsNullOrEmpty(savedCode))
            {
                viewModel.AppliedDiscount = await GetDiscountFromApi(savedCode);
            }

            if (viewModel.AppliedDiscount != null)
            {
                bool isValidForCart = (viewModel.AppliedDiscount.MAKH == null) ||
                                      (viewModel.Items.Any(i => i.MaKH == viewModel.AppliedDiscount.MAKH));

                if (isValidForCart)
                {
                    // [MỚI] TÍNH TIỀN GIẢM CHO TỪNG ITEM ĐỂ HIỂN THỊ
                    foreach (var item in viewModel.Items)
                    {
                        // Kiểm tra xem item này có được hưởng mã không
                        bool isItemEligible = (viewModel.AppliedDiscount.MAKH == null) || // Mã toàn sàn
                                              (viewModel.AppliedDiscount.MAKH == item.MaKH); // Mã chỉ định

                        if (isItemEligible)
                        {
                            if (viewModel.AppliedDiscount.PHANTRAM > 0)
                            {
                                // Giảm theo %
                                item.TienGiam = (item.TongTien ?? 0) * (float)viewModel.AppliedDiscount.PHANTRAM / 100;
                            }
                            else if (viewModel.AppliedDiscount.MAKH != null)
                            {
                                // Giảm tiền mặt CỤ THỂ cho khóa học này
                                // (Lưu ý: Nếu giảm tiền mặt cho toàn sàn thì KHÔNG chia đều vào đây để tránh sai số, chỉ trừ tổng)
                                item.TienGiam = (float)viewModel.AppliedDiscount.GIAMTIEN;
                            }
                        }
                    }

                    // Tính tổng giảm giá dựa trên các item đã tính
                    viewModel.DiscountAmount = viewModel.Items.Sum(i => i.TienGiam);

                    // Trường hợp giảm tiền mặt toàn sàn (Fixed Amount Global), cộng thêm vào tổng
                    if (viewModel.AppliedDiscount.MAKH == null && viewModel.AppliedDiscount.GIAMTIEN > 0 && viewModel.AppliedDiscount.PHANTRAM == null)
                    {
                        viewModel.DiscountAmount = (float)viewModel.AppliedDiscount.GIAMTIEN;
                        // Lưu ý: Trường hợp này từng item sẽ không hiện tiền giảm (vì khó chia đều), chỉ hiện tổng giảm ở dưới.
                    }
                }
                else
                {
                    HttpContext.Session.Remove(CART_DISCOUNT_CODE);
                    viewModel.AppliedDiscount = null;
                }
            }

            //viewModel.TotalPayable = viewModel.Subtotal - viewModel.DiscountAmount;
            // 1. Lấy điểm hiện có của User (Gọi API GetProfile)
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_apiBaseUrl);
                var res = await client.GetAsync($"HocVien/GetProfile/{HttpContext.Session.GetInt32("MaHV")}"); // Hoặc API lấy user info
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadAsStringAsync();
                    var user = JsonConvert.DeserializeObject<dynamic>(data);
                    viewModel.UserPoints = (int)(user.tichDiem ?? 0); // Giả sử API trả về diemTichLuy
                }
            }

            // 2. Tính tiền giảm từ điểm (Lấy từ Session)
            int pointsToUse = HttpContext.Session.GetInt32("PointsToUse") ?? 0;

            // Validate: Không được dùng quá số điểm hiện có
            if (pointsToUse > viewModel.UserPoints) pointsToUse = viewModel.UserPoints;

            viewModel.PointsToUse = pointsToUse;
            viewModel.PointDiscountAmount = pointsToUse * 1000; // 1 điểm = 1000đ

            // 3. Tính tổng thanh toán cuối cùng
            // Total = Subtotal - CouponDiscount - PointDiscount
            viewModel.TotalPayable = viewModel.Subtotal - viewModel.DiscountAmount - viewModel.PointDiscountAmount;
            if (viewModel.TotalPayable < 0) viewModel.TotalPayable = 0;

            return viewModel;
        }

        [HttpPost]
        public async Task<IActionResult> XoaKhoiGioHang(int maKH)
        {
            var maHV = HttpContext.Session.GetInt32("MaHV");
            if (maHV == null) return RedirectToAction("Login", "Account");

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_apiBaseUrl);
                // Gọi API xóa (Bạn cần đảm bảo API này đã tồn tại bên Backend)
                var response = await httpClient.DeleteAsync($"GioHang/Delete/{maHV}/{maKH}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Đã xóa khóa học khỏi giỏ hàng.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi khi xóa khóa học.";
                }
            }
            return RedirectToAction("XemGioHang");
        }
    }
}

