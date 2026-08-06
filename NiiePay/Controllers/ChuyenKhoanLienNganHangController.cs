//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using NiiePay.Entities;
//using NiiePay.Models;

//namespace NiiePay.Controllers;

//[Route("api/[controller]")]
//[ApiController]
//public class ChuyenKhoanLienNganHangController : ControllerBase
//{
//    private readonly NiiePayContext _context;

//    public ChuyenKhoanLienNganHangController(NiiePayContext context)
//    {
//        _context = context;
//    }

//    // POST api/ChuyenKhoanLienNganHang/external
//    // Handles interbank transfer requests. If the destination account exists in our DB
//    // the receiver's balance will be credited. Otherwise the transfer is recorded and
//    // considered successful from the sender side.
//    [HttpPost("external")]
//    public async Task<IActionResult> External([FromBody] ChuyenKhoanLienNganHangRequest req)
//    {
//        if (req == null)
//            return BadRequest(new ApiResponse { Status = "FAIL", Message = "Yêu cầu không hợp lệ." });

//        if (string.IsNullOrWhiteSpace(req.TaiKhoanGui) || string.IsNullOrWhiteSpace(req.TaiKhoanNhan) || req.SoTien <= 0)
//            return BadRequest(new ApiResponse { Status = "FAIL", Message = "Thiếu thông tin hoặc số tiền không hợp lệ." });

//        var sender = await _context.Accounts.FirstOrDefaultAsync(a => a.SoTaiKhoan == req.TaiKhoanGui);
//        if (sender == null)
//        {
//            // return a structured response with attempted transaction info (no DB record)
//            var attempted = new
//            {
//                MaGiaoDich = (string?)null,
//                TaiKhoanGui = req.TaiKhoanGui,
//                TaiKhoanNhan = req.TaiKhoanNhan,
//                MaNganHang = req.MaNganHang,
//                SoTien = req.SoTien,
//                ThoiGian = (DateTime?)null,
//                SoDuSauGiaoDich = (decimal?)null,
//                NoiDung = req.NoiDung,
//                TrangThai = "FAILED"
//            };

//            return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Tài khoản người gửi không tồn tại.", Data = attempted });
//        }

//        // Generate transaction id
//        var txnId = "TXN" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);

//        // If transferring would leave sender below required minimum (50,000) -> record FAILED transaction
//        const decimal MinimumBalance = 50000m;
//        if (sender.SoDuKhaDung - req.SoTien < MinimumBalance)
//        {
//            var failed = new GiaoDich
//            {
//                MaGiaoDich = txnId,
//                TaiKhoanGui = req.TaiKhoanGui,
//                TaiKhoanNhan = req.TaiKhoanNhan,
//                MaNganHang = req.MaNganHang,
//                SoTien = req.SoTien,
//                NoiDung = req.NoiDung,
//                TrangThai = "FAILED",
//                SoDuSauGiaoDich = null,
//                ThoiGian = DateTime.Now
//            };

//            _context.GiaoDiches.Add(failed);
//            await _context.SaveChangesAsync();

//            var insufficientFundsData = new
//            {
//                failed.MaGiaoDich,
//                failed.TaiKhoanGui,
//                failed.TaiKhoanNhan,
//                failed.MaNganHang,
//                failed.SoTien,
//                ThoiGian = failed.ThoiGian,
//                SoDuSauGiaoDich = failed.SoDuSauGiaoDich,
//                NoiDung = failed.NoiDung,
//                TrangThai = failed.TrangThai
//            };

//            return Ok(new ApiResponseGeneric { Status = "FAIL", Message = $"Không đủ tiền khả dụng. Tài khoản phải giữ tối thiểu {MinimumBalance:N0} đ sau giao dịch.", Data = insufficientFundsData });
//        }

//        // Proceed with transfer inside a transaction
//        using var tx = await _context.Database.BeginTransactionAsync();
//        try
//        {
//            // try to find receiver first (by account number or phone)
//            var receiver = await _context.Accounts.FirstOrDefaultAsync(a => a.SoTaiKhoan == req.TaiKhoanNhan || a.SoDienThoai == req.TaiKhoanNhan);

