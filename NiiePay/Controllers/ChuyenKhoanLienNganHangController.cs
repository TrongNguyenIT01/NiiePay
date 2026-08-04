using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NiiePay.Entities;
using NiiePay.Models;

namespace NiiePay.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChuyenKhoanLienNganHangController : ControllerBase
{
    private readonly NiiePayContext _context;

    public ChuyenKhoanLienNganHangController(NiiePayContext context)
    {
        _context = context;
    }

    // POST api/ChuyenKhoanLienNganHang/external
    // Handles interbank transfer requests. If the destination account exists in our DB
    // the receiver's balance will be credited. Otherwise the transfer is recorded and
    // considered successful from the sender side.
    [HttpPost("external")]
    public async Task<IActionResult> External([FromBody] ChuyenKhoanLienNganHangRequest req)
    {
        if (req == null)
            return BadRequest(new ApiResponse { Status = "FAIL", Message = "Yêu cầu không hợp lệ." });

        if (string.IsNullOrWhiteSpace(req.TaiKhoanGui) || string.IsNullOrWhiteSpace(req.TaiKhoanNhan) || req.SoTien <= 0)
            return BadRequest(new ApiResponse { Status = "FAIL", Message = "Thiếu thông tin hoặc số tiền không hợp lệ." });

        var sender = await _context.Accounts.FirstOrDefaultAsync(a => a.SoTaiKhoan == req.TaiKhoanGui);
        if (sender == null)
        {
            // return a structured response with attempted transaction info (no DB record)
            var attempted = new
            {
                MaGiaoDich = (string?)null,
                TaiKhoanGui = req.TaiKhoanGui,
                TaiKhoanNhan = req.TaiKhoanNhan,
                MaNganHang = req.MaNganHang,
                SoTien = req.SoTien,
                ThoiGian = (DateTime?)null,
                SoDuSauGiaoDich = (decimal?)null,
                NoiDung = req.NoiDung,
                TrangThai = "FAILED"
            };

            return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Tài khoản người gửi không tồn tại.", Data = attempted });
        }

        // Generate transaction id
        var txnId = "TXN" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);

        // If transferring would leave sender below required minimum (50,000) -> record FAILED transaction
        const decimal MinimumBalance = 50000m;
        if (sender.SoDuKhaDung - req.SoTien < MinimumBalance)
        {
            var failed = new GiaoDich
            {
                MaGiaoDich = txnId,
                TaiKhoanGui = req.TaiKhoanGui,
                TaiKhoanNhan = req.TaiKhoanNhan,
                MaNganHang = req.MaNganHang,
                SoTien = req.SoTien,
                NoiDung = req.NoiDung,
                TrangThai = "FAILED",
                SoDuSauGiaoDich = null,
                ThoiGian = DateTime.Now
            };

            _context.GiaoDiches.Add(failed);
            await _context.SaveChangesAsync();

            var insufficientFundsData = new
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

            return Ok(new ApiResponseGeneric { Status = "FAIL", Message = $"Không đủ tiền khả dụng. Tài khoản phải giữ tối thiểu {MinimumBalance:N0} đ sau giao dịch.", Data = insufficientFundsData });
        }

        // Proceed with transfer inside a transaction
        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // try to find receiver first (by account number or phone)
            var receiver = await _context.Accounts.FirstOrDefaultAsync(a => a.SoTaiKhoan == req.TaiKhoanNhan || a.SoDienThoai == req.TaiKhoanNhan);

            // Business rule: interbank transfers must be between different banks.
            // If receiver exists in our DB and belongs to the same bank as sender -> reject and ask to use internal transfer.
            if (receiver != null && string.Equals(receiver.MaNganHang, sender.MaNganHang, StringComparison.OrdinalIgnoreCase))
            {
                var failedSameBank = new GiaoDich
                {
                    MaGiaoDich = txnId,
                    TaiKhoanGui = req.TaiKhoanGui,
                    TaiKhoanNhan = req.TaiKhoanNhan,
                    MaNganHang = receiver.MaNganHang,
                    SoTien = req.SoTien,
                    NoiDung = req.NoiDung,
                    TrangThai = "FAILED",
                    SoDuSauGiaoDich = null,
                    ThoiGian = DateTime.Now
                };

                _context.GiaoDiches.Add(failedSameBank);
                await _context.SaveChangesAsync();

                var failedSameBankData = new
                {
                    failedSameBank.MaGiaoDich,
                    failedSameBank.TaiKhoanGui,
                    failedSameBank.TaiKhoanNhan,
                    failedSameBank.MaNganHang,
                    failedSameBank.SoTien,
                    ThoiGian = failedSameBank.ThoiGian,
                    SoDuSauGiaoDich = failedSameBank.SoDuSauGiaoDich,
                    NoiDung = failedSameBank.NoiDung,
                    TrangThai = failedSameBank.TrangThai
                };

                await tx.RollbackAsync();
                return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Tài khoản nhận cùng ngân hàng. Vui lòng dùng chuyển nội bộ (internal).", Data = failedSameBankData });
            }

