using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NiiePay.Entities;
using NiiePay.Models;

namespace NiiePay.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChuyenKhoanNoiBoController : ControllerBase
{
    private readonly NiiePayContext _context;

    public ChuyenKhoanNoiBoController(NiiePayContext context)
    {
        _context = context;
    }

    // POST api/ChuyenKhoanNoiBo/internal
    // Internal transfer within the same bank. Both sender and receiver must exist and belong to the same MaNganHang.
    [HttpPost("internal")]
    public async Task<IActionResult> Internal([FromBody] ChuyenKhoanLienNganHangRequest req)
    {
        if (req == null)
            return BadRequest(new ApiResponse { Status = "FAIL", Message = "Yêu cầu không hợp lệ." });

        if (string.IsNullOrWhiteSpace(req.TaiKhoanGui) || string.IsNullOrWhiteSpace(req.TaiKhoanNhan) || req.SoTien <= 0)
            return BadRequest(new ApiResponse { Status = "FAIL", Message = "Thiếu thông tin hoặc số tiền không hợp lệ." });

        var sender = await _context.Accounts.FirstOrDefaultAsync(a => a.SoTaiKhoan == req.TaiKhoanGui);
        if (sender == null)
            return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Tài khoản người gửi không tồn tại.", Data = (object?)null });

        var receiver = await _context.Accounts.FirstOrDefaultAsync(a => a.SoTaiKhoan == req.TaiKhoanNhan || a.SoDienThoai == req.TaiKhoanNhan);
        if (receiver == null)
            return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Tài khoản người nhận không tồn tại trong cùng ngân hàng.", Data = (object?)null });

        // Ensure both accounts belong to the same bank
        if (!string.Equals(sender.MaNganHang, receiver.MaNganHang, StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Tài khoản người gửi và người nhận không cùng ngân hàng. Vui lòng sử dụng API chuyển liên ngân hàng.", Data = (object?)null });
        }

        const decimal MinimumBalance = 50000m;
        if (sender.SoDuKhaDung - req.SoTien < MinimumBalance)
        {
            // record failed transaction
            var txnId = "TXN" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
            var failed = new GiaoDich
            {
                MaGiaoDich = txnId,
                TaiKhoanGui = req.TaiKhoanGui,
                TaiKhoanNhan = req.TaiKhoanNhan,
                MaNganHang = sender.MaNganHang,
                SoTien = req.SoTien,
                NoiDung = req.NoiDung,
                TrangThai = "FAILED",
                SoDuSauGiaoDich = null,
                ThoiGian = DateTime.Now
            };

            _context.GiaoDiches.Add(failed);
            await _context.SaveChangesAsync();

            var data = new
            {
                failed.MaGiaoDich,
                failed.TaiKhoanGui,
                failed.TaiKhoanNhan,
                failed.MaNganHang,
                failed.SoTien,
                ThoiGian = failed.ThoiGian,
                SoDuSauGiaoDich = failed.SoDuSauGiaoDich,
                NoiDung = failed.NoiDung,
                TrangThai = failed.TrangThai
            };

            return Ok(new ApiResponseGeneric { Status = "FAIL", Message = $"Không đủ tiền khả dụng. Tài khoản phải giữ tối thiểu {MinimumBalance:N0} đ sau giao dịch.", Data = data });
        }

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // debit sender
            sender.SoDuKhaDung -= req.SoTien;
            _context.Accounts.Update(sender);

            // credit receiver
            receiver.SoDuKhaDung += req.SoTien;
            _context.Accounts.Update(receiver);

            var txnId = "TXN" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
            var giaoDich = new GiaoDich
            {
                MaGiaoDich = txnId,
                TaiKhoanGui = req.TaiKhoanGui,
                TaiKhoanNhan = req.TaiKhoanNhan,
                MaNganHang = sender.MaNganHang,
                SoTien = req.SoTien,
                NoiDung = req.NoiDung,
                TrangThai = "SUCCESS",
                SoDuSauGiaoDich = sender.SoDuKhaDung,
                ThoiGian = DateTime.Now
            };

            _context.GiaoDiches.Add(giaoDich);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            var data = new
            {
                giaoDich.MaGiaoDich,
                giaoDich.TaiKhoanGui,
                giaoDich.TaiKhoanNhan,
                giaoDich.MaNganHang,
                giaoDich.SoTien,
                ThoiGian = giaoDich.ThoiGian,
                SoDuSauGiaoDich = giaoDich.SoDuSauGiaoDich,
                NoiDung = giaoDich.NoiDung,
                TrangThai = giaoDich.TrangThai
            };

            return Ok(new ApiResponseGeneric { Status = "SUCCESS", Message = "Chuyển khoản nội bộ thành công.", Data = data });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();

            var txnId = "TXN" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
            var failed = new GiaoDich
            {
                MaGiaoDich = txnId,
                TaiKhoanGui = req.TaiKhoanGui,
                TaiKhoanNhan = req.TaiKhoanNhan,
                MaNganHang = sender.MaNganHang,
                SoTien = req.SoTien,
                NoiDung = req.NoiDung,
                TrangThai = "FAILED",
                SoDuSauGiaoDich = null,
                ThoiGian = DateTime.Now
            };

            try
            {
                _context.GiaoDiches.Add(failed);
                await _context.SaveChangesAsync();
            }
            catch { }

            var data = new
            {
                failed.MaGiaoDich,
                failed.TaiKhoanGui,
                failed.TaiKhoanNhan,
                failed.MaNganHang,
                failed.SoTien,
                ThoiGian = failed.ThoiGian,
                SoDuSauGiaoDich = failed.SoDuSauGiaoDich,
                NoiDung = failed.NoiDung,
                TrangThai = failed.TrangThai
            };

            return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Lỗi khi thực hiện giao dịch: " + ex.Message, Data = data });
        }
    }
}