//            // Business rule: interbank transfers must be between different banks.
//            // If receiver exists in our DB and belongs to the same bank as sender -> reject and ask to use internal transfer.
//            if (receiver != null && string.Equals(receiver.MaNganHang, sender.MaNganHang, StringComparison.OrdinalIgnoreCase))
//            {
//                var failedSameBank = new GiaoDich
//                {
//                    MaGiaoDich = txnId,
//                    TaiKhoanGui = req.TaiKhoanGui,
//                    TaiKhoanNhan = req.TaiKhoanNhan,
//                    MaNganHang = receiver.MaNganHang,
//                    SoTien = req.SoTien,
//                    NoiDung = req.NoiDung,
//                    TrangThai = "FAILED",
//                    SoDuSauGiaoDich = null,
//                    ThoiGian = DateTime.Now
//                };

//                _context.GiaoDiches.Add(failedSameBank);
//                await _context.SaveChangesAsync();

//                var failedSameBankData = new
//                {
//                    failedSameBank.MaGiaoDich,
//                    failedSameBank.TaiKhoanGui,
//                    failedSameBank.TaiKhoanNhan,
//                    failedSameBank.MaNganHang,
//                    failedSameBank.SoTien,
//                    ThoiGian = failedSameBank.ThoiGian,
//                    SoDuSauGiaoDich = failedSameBank.SoDuSauGiaoDich,
//                    NoiDung = failedSameBank.NoiDung,
//                    TrangThai = failedSameBank.TrangThai
//                };

//                await tx.RollbackAsync();
//                return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Tài khoản nhận cùng ngân hàng. Vui lòng dùng chuyển nội bộ (internal).", Data = failedSameBankData });
//            }

//            // If receiver does not exist in our DB, client must provide MaNganHang and it must be different from sender's bank
//            if (receiver == null)
//            {
//                if (string.IsNullOrWhiteSpace(req.MaNganHang))
//                {
//                    await tx.RollbackAsync();
//                    return BadRequest(new ApiResponse { Status = "FAIL", Message = "Tài khoản nhận không có trong hệ thống, vui lòng cung cấp MaNganHang của ngân hàng nhận." });
//                }

//                if (string.Equals(req.MaNganHang, sender.MaNganHang, StringComparison.OrdinalIgnoreCase))
//                {
//                    var failed = new GiaoDich
//                    {
//                        MaGiaoDich = txnId,
//                        TaiKhoanGui = req.TaiKhoanGui,
//                        TaiKhoanNhan = req.TaiKhoanNhan,
//                        MaNganHang = req.MaNganHang,
//                        SoTien = req.SoTien,
//                        NoiDung = req.NoiDung,
//                        TrangThai = "FAILED",
//                        SoDuSauGiaoDich = null,
//                        ThoiGian = DateTime.Now
//                    };

//                    _context.GiaoDiches.Add(failed);
//                    await _context.SaveChangesAsync();

//                    await tx.RollbackAsync();
//                    return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Mã ngân hàng phải khác ngân hàng của người gửi. Nếu cùng ngân hàng, sử dụng chuyển khoản nội bộ.", Data = new { failed.MaGiaoDich, failed.TaiKhoanGui, failed.TaiKhoanNhan, failed.MaNganHang, failed.SoTien, ThoiGian = failed.ThoiGian, SoDuSauGiaoDich = failed.SoDuSauGiaoDich, NoiDung = failed.NoiDung, TrangThai = failed.TrangThai } });
//                }
//            }

//            // debit sender
//            sender.SoDuKhaDung -= req.SoTien;
//            _context.Accounts.Update(sender);

//            // credit receiver if exists
//            if (receiver != null)
//            {
//                receiver.SoDuKhaDung += req.SoTien;
//                _context.Accounts.Update(receiver);
//            }