            // If receiver does not exist in our DB, client must provide MaNganHang and it must be different from sender's bank
            if (receiver == null)
            {
                if (string.IsNullOrWhiteSpace(req.MaNganHang))
                {
                    await tx.RollbackAsync();
                    return BadRequest(new ApiResponse { Status = "FAIL", Message = "Tài khoản nhận không có trong hệ thống, vui lòng cung cấp MaNganHang của ngân hàng nhận." });
                }

                if (string.Equals(req.MaNganHang, sender.MaNganHang, StringComparison.OrdinalIgnoreCase))
                {
                    var failed = new GiaoDich
                    {
                        MaGiaoDich = txnId,
                        TaiKhoanGui = req.TaiKhoanGui,
                        TaiKhoanNhan = req.TaiKhoanNhan,
                        MaNganHang = req.MaNganHang,
                        SoTien = req.SoTien,
                        NoiDung = req.NoiDung,
                        TrangThai = "FAILED",
                        SoDuSauGiaoDich = null,
                        ThoiGian = DateTime.Now
                    };

                    _context.GiaoDiches.Add(failed);
                    await _context.SaveChangesAsync();

                    await tx.RollbackAsync();
                    return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Mã ngân hàng phải khác ngân hàng của người gửi. Nếu cùng ngân hàng, sử dụng chuyển khoản nội bộ.", Data = new { failed.MaGiaoDich, failed.TaiKhoanGui, failed.TaiKhoanNhan, failed.MaNganHang, failed.SoTien, ThoiGian = failed.ThoiGian, SoDuSauGiaoDich = failed.SoDuSauGiaoDich, NoiDung = failed.NoiDung, TrangThai = failed.TrangThai } });
                }
            }

            // debit sender
            sender.SoDuKhaDung -= req.SoTien;
            _context.Accounts.Update(sender);

            // credit receiver if exists
            if (receiver != null)
            {
                receiver.SoDuKhaDung += req.SoTien;
                _context.Accounts.Update(receiver);
            }

            var giaoDich = new GiaoDich
            {
                MaGiaoDich = txnId,
                TaiKhoanGui = req.TaiKhoanGui,
                TaiKhoanNhan = req.TaiKhoanNhan,
                MaNganHang = req.MaNganHang,
                SoTien = req.SoTien,
                NoiDung = req.NoiDung,
                TrangThai = "SUCCESS",
                SoDuSauGiaoDich = sender.SoDuKhaDung,
                ThoiGian = DateTime.Now
            };

            _context.GiaoDiches.Add(giaoDich);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            var successData = new
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

            return Ok(new ApiResponseGeneric { Status = "SUCCESS", Message = "Chuyển khoản thành công.", Data = successData });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();

            // record a failed transaction for traceability
            var failed = new GiaoDich
            {
                MaGiaoDich = txnId,
                TaiKhoanGui = req.TaiKhoanGui,
                TaiKhoanNhan = req.TaiKhoanNhan,
                MaNganHang = req.MaNganHang,
                SoTien = req.SoTien,
                NoiDung = req.NoiDung,
                TrangThai = "FAILED",
                SoDuSauGiaoDich = null
            };
            try
            {
                _context.GiaoDiches.Add(failed);
                await _context.SaveChangesAsync();
            }
            catch { /* ignore */ }

            var exceptionFailedData = new
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

            return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Lỗi khi thực hiện giao dịch: " + ex.Message, Data = exceptionFailedData });
        }
    }
}
