namespace QL_KhoaHoc.Models
{
    public class ThongKeThuNhapGV
    {
        public int MaKH { get; set; }
        public string TenKhoaHoc { get; set; }
        public int SoLuongBan { get; set; }
        public double DonGiaGoc { get; set; } // Giá niêm yết
        public double TongDoanhThuHeThong { get; set; } // Tổng tiền bán được (chưa trừ KM)
        public double DoanhThuGiangVien { get; set; } // 60% của Tổng doanh thu hệ thống
    }
}
