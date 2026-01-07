//using System.ComponentModel.DataAnnotations;

//namespace QL_KhoaHoc.Models
//{
//    public class KhoaHoc
//    {

//        public int MaKhoaHoc { get; set; }


//        public int MaGiangVien { get; set; }

//        //[Required(ErrorMessage = "Mã danh mục là bắt buộc.")]
//        public int? MaDanhMuc { get; set; }  // Nullable OK

//        [Required(ErrorMessage = "Tên khóa học là bắt buộc.")]
//        [StringLength(500, ErrorMessage = "Tên không quá 500 ký tự.")]
//        public string TenKhoaHoc { get; set; } = string.Empty;  // Default empty string

//        public string? MoTaNgan { get; set; } = string.Empty;  // Default empty

//        //[Range(0, double.MaxValue, ErrorMessage = "Giá tiền >= 0.")]
//        public float? GiaTien { get; set; } = 0;  // Default 0 nếu null

//        public bool DaPhatHanh { get; set; } = false;  // Default false

//        public DateTime? NgayPhatHanh { get; set; }  // Nullable OK, không default

//        [StringLength(500)]
//        public string? AnhBia { get; set; } = string.Empty;  // Default empty

//        public DateTime? NgayTao { get; set; } = DateTime.Now;  // Default Now nếu null

//        public string? MucTieu { get; set; } = string.Empty;

//        public string? DoiTuong { get; set; } = string.Empty;

//        public string? YeuCau { get; set; } = string.Empty;

//        public DateTime? NgayCapNhat { get; set; }  // Nullable, set sau nếu cần

//        public int SoDanhGia { get; set; } = 0;  // Default 0

//        public int SoDangKy { get; set; } = 0;  // Default 0

//        [StringLength(1000)]
//        public string? TinNhanChaoMung { get; set; } = string.Empty;
//        public string? TrangThai { get; set; } = "Nhap"; // Default "Chờ duyệt"

//    }

//    // Model cho yêu cầu tạo (extend từ KhoaHoc để bao gồm các phần con)
//    public class KhoaHocCreateModel : KhoaHoc
//    {
//        public List<ChuongMucCreateModel>? ChuongMucs { get; set; }
//    }

//    public class ChuongMucCreateModel
//    {
//        public string? TenChuong { get; set; }
//        public int? ThuTu { get; set; }
//        public List<BaiGiangCreateModel>? BaiGiangs { get; set; }
//        public List<TracNghiemCreateModel>? TracNghiems { get; set; }
//        public List<BaiTapCreateModel>? BaiTaps { get; set; }
//    }

//    public class BaiGiangCreateModel
//    {
//        public string? Video { get; set; }
//        public string? BaiViet { get; set; }
//        //public string? FileTaiLen { get; set; }
//        public IFormFile? VideoFile { get; set; }  // Tạm để nhận file upload
//        public string? FileTaiXuong { get; set; }
//        public int? ThuTu { get; set; }
//    }

//    public class TracNghiemCreateModel
//    {
//        public string? TieuDe { get; set; }
//        public int? ThuTu { get; set; }
//        public List<CauHoiTNCreateModel>? CauHois { get; set; }
//    }

//    public class CauHoiTNCreateModel
//    {
//        public string? NoiDung { get; set; }
//        public List<DapAnCreateModel>? DapAns { get; set; }
//    }

//    public class DapAnCreateModel
//    {
//        public string? NoiDung { get; set; }
//        public string? GiaiThich { get; set; }
//        public bool KetQua { get; set; }
//    }

//    public class BaiTapCreateModel
//    {
//        public string? TieuDe { get; set; }
//        public int? ThoiLuong { get; set; }
//        public string? HuongDan { get; set; }
//        public int? ThuTu { get; set; }
//        public List<CauHoiBTCreateModel>? CauHois { get; set; }
//    }

//    public class CauHoiBTCreateModel
//    {
//        public string? NoiDung { get; set; }
//        public string? DapAnMau { get; set; }
//    }
//}


using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // ⭐ THÊM DÒNG NÀY ĐỂ KHÔNG BỊ LỖI IFormFile
using System.Collections.Generic; // Cần thiết cho List
using System; // Cần thiết cho DateTime

namespace QL_KhoaHoc.Models
{
    public class KhoaHoc
    {
        public int MaKhoaHoc { get; set; }
        public int MaGiangVien { get; set; }
        public int? MaDanhMuc { get; set; }

