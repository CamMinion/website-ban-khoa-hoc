
namespace QL_KhoaHoc.Models
{
    public class HocVien
    {
        // --- Từ bảng TAIKHOAN ---
        public int MaTK { get; set; }
        public string TenDangNhap { get; set; } // TENDN
        public string Email { get; set; }       // EMAIL
        public string SoDienThoai { get; set; } // SDT
        public string AnhDaiDien { get; set; }  // ANHDAIDIEN
        public DateTime? NgayTao { get; set; }  // NGAYTAO

        // --- Từ bảng HOCVIEN ---
        public int MaHV { get; set; }
        public double TichDiem { get; set; }    // TICHDIEM (Float)
    }
}
