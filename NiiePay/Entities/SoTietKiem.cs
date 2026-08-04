using System;
using System.Collections.Generic;

namespace NiiePay.Entities;

public partial class SoTietKiem
{
    public string MaSoTietKiem { get; set; } = null!;

    public string SoTaiKhoan { get; set; } = null!;

    public decimal SoTienGui { get; set; }

    public int KyHan { get; set; }

    public decimal LaiSuat { get; set; }

    public DateOnly? NgayMoSo { get; set; }

    public DateOnly NgayHetHan { get; set; }

    public bool TuDongGiaHan { get; set; }

    public virtual Account SoTaiKhoanNavigation { get; set; } = null!;
}
