using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NiiePay.Entities;
using NiiePay.Models;

namespace NiiePay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LichSuGiaoDichController : ControllerBase
    {
        private readonly NiiePayContext _context;

        public LichSuGiaoDichController(NiiePayContext context)
        {
            _context = context;
        }
        [HttpGet("LayLichSu")]
        public async Task<IActionResult> GetLichSuGiaoDich(
            [FromQuery] string soTaiKhoan,
            [FromQuery] DateTime tuNgay,
            [FromQuery] DateTime denNgay)
        {
            if (string.IsNullOrWhiteSpace(soTaiKhoan))
                return BadRequest(new ApiResponse { Status = "FAIL", Message = "Vui lòng nhập số tài khoản." });

            var denNgayEnd = denNgay.Date.AddDays(1).AddTicks(-1);

  
            var query = from g in _context.GiaoDiches
                        where g.TaiKhoanSoHuu == soTaiKhoan
                           && g.TrangThai == "SUCCESS"
                           && g.ThoiGian >= tuNgay
                           && g.ThoiGian <= denNgayEnd
                        let taiKhoanDoiUng = g.LoaiGiaoDich == "M_out" ? g.TaiKhoanNhan : g.TaiKhoanGui
                        join a in _context.Accounts on taiKhoanDoiUng equals a.SoTaiKhoan into accountGroup
                        from acc in accountGroup.DefaultIfEmpty()
                        orderby g.ThoiGian descending
                        select new
                        {
                            MaGiaoDich = g.MaGiaoDich,
                            ThoiGian = g.ThoiGian,
                            LoaiGiaoDich = g.LoaiGiaoDich, // Vẫn lấy lên để làm điều kiện IF
                            SoTaiKhoanGiaoDich = taiKhoanDoiUng,
                            HoTenTaiKhoanGiaoDich = acc != null ? acc.HoTenChuThe : "Khách hàng liên ngân hàng",
                            SoTienRaw = g.SoTien,          // Tiền thô (dạng số)
                            SoDuSauGiaoDich = g.SoDuSauGiaoDich,
                            NoiDung = g.NoiDung
                        };

   
            var rawData = await query.ToListAsync();

       
            var result = rawData.Select(x => new LichSuGiaoDich
            {
                MaGiaoDich = x.MaGiaoDich,
                ThoiGian = x.ThoiGian,
                SoTaiKhoanGiaoDich = x.SoTaiKhoanGiaoDich,
                HoTenTaiKhoanGiaoDich = x.HoTenTaiKhoanGiaoDich,

                // Xử lý Logic hiển thị số tiền:
                // :N0 giúp format tiền tệ có dấu phẩy (Ví dụ: 500000 -> 500,000)
                SoTien = x.LoaiGiaoDich == "M_in" ? $"+{x.SoTienRaw:N0}" : $"-{x.SoTienRaw:N0}",

                SoDuSauGiaoDich = x.SoDuSauGiaoDich,
                NoiDung = x.NoiDung
            }).ToList();

            return Ok(new ApiResponseGeneric
            {
                Status = "SUCCESS",
                Message = "Truy vấn lịch sử thành công.",
                Data = result
            });
        }
    }
}
