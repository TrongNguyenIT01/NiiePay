namespace NiiePay.Models
{
    public class LichSuGiaoDich
    {
        public string MaGiaoDich { get; set; } = null!;
        public DateTime? ThoiGian { get; set; }



        public string? SoTaiKhoanGiaoDich { get; set; }
        public string? HoTenTaiKhoanGiaoDich { get; set; }


        public string SoTien { get; set; } = null!;

        public decimal? SoDuSauGiaoDich { get; set; }
        public string? NoiDung { get; set; }
    }
}
