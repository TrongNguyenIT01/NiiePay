using System;
using System.Collections.Generic;

namespace NiiePay.Entities;

public partial class GiaoDich
{
    public string MaGiaoDich { get; set; } = null!;

    public string TaiKhoanGui { get; set; } = null!;

    public string TaiKhoanNhan { get; set; } = null!;

    public string MaNganHang { get; set; } = null!;

    public decimal SoTien { get; set; }

    public DateTime? ThoiGian { get; set; }

    public decimal? SoDuSauGiaoDich { get; set; }

    public string? NoiDung { get; set; }

    public string TrangThai { get; set; } = null!;
    public string TaiKhoanSoHuu { get; set; } = null!;
    public string LoaiGiaoDich { get; set; } = null!;

    public virtual NganHang MaNganHangNavigation { get; set; } = null!;

    public virtual Account TaiKhoanGuiNavigation { get; set; } = null!;
}
