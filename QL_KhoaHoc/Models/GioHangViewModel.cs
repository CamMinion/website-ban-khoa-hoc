using System.Collections.Generic;

namespace QL_KhoaHoc.Models
{
    // Model này sẽ bọc mọi thứ mà View CuaToi.cshtml cần
    public class GioHangViewModel
    {
        public List<ChiTietGioHang> Items { get; set; }
        public GiamGia AppliedDiscount { get; set; }
        public float Subtotal { get; set; }      // Tổng tiền hàng
        public float DiscountAmount { get; set; } // Số tiền được giảm
        public float TotalPayable { get; set; }   // Tiền cuối cùng
        // === THÊM DÒNG NÀY ===
        public List<GiamGia> AvailableDiscounts { get; set; }

        // [MỚI]
        public int UserPoints { get; set; } // Điểm hiện có của user
        public int PointsToUse { get; set; } // Điểm user muốn dùng
        public float PointDiscountAmount { get; set; } // Số tiền giảm từ điểm

        public GioHangViewModel()
        {
            Items = new List<ChiTietGioHang>();
            AppliedDiscount = null;

            // === VÀ THÊM DÒNG NÀY ===
            AvailableDiscounts = new List<GiamGia>();
        }
    }
}
