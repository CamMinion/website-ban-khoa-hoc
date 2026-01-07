using Microsoft.AspNetCore.Mvc;
using QL_KhoaHoc.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace QL_KhoaHoc.Controllers
{
    public class HomeController : Controller
    {
        // Đảm bảo port API đúng với project API của bạn
        private readonly string _apiBaseUrl = "http://localhost:5105/api/KhoaHoc";

        public async Task<IActionResult> Index(int page = 1)
        {
            // CẤU HÌNH: 16 khóa học mỗi trang (4 ngang x 4 dọc)
            int pageSize = 16;

            List<KhoaHoc> dsKhoaHoc = new List<KhoaHoc>();

            using (HttpClient client = new HttpClient())
            {
                // Gọi API lấy toàn bộ danh sách
                // (Thực tế nên viết API phân trang riêng để tối ưu hiệu năng nếu dữ liệu lớn)
                HttpResponseMessage response = await client.GetAsync(_apiBaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<KhoaHoc>>(data);
                    if (result != null)
                    {
                        // Chỉ lấy khóa học đã phát hành và đã duyệt (nếu API chưa lọc)
                        dsKhoaHoc = result.Where(x => x.DaPhatHanh == true).ToList();
                    }
                }
            }

            // --- LOGIC PHÂN TRANG SERVER-SIDE ---
            var totalItems = dsKhoaHoc.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Kiểm tra trang hợp lệ
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // Cắt dữ liệu
            var pagedList = dsKhoaHoc
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Truyền thông tin sang View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedList);
        }
    }
}