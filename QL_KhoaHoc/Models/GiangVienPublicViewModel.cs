namespace QL_KhoaHoc.Models
{
    public class GiangVienPublicViewModel
    {
        // Thông tin cá nhân
        public int MaGV { get; set; }
        public string HoTen { get; set; }
        public string ChucDanh { get; set; }
        public string TieuSu { get; set; }
        public string AnhDaiDien { get; set; }
        public string Website { get; set; }
        public string Facebook { get; set; }
        public string LinkedIn { get; set; }
        public string Youtube { get; set; }

        // Danh sách khóa học
        public List<KhoaHoc> KhoaHocs { get; set; }

        // Thống kê (Optional)
        public int TongHocVien { get; set; }
        public int TongDanhGia { get; set; }
    }
}
