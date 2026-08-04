using System.ComponentModel.DataAnnotations;

namespace NiiePay.Models
{
    public class DangKy
    {
        [Required(ErrorMessage = "Mã ngân hàng không được để trống")]
        public string MaNganHang { get; set; } = null!;

        [Required(ErrorMessage = "Số tài khoản không được để trống")]
        public string SoTaiKhoan { get; set; } = null!;

        [Required(ErrorMessage = "Họ tên chủ thẻ không được để trống")]
        public string HoTenChuThe { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        public string SoDienThoai { get; set; } = null!;

        [Required(ErrorMessage = "CCCD không được để trống")]
        public string CCCD { get; set; } = null!;

        public DateTime? NgayHetHan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số dư ban đầu")]
        public decimal SoDuBanDau { get; set; }
    }
}
