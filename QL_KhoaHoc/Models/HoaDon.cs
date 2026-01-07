namespace QL_KhoaHoc.Models
{
    public class HoaDon
    {
        public int MaHD { get; set; }
        public int MaHV { get; set; }
        public int? MaGG { get; set; }
        public float ThanhTien { get; set; }
        public DateTime NgayTao { get; set; }
        public string TrangThai { get; set; }
    }

    public class ThanhToanViewModel
    {
        public HoaDon HoaDon { get; set; }
        public string QrDataURL { get; set; } // Để chứa chuỗi Base64
        public string ErrorMessage { get; set; } // Nếu có lỗi tạo QR
    }
}