//            var giaoDich = new GiaoDich
//            {
//                MaGiaoDich = txnId,
//                TaiKhoanGui = req.TaiKhoanGui,
//                TaiKhoanNhan = req.TaiKhoanNhan,
//                MaNganHang = req.MaNganHang,
//                SoTien = req.SoTien,
//                NoiDung = req.NoiDung,
//                TrangThai = "SUCCESS",
//                SoDuSauGiaoDich = sender.SoDuKhaDung,
//                ThoiGian = DateTime.Now
//            };

//            _context.GiaoDiches.Add(giaoDich);
//            await _context.SaveChangesAsync();
//            await tx.CommitAsync();

//            var successData = new
//            {
//                giaoDich.MaGiaoDich,
//                giaoDich.TaiKhoanGui,
//                giaoDich.TaiKhoanNhan,
//                giaoDich.MaNganHang,
//                giaoDich.SoTien,
//                ThoiGian = giaoDich.ThoiGian,
//                SoDuSauGiaoDich = giaoDich.SoDuSauGiaoDich,
//                NoiDung = giaoDich.NoiDung,
//                TrangThai = giaoDich.TrangThai
//            };

//            return Ok(new ApiResponseGeneric { Status = "SUCCESS", Message = "Chuyển khoản thành công.", Data = successData });
//        }
//        catch (Exception ex)
//        {
//            await tx.RollbackAsync();

//            // record a failed transaction for traceability
//            var failed = new GiaoDich
//            {
//                MaGiaoDich = txnId,
//                TaiKhoanGui = req.TaiKhoanGui,
//                TaiKhoanNhan = req.TaiKhoanNhan,
//                MaNganHang = req.MaNganHang,
//                SoTien = req.SoTien,
//                NoiDung = req.NoiDung,
//                TrangThai = "FAILED",
//                SoDuSauGiaoDich = null
//            };
//            try
//            {
//                _context.GiaoDiches.Add(failed);
//                await _context.SaveChangesAsync();
//            }
//            catch { /* ignore */ }

//            var exceptionFailedData = new
//            {
//                failed.MaGiaoDich,
//                failed.TaiKhoanGui,
//                failed.TaiKhoanNhan,
//                failed.MaNganHang,
//                failed.SoTien,
//                ThoiGian = failed.ThoiGian,
//                SoDuSauGiaoDich = failed.SoDuSauGiaoDich,
//                NoiDung = failed.NoiDung,
//                TrangThai = failed.TrangThai
//            };

