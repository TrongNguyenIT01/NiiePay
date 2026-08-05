using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NiiePay.Entities;

namespace NiiePay.Controllers
{
    // Đường dẫn vẫn giữ nguyên như tài liệu yêu cầu: api/savings
    [Route("api/savings")]
    [ApiController]
    public class LaiSuatController : ControllerBase // Tên class tiếng Việt, CÓ đuôi Controller
    {
        private readonly NiiePayContext _context;

        public LaiSuatController(NiiePayContext context)
        {
            _context = context;
        }

        // Đường dẫn API đầy đủ sẽ là: GET /api/savings/rates
        [HttpGet("rates")]
        public async Task<IActionResult> LayDanhSachLaiSuat() // Tên hàm tiếng Việt
        {
            var rates = await _context.LaiSuatKyHan
                .OrderBy(r => r.TermMonths)
                .Select(r => new {
                    termMonths = r.TermMonths,
                    interestRate = r.InterestRate
                })
                .ToListAsync();

            return Ok(rates);
        }
    }
}
