using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QL_KhoaHoc.Models
{
    public class GiangVien
    {
        public int MaTK { get; set; } // Dùng để xác định giảng viên

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chức danh")]
        public string ChucDanh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiểu sử")]
        [StringLength(2000, ErrorMessage = "Tiểu sử không quá 2000 ký tự")]
        public string TieuSu { get; set; }

        public string? Website { get; set; }
        public string? Facebook { get; set; }
        public string? LinkedIn { get; set; }
        public string? Youtube { get; set; }

        // --- Thông tin ngân hàng ---
        [Required(ErrorMessage = "Vui lòng chọn ngân hàng")]
        public string TenNH { get; set; } // Sẽ dùng Dropdown

        [Required(ErrorMessage = "Vui lòng nhập số tài khoản")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "Số tài khoản phải từ 8 đến 20 ký tự")]
        [RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = "Số tài khoản không được chứa ký tự đặc biệt")]
        public string STK { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản")]
        [RegularExpression(@"^[A-Z\s]*$", ErrorMessage = "Tên chủ tài khoản phải là IN HOA KHÔNG DẤU (VD: NGUYEN VAN A)")]
        public string TenChuThe { get; set; }

        public DateTime? NgayHetHan { get; set; }

        public string? CCCD_MatTruoc { get; set; } // Lưu URL ảnh
        public string? CCCD_MatSau { get; set; }   // Lưu URL ảnh
        [NotMapped] // Không lưu vào DB, chỉ để hứng file upload
        public IFormFile? FileMatTruoc { get; set; }

        [NotMapped]
        public IFormFile? FileMatSau { get; set; }

        public string? TrangThaiHoSo { get; set; } // Map với cột TRANGTHAI của bảng TKNGANHANG
    }
}
