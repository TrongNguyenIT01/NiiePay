using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NiiePay.Entities;
using NiiePay.Models;
using System.Linq;

namespace NiiePay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TruyVanController : ControllerBase
    {
        private readonly NiiePayContext _context;

        public TruyVanController(NiiePayContext context)
        {
            _context = context;
        }

        // GET api/truyvan/search?query={query}
        // Search by account number, phone or CCCD
        // If query is omitted or empty, returns all accounts (summary)
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? query = null)
        {
            var q = _context.Accounts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                q = q.Where(a => a.SoTaiKhoan.Contains(query) || a.SoDienThoai.Contains(query) || a.Cccd.Contains(query));
            }

            var results = await q
                .Select(a => new
                {
                    a.SoTaiKhoan,
                    a.HoTenChuThe,
                    a.SoDienThoai,
                    a.Cccd,
                    a.MaNganHang,
                    a.SoDuKhaDung
                })
                .ToListAsync();

            if (results.Count == 0)
            {
                return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Không tìm thấy tài khoản.", Count = 0, Data = results });
            }

            return Ok(new ApiResponseGeneric { Status = "SUCCESS", Message = "Tìm thấy tài khoản.", Count = results.Count, Data = results });
        }
    }
}
