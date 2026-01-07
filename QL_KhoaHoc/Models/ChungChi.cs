namespace QL_KhoaHoc.Models
{
    public class ChungChi
    {
        public string TenKhoaHoc { get; set; }
        public string MaCode { get; set; } // Mã chứng chỉ (VD: CERT-2023...)
        public DateTime NgayCap { get; set; }
        public string TenGiangVien { get; set; }
        public string AnhBiaKhoaHoc { get; set; } // Để hiển thị cho đẹp
    }
}
