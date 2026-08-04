using System;
using System.Collections.Generic;

namespace NiiePay.Entities;

public partial class NganHang
{
    public string MaNganHang { get; set; } = null!;

    public string TenNganHang { get; set; } = null!;

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<GiaoDich> GiaoDiches { get; set; } = new List<GiaoDich>();
}
