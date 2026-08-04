using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NiiePay.Entities;
using NiiePay.Models;
namespace NiiePay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DangKyController : ControllerBase
    {
        private readonly NiiePayContext _context;

        public DangKyController(NiiePayContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAccount([FromBody] DangKy request)
        {
            // 1. Kiểm tra số dư khả dụng tối thiểu
            if (request.SoDuBanDau < 50000)
            {
                return Ok(new ApiResponse
                {
                    Status = "FAIL",
                    Message = "Số dư ban đầu tối thiểu phải là 50,000đ."
                });
            }

            // 2. Kiểm tra Mã ngân hàng có tồn tại không
            var bankExists = await _context.NganHangs
                .AnyAsync(b => b.MaNganHang == request.MaNganHang);
            if (!bankExists)
            {
                return Ok(new ApiResponse
                {
                    Status = "FAIL",
                    Message = "Mã ngân hàng không tồn tại trên hệ thống."
                });
            }

            // 3. Kiểm tra trùng lặp Số tài khoản (STK)
            var accountExists = await _context.Accounts
                .AnyAsync(a => a.SoTaiKhoan == request.SoTaiKhoan);
            if (accountExists)
            {
                return Ok(new ApiResponse
                {
                    Status = "FAIL",
                    Message = "Số tài khoản này đã được đăng ký."
                });
            }

            // 4. Kiểm tra trùng lặp Số điện thoại (SDT)
            var phoneExists = await _context.Accounts
                .AnyAsync(a => a.SoDienThoai == request.SoDienThoai);
            if (phoneExists)
            {
                return Ok(new ApiResponse
                {
                    Status = "FAIL",
                    Message = "Số điện thoại này đã được sử dụng cho một tài khoản khác."
                });
            }

            // 5. Kiểm tra trùng lặp Căn cước công dân (CCCD)
            var cccdExists = await _context.Accounts
                .AnyAsync(a => a.Cccd == request.CCCD);
            if (cccdExists)
            {
                return Ok(new ApiResponse
                {
                    Status = "FAIL",
                    Message = "Số CCCD này đã được đăng ký trên hệ thống."
                });
            }

            try
            {
                // 6. Nếu tất cả đều hợp lệ -> Tiến hành ánh xạ dữ liệu và lưu vào CSDL
                var newAccount = new Account
                {
                    SoTaiKhoan = request.SoTaiKhoan,
                    MaNganHang = request.MaNganHang,
                    HoTenChuThe = request.HoTenChuThe,
                    SoDienThoai = request.SoDienThoai,
                    Cccd = request.CCCD,
                    NgayHetHan = request.NgayHetHan.HasValue ? DateOnly.FromDateTime(request.NgayHetHan.Value) : null,
                    SoDuKhaDung = request.SoDuBanDau,
                    ThoiGianTao = DateTime.Now
                };

                _context.Accounts.Add(newAccount);
                await _context.SaveChangesAsync();

                // 7. Trả về thông báo thành công
                return Ok(new ApiResponse
                {
                    Status = "SUCCESS",
                    SoTaiKhoan = newAccount.SoTaiKhoan,
                    Message = "Tạo tài khoản thành công"
                });
            }
            catch (Exception ex)
            {
                // Bắt lỗi hệ thống (nếu có lỗi trong quá trình lưu)
                return Ok(new ApiResponse
                {
                    Status = "FAIL",
                    Message = $"Đã xảy ra lỗi hệ thống khi tạo tài khoản: {ex.Message}"
                });
            }
        }
    }
}
