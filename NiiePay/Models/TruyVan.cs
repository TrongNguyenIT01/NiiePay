using System;
using System.Collections.Generic;

namespace NiiePay.Models
{
    // DTO for account query (truy vấn) - no transaction history included here
    public class AccountDto
    {
        public string SoTaiKhoan { get; set; } = null!;
        public string MaNganHang { get; set; } = null!;
        public string HoTenChuThe { get; set; } = null!;
        public string SoDienThoai { get; set; } = null!;
        public string Cccd { get; set; } = null!;
        // ISO date string for compatibility
        public string? NgayHetHan { get; set; }
        public decimal SoDuKhaDung { get; set; }
        public DateTime? ThoiGianTao { get; set; }
    }
    
}
