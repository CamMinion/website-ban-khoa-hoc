namespace QL_KhoaHoc.Models
{
    public class DanhMuc
    {
        public int MaDanhMuc { get; set; }
        public string TenDanhMuc { get; set; }
        public List<DanhMucCon> DanhMucCons { get; set; } = new List<DanhMucCon>();
    }
    public class DanhMucCon
    {
        public int MaDanhMucCon { get; set; }
        public string TenDanhMucCon { get; set; }
        public int MaDm { get; set; }
    }
}
