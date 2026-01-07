namespace QL_KhoaHoc.Models
{
    public class BankList
    {
        public static List<string> GetBanks()
        {
            // Danh sách các ngân hàng phổ biến tại VN
            return new List<string> {
            "Vietcombank", "Techcombank", "MB Bank", "VietinBank", "BIDV", "Agribank",
            "ACB", "VPBank", "TPBank", "Sacombank", "VIB", "MSB", "OCB", "HDBank",
            "SHB", "SeABank", "Eximbank", "Nam A Bank", "Viet Capital Bank (BVBank)"
        };
        }
    }
}
