using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace QL_KhoaHoc.ViewComponents
{
    public class CartBadgeViewComponent : ViewComponent
    {
        private readonly string _apiBaseUrl = "http://localhost:5105/api/"; // Đổi port cho đúng

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int count = 0;
            // Lấy MaHV từ Session
            int? maHV = HttpContext.Session.GetInt32("MaHV");

            if (maHV.HasValue)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_apiBaseUrl);
                    try
                    {
                        var response = await client.GetAsync($"GioHang/Count/{maHV}");
                        if (response.IsSuccessStatusCode)
                        {
                            var data = await response.Content.ReadAsStringAsync();
                            var result = JsonConvert.DeserializeObject<dynamic>(data);
                            count = (int)result.count;
                        }
                    }
                    catch { /* Nếu lỗi API thì count vẫn là 0 */ }
                }
            }

            return View(count); // Trả về số lượng (int)
        }
    }
}