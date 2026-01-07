using System.ComponentModel.DataAnnotations;

namespace QL_KhoaHoc.Models
{
    public class TaiKhoan
    {
    }

    public class RegisterModel
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Họ tên không quá 50 ký tự.")]
        public string TenDN { get; set; } = string.Empty;  // TENDN = Họ tên

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6-20 ký tự.")]
        public string MatKhau { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhập lại mật khẩu.")]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu không khớp.")]
        public string NhapLaiMatKhau { get; set; } = string.Empty;

        public bool AsGiangVien { get; set; }  // <- mới thêm
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
        public string TenDN { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        public string MatKhau { get; set; } = string.Empty;
    }

    public class SendOTPModel
    {
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyOTPModel
    {
        public string OTP { get; set; } = string.Empty;
    }
}
