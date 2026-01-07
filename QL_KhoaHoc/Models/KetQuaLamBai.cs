namespace QL_KhoaHoc.Models
{
    public class KetQuaLamBai
    {
        public int MaHV { get; set; }
        public string Type { get; set; } // "TN" (Trắc nghiệm) hoặc "BT" (Bài tập)
        public int IdRef { get; set; }   // ID của bài TN hoặc BT
        public double DiemSo { get; set; }
        public int SoCauDung { get; set; }
        public int TongSoCau { get; set; }
        public List<ChiTietTraLoi>? ChiTiet { get; set; }
    }

    // Class chi tiết từng câu
    public class ChiTietTraLoi
    {
        public int MaCauHoi { get; set; }    // ID câu hỏi
        public int? MaDA { get; set; }       // ID đáp án chọn (cho Trắc nghiệm)
        public string? CauTraLoi { get; set; } // Nội dung text (cho Bài tập)
        public bool IsCorrect { get; set; }   // Đúng hay sai
    }
}
