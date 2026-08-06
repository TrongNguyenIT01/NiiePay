
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NiiePay.Entities;
using NiiePay.Models;

namespace NiiePay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SavingController : ControllerBase
    {
        private readonly NiiePayContext _context;
        public SavingController(NiiePayContext context)
        {
            _context = context;
        }
        private readonly Dictionary<int, double> _interestRates = new()
        {
            { 1, 3.5 }, { 2, 3.7 }, { 3, 3.8 }, { 6, 4.8 },
            { 9, 4.9 }, { 12, 5.2 }, { 18, 5.5 }, { 24, 5.8 }, { 36, 5.8 }
        };

        [HttpPost("open")]
        public IActionResult OpenSavingAccount([FromBody] Saving request)
        {
            try
            {
                // 1. Validate dữ liệu đầu vào
                if (!_interestRates.ContainsKey(request.KyHan))
                {
                    return BadRequest(new SavingResponse
                    {
                        Status = "FAIL",
                        Message = "Kỳ hạn không hợp lệ. Chỉ hỗ trợ 1, 2, 3, 6, 9, 12, 18, 24, 36 tháng."
                    });
                }

                if (request.SoTienGui < 50000) 
                {
                    return BadRequest(new SavingResponse
                    {
                        Status = "FAIL",
                        Message = "Số tiền gửi tối thiểu là 50,000 VND."
                    });
                }

                
                double interestRate = _interestRates[request.KyHan];
                DateTime startDate = DateTime.Today; 
                DateTime maturityDate = startDate.AddMonths(request.KyHan); 

                // Sinh mã sổ tiết kiệm (SAV + chuỗi thời gian để đảm bảo unique)
                string savingId = "SAV" + DateTime.Now.ToString("yyyyMMddHHmmss");

                // 3. Thực thi lưu vào CSDL qua ADO.NET
                using (SqlConnection connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {
                    connection.Open();
                    string query = @"INSERT INTO SoTietKiem 
                                     ([MaSoTietKiem], [SoTaiKhoan], [SoTienGui], [KyHan], [LaiSuat], [NgayMoSo], [NgayHetHan], [TuDongGiaHan]) 
                                     VALUES (@MaSoTietKiem, @SoTaiKhoan, @SoTienGui, @KyHan, @LaiSuat, @NgayMoSo, @NgayHetHan, @TuDongGiaHan)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaSoTietKiem", savingId);
                        command.Parameters.AddWithValue("@SoTaiKhoan", request.SoTaiKhoan);
                        command.Parameters.AddWithValue("@SoTienGui", request.SoTienGui);
                        command.Parameters.AddWithValue("@KyHan", request.KyHan);
                        command.Parameters.AddWithValue("@LaiSuat", interestRate);
                        command.Parameters.AddWithValue("@NgayMoSo", startDate);
                        command.Parameters.AddWithValue("@NgayHetHan", maturityDate);
                        command.Parameters.AddWithValue("@TuDongGiaHan", request.TuDongGiaHan);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            throw new Exception("Không thể lưu thông tin vào Database.");
                        }
                    }
                }

                // 4. Trả về kết quả thành công
                return Ok(new SavingResponse
                {
                    Status = "SUCCESS",
                    MaSoTietKiem = savingId,
                    KyHan = request.KyHan,
                    LaiSuat = interestRate,
                    NgayBatDau = startDate.ToString("yyyy-MM-dd"),     // Ép đúng định dạng YYYY-MM-DD
                    NgayHetHan = maturityDate.ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                // Bắt các lỗi không lường trước (Lỗi DB, rớt mạng, v.v...)
                return StatusCode(500, new SavingResponse
                {
                    Status = "FAIL",
                    Message = "Hệ thống gặp lỗi: " + ex.Message
                });
            }

        }
    }
}
