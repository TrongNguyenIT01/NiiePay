namespace NiiePay.Models
{
    // Request DTO for interbank transfer
    public class ChuyenKhoanLienNganHangRequest
    {
        public string TaiKhoanGui { get; set; } = null!;
        public string TaiKhoanNhan { get; set; } = null!;
        public string MaNganHang { get; set; } = null!;
        public decimal SoTien { get; set; }
        public string? NoiDung { get; set; }
    }
}