//            return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Lỗi khi thực hiện giao dịch: " + ex.Message, Data = exceptionFailedData });
//        }
//    }
//}
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

    // MỚI: hàm sinh mã giao dịch, tách ra để có thể gọi nhiều lần (1 cho sender, 1 cho receiver)
    // trong cùng 1 lần chuyển mà không bị trùng mã
    private static string GenTxnId()
        => "TXN" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);

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
        var txnId = GenTxnId(); // SỬA: gọi qua hàm GenTxnId() thay vì viết trực tiếp, giữ nguyên định dạng mã

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
                ThoiGian = DateTime.Now,
                TaiKhoanSoHuu = req.TaiKhoanGui,   // MỚI: giao dịch thất bại vẫn ghi nhận thuộc về phía người gửi
                LoaiGiaoDich = "M_out"             // MỚI
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

            // If receiver exists in our DB, validate provided MaNganHang if present.
            // If client provided a MaNganHang that doesn't match receiver's MaNganHang -> fail.
            if (receiver != null)
            {
                if (!string.IsNullOrWhiteSpace(req.MaNganHang) && !string.Equals(req.MaNganHang, receiver.MaNganHang, StringComparison.OrdinalIgnoreCase))
                {
                    var failedBankMismatch = new GiaoDich
                    {
                        MaGiaoDich = txnId,
                        TaiKhoanGui = req.TaiKhoanGui,
                        TaiKhoanNhan = req.TaiKhoanNhan,
                        MaNganHang = req.MaNganHang,
                        SoTien = req.SoTien,
                        NoiDung = req.NoiDung,
                        TrangThai = "FAILED",
                        SoDuSauGiaoDich = null,
                        ThoiGian = DateTime.Now,
                        TaiKhoanSoHuu = req.TaiKhoanGui,
                        LoaiGiaoDich = "M_out"
                    };

                    _context.GiaoDiches.Add(failedBankMismatch);
                    await _context.SaveChangesAsync();

                    await tx.RollbackAsync();
                    return Ok(new ApiResponseGeneric { Status = "FAIL", Message = "Mã ngân hàng không khớp với ngân hàng của người nhận.", Data = new { failedBankMismatch.MaGiaoDich, failedBankMismatch.TaiKhoanGui, failedBankMismatch.TaiKhoanNhan, failedBankMismatch.MaNganHang, failedBankMismatch.SoTien, ThoiGian = failedBankMismatch.ThoiGian, SoDuSauGiaoDich = failedBankMismatch.SoDuSauGiaoDich, NoiDung = failedBankMismatch.NoiDung, TrangThai = failedBankMismatch.TrangThai } });
                }

                // If client did not provide MaNganHang, populate it from receiver's record.
                if (string.IsNullOrWhiteSpace(req.MaNganHang))
                {
                    req.MaNganHang = receiver.MaNganHang;
                }

                // Business rule: interbank transfers must be between different banks.
                // If receiver belongs to the same bank as sender -> reject and ask to use internal transfer.
                if (string.Equals(receiver.MaNganHang, sender.MaNganHang, StringComparison.OrdinalIgnoreCase))
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
                        ThoiGian = DateTime.Now,
                        TaiKhoanSoHuu = req.TaiKhoanGui,   // MỚI
                        LoaiGiaoDich = "M_out"             // MỚI
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
                        ThoiGian = DateTime.Now,
                        TaiKhoanSoHuu = req.TaiKhoanGui,   // MỚI
                        LoaiGiaoDich = "M_out"             // MỚI
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

            var thoiGianGiaoDich = DateTime.Now; // MỚI: mốc thời gian dùng chung cho cả 2 dòng (sender + receiver)

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
                ThoiGian = thoiGianGiaoDich,
                TaiKhoanSoHuu = req.TaiKhoanGui,   // MỚI: dòng này thuộc về sender
                LoaiGiaoDich = "M_out"             // MỚI: tiền ra
            };

            _context.GiaoDiches.Add(giaoDich);

            // MỚI: tạo thêm 1 dòng riêng cho receiver, dùng MÃ GIAO DỊCH RIÊNG (khác txnId của sender)
            // để không phải đổi Primary Key hiện tại của bảng GiaoDich
            GiaoDich? giaoDichNhan = null;
            if (receiver != null)
            {
                giaoDichNhan = new GiaoDich
                {
                    MaGiaoDich = GenTxnId(),                // mã riêng, độc lập với txnId ở trên
                    TaiKhoanGui = req.TaiKhoanGui,
                    TaiKhoanNhan = req.TaiKhoanNhan,
                    MaNganHang = sender.MaNganHang,          // ngân hàng của bên gửi, để bên nhận biết tiền đến từ đâu
                    SoTien = req.SoTien,
                    NoiDung = req.NoiDung,
                    TrangThai = "SUCCESS",
                    SoDuSauGiaoDich = receiver.SoDuKhaDung,  // số dư của RECEIVER sau khi nhận
                    ThoiGian = thoiGianGiaoDich,
                    TaiKhoanSoHuu = receiver.SoTaiKhoan,     // dòng này thuộc về receiver
                    LoaiGiaoDich = "M_in"                    // tiền vào
                };

                _context.GiaoDiches.Add(giaoDichNhan);
            }

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
                SoDuSauGiaoDich = null,
                TaiKhoanSoHuu = req.TaiKhoanGui,   // MỚI
                LoaiGiaoDich = "M_out"             // MỚI
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