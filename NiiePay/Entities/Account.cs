using System;
using System.Collections.Generic;

namespace NiiePay.Entities;

public partial class Account
{
    public string SoTaiKhoan { get; set; } = null!;

    public string MaNganHang { get; set; } = null!;

    public string HoTenChuThe { get; set; } = null!;

    public string SoDienThoai { get; set; } = null!;

    public string Cccd { get; set; } = null!;

    public DateOnly? NgayHetHan { get; set; }

    public decimal SoDuKhaDung { get; set; }

    public DateTime? ThoiGianTao { get; set; }

    public virtual ICollection<GiaoDich> GiaoDiches { get; set; } = new List<GiaoDich>();

    public virtual NganHang MaNganHangNavigation { get; set; } = null!;

    public virtual ICollection<SoTietKiem> SoTietKiems { get; set; } = new List<SoTietKiem>();
}
