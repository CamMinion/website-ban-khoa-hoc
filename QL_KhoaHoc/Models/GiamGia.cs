namespace QL_KhoaHoc.Models
{
    public class GiamGia
    {
        // (Nội dung y hệt file GiamGia.cs của API)
        public int MAGG { get; set; }
        public string MACODE { get; set; }
        public string MOTA { get; set; }
        public float? PHANTRAM { get; set; }
        public float? GIAMTIEN { get; set; }
        public DateTime NGAYBATDAU { get; set; }
        public DateTime NGAYKETTHUC { get; set; }
        public int? SOLANSUDUNG { get; set; }
        public bool CONHIEULUC { get; set; }
        public int? MAKH { get; set; }
    }
}