        [Required(ErrorMessage = "Tên khóa học là bắt buộc.")]
        [StringLength(500, ErrorMessage = "Tên không quá 500 ký tự.")]
        public string TenKhoaHoc { get; set; } = string.Empty;

        public string? MoTaNgan { get; set; } = string.Empty;

        public float? GiaTien { get; set; } = 0;

        public bool DaPhatHanh { get; set; } = false;

        public DateTime? NgayPhatHanh { get; set; }

        [StringLength(500)]
        public string? AnhBia { get; set; } = string.Empty;

        public DateTime? NgayTao { get; set; } = DateTime.Now;

        public string? MucTieu { get; set; } = string.Empty;

        public string? DoiTuong { get; set; } = string.Empty;

        public string? YeuCau { get; set; } = string.Empty;

        public DateTime? NgayCapNhat { get; set; }

        public int SoDanhGia { get; set; } = 0;

        public int SoDangKy { get; set; } = 0;

        [StringLength(1000)]
        public string? TinNhanChaoMung { get; set; } = string.Empty;
        public string? TrangThai { get; set; } = "Nhap"; // Default "Nhap"
        public double TienDo { get; set; } = 0;
        public string? LyDoTuChoi { get; set; } // Thêm lý do từ chối

    }

    // Model cho yêu cầu tạo (extend từ KhoaHoc để bao gồm các phần con)
    public class KhoaHocCreateModel : KhoaHoc
    {
        public List<ChuongMucCreateModel>? ChuongMucs { get; set; }
        public List<DanhGiaViewModel> DanhSachDanhGia { get; set; } = new List<DanhGiaViewModel>();
        // [THÊM MỚI] Biến nhận file upload (Ảnh hoặc Video) cho Ảnh bìa
        public IFormFile? AnhBiaFile { get; set; }

        // [THÊM MỚI 2 DÒNG NÀY]
        public string? TenGiangVien { get; set; }
        public string? TenDanhMuc { get; set; }
    }

    public class ChuongMucCreateModel
    {
        public int? MaCM { get; set; } // ⭐ THÊM ID
        public string? TenChuong { get; set; }
        public int? ThuTu { get; set; }
        public List<BaiGiangCreateModel>? BaiGiangs { get; set; }
        public List<TracNghiemCreateModel>? TracNghiems { get; set; }
        public List<BaiTapCreateModel>? BaiTaps { get; set; }
    }

    public class BaiGiangCreateModel
    {
        public int? MaBG { get; set; } // ⭐ THÊM ID
        public string? Video { get; set; }
        public string? BaiViet { get; set; }
        public IFormFile? VideoFile { get; set; }
        public string? FileTaiXuong { get; set; }
        // [THÊM MỚI] Để nhận file tài liệu từ Form
        public IFormFile? TaiLieuFile { get; set; }
        public int? ThuTu { get; set; }
    }

    public class TracNghiemCreateModel
    {
        public int? MaTN { get; set; } // ⭐ THÊM ID
        public string? TieuDe { get; set; }
        public int? ThuTu { get; set; }
        public List<CauHoiTNCreateModel>? CauHois { get; set; }
    }

    public class CauHoiTNCreateModel
    {
        public int? MaCH { get; set; } // ⭐ THÊM ID
        public string? NoiDung { get; set; }
        public List<DapAnCreateModel>? DapAns { get; set; }
    }

    public class DapAnCreateModel
    {
        public int? MaDA { get; set; } // ⭐ THÊM ID
        public string? NoiDung { get; set; }
        public string? GiaiThich { get; set; }
        public bool KetQua { get; set; }
    }

    public class BaiTapCreateModel
    {
        public int? MaBT { get; set; } // ⭐ THÊM ID
        public string? TieuDe { get; set; }
        public int? ThoiLuong { get; set; }
        public string? HuongDan { get; set; }
        // [THÊM MỚI] Biến để nhận file hướng dẫn từ form
        public IFormFile? HuongDanFile { get; set; }
        public int? ThuTu { get; set; }
        public List<CauHoiBTCreateModel>? CauHois { get; set; }
    }

    public class CauHoiBTCreateModel
    {
        public int? MaCauHoi { get; set; } // ⭐ THÊM ID
        public string? NoiDung { get; set; }
        public string? DapAnMau { get; set; }
    }

    public class DanhGiaViewModel
    {
        public string TenHocVien { get; set; }
        public int SoSao { get; set; }
        public string NoiDung { get; set; }
        public DateTime NgayDanhGia { get; set; }
        // Có thể thêm Avatar nếu muốn
    }
}