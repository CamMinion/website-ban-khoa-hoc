namespace QL_KhoaHoc.Models
{
    public class GioHang
    {
        public int MaHV { get; set; }
        public int MaKH { get; set; }
    }
    public class ChiTietGioHang
    {
        public int MaGH { get; set; }
        public int MaKH { get; set; }
        public int SoLuong { get; set; }
        public float DonGia { get; set; }
        public float? TongTien { get; set; }

        // Các trường mở rộng từ JOIN
        public string? TenKhoaHoc { get; set; }
        public string? AnhBia { get; set; }
        // [MỚI] Thêm thuộc tính này để lưu tiền giảm hiển thị ra View
        public float TienGiam { get; set; } = 0;
    }
}
