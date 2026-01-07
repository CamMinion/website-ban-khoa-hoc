using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QL_KhoaHoc.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;


namespace QL_KhoaHoc.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        // Lưu ý: Đổi port 5105 thành port thực tế của project API bạn đang chạy
        private readonly string _apiBaseUrl = "http://localhost:5105/api/";

        public async Task<IViewComponentResult> InvokeAsync()
        {
            List<DanhMuc> categories = new List<DanhMuc>();

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_apiBaseUrl);
                try
                {
                    // Gọi API lấy danh mục phân cấp (API này cần được viết bên Backend trước)
                    var response = await client.GetAsync("DanhMuc/GetMenuDanhMuc");

                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadAsStringAsync();
                        categories = JsonConvert.DeserializeObject<List<DanhMuc>>(data);
                    }
                }
                catch (Exception)
                {
                    // Nếu lỗi kết nối API, trả về danh sách rỗng để không sập web
                    categories = new List<DanhMuc>();
                }
            }

            // Trả về View mặc định của Component (Views/Shared/Components/CategoryMenu/Default.cshtml)
            return View(categories);
        }
    }
}