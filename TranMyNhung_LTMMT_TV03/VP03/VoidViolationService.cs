using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TranMyNhung_LTMMT_TV03.VP03
{
    public class ViolationLookupItem
    {
        public int ViolationId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ViolationType { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public int? ReceiptId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; }
    }

    public class VoidViolationRequest
    {
        public int ViolationId { get; set; }
        public int ManagerAccountId { get; set; }
        public string Reason { get; set; }
    }

    public class VoidViolationResult
    {
        public int ViolationId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public decimal Amount { get; set; }
        public string PreviousStatus { get; set; }
        public bool HasExistingReceipt { get; set; }
        public int? ReceiptId { get; set; }
        public string ManagerName { get; set; }
        public DateTime VoidedAt { get; set; }
        public string Reason { get; set; }
        public bool ReaderUnlocked { get; set; }
    }

    public class VoidViolationService
    {
        private const string ConnectionString =
            "Server=127.0.0.1;Port=3306;Database=thuvien_tv03;User ID=root;Password=110704;CharSet=utf8mb4;";

        private static MySqlConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        private async Task<string> VerifyLibraryManagerAsync(MySqlConnection conn, MySqlTransaction tx, int accountId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    SELECT HoTen, VaiTro, TrangThai
                    FROM TaiKhoan
                    WHERE IdTaiKhoan = @id
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", accountId);

                using (DbDataReader r = await cmd.ExecuteReaderAsync())
                {
                    if (!await r.ReadAsync())
                        throw new UnauthorizedAccessException("Tài khoản thực hiện không tồn tại trong hệ thống.");

                    string name = r.GetString(0);
                    string role = r.GetString(1);
                    string status = r.GetString(2);

                    if (status != "HOAT_DONG")
                        throw new UnauthorizedAccessException("Tài khoản đang bị khóa, không thể thực hiện thao tác.");

                    if (role != "QUAN_LY_THU_VIEN")
                        throw new UnauthorizedAccessException($"Từ chối truy cập: Chỉ QUẢN LÝ THƯ VIỆN mới có quyền hủy hiệu lực phiếu phạt (vai trò hiện tại: {role}).");

                    return name;
                }
            }
        }

        public async Task<List<ViolationLookupItem>> SearchViolationsAsync(string keyword = "", bool includeVoided = false)
        {
            var list = new List<ViolationLookupItem>();
            string kw = (keyword ?? "").Trim();

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT vp.IdViPham, vp.IdDocGia, dg.MaDinhDanhTruong, dg.HoTen,
                               vp.LoaiViPham, vp.SoTien, vp.TrangThai, vp.IdPhieuThu,
                               vp.NgayLap, COALESCE(vp.MoTa, '')
                        FROM ViPham vp
                        JOIN DocGia dg ON dg.IdDocGia = vp.IdDocGia
                        WHERE (@incVoid = 1 OR vp.TrangThai <> 'DA_HUY')
                          AND (@kw = '' OR dg.MaDinhDanhTruong LIKE @likeKw OR dg.HoTen LIKE @likeKw OR CAST(vp.IdViPham AS CHAR) = @kw)
                        ORDER BY vp.NgayLap DESC
                        LIMIT 200;";
                    cmd.Parameters.AddWithValue("@incVoid", includeVoided ? 1 : 0);
                    cmd.Parameters.AddWithValue("@kw", kw);
                    cmd.Parameters.AddWithValue("@likeKw", "%" + kw + "%");

                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            list.Add(new ViolationLookupItem
                            {
                                ViolationId = r.GetInt32(0),
                                ReaderId = r.GetInt32(1),
                                ReaderCode = r.GetString(2),
                                ReaderName = r.GetString(3),
                                ViolationType = r.GetString(4),
                                Amount = r.GetDecimal(5),
                                Status = r.GetString(6),
                                ReceiptId = r.IsDBNull(7) ? (int?)null : r.GetInt32(7),
                                CreatedAt = r.GetDateTime(8),
                                Description = r.GetString(9)
                            });
                        }
                    }
                }
            }

            return list;
        }

        public async Task<VoidViolationResult> VoidViolationAsync(VoidViolationRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.Reason) || req.Reason.Trim().Length < 5)
                throw new ArgumentException("Bắt buộc nhập lý do hủy hiệu lực phiếu phạt (tối thiểu 5 ký tự) để lưu nhật ký.");

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();
                using (var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        // 1. Kiểm tra nghiêm ngặt quyền QUAN_LY_THU_VIEN
                        string managerName = await VerifyLibraryManagerAsync(conn, tx, req.ManagerAccountId);

                        int readerId;
                        string readerCode, readerName, vType, prevStatus, oldDesc;
                        decimal amount;
                        int? receiptId;

                        // 2. Khóa bản ghi ViPham (giữ nguyên bản ghi, không xóa)
                        using (var lockCmd = conn.CreateCommand())
                        {
                            lockCmd.Transaction = tx;
                            lockCmd.CommandText = @"
                                SELECT vp.IdDocGia, dg.MaDinhDanhTruong, dg.HoTen,
                                       vp.LoaiViPham, vp.SoTien, vp.TrangThai, vp.IdPhieuThu, COALESCE(vp.MoTa, '')
                                FROM ViPham vp
                                JOIN DocGia dg ON dg.IdDocGia = vp.IdDocGia
                                WHERE vp.IdViPham = @id
                                FOR UPDATE;";
                            lockCmd.Parameters.AddWithValue("@id", req.ViolationId);

                            using (DbDataReader r = await lockCmd.ExecuteReaderAsync())
                            {
                                if (!await r.ReadAsync())
                                    throw new KeyNotFoundException($"Không tìm thấy phiếu phạt #{req.ViolationId}.");

                                readerId = r.GetInt32(0);
                                readerCode = r.GetString(1);
                                readerName = r.GetString(2);
                                vType = r.GetString(3);
                                amount = r.GetDecimal(4);
                                prevStatus = r.GetString(5);
                                receiptId = r.IsDBNull(6) ? (int?)null : r.GetInt32(6);
                                oldDesc = r.GetString(7);
                            }
                        }

                        if (prevStatus == "DA_HUY")
                            throw new InvalidOperationException($"Phiếu phạt #{req.ViolationId} đã ở trạng thái DA_HUY trước đó.");

                        var voidedAt = DateTime.Now;
                        string cleanReason = req.Reason.Trim();
                        string updatedDesc = string.IsNullOrEmpty(oldDesc)
                            ? $"[ĐÃ HỦY bởi {managerName} lúc {voidedAt:dd/MM/yyyy HH:mm}] Lý do: {cleanReason}"
                            : $"{oldDesc} | [ĐÃ HỦY bởi {managerName} lúc {voidedAt:dd/MM/yyyy HH:mm}] Lý do: {cleanReason}";

                        // 3. Cập nhật trạng thái ViPham sang DA_HUY (KHÔNG xóa dòng PhieuThu nếu đã thu tiền)
                        using (var voidCmd = conn.CreateCommand())
                        {
                            voidCmd.Transaction = tx;
                            voidCmd.CommandText = @"
                                UPDATE ViPham
                                SET TrangThai = 'DA_HUY',
                                    LyDoHuy = @lyDo,
                                    IdNguoiHuy = @nguoiHuy,
                                    ThoiDiemHuy = @thoiDiem,
                                    MoTa = @moTa
                                WHERE IdViPham = @id;";
                            voidCmd.Parameters.AddWithValue("@lyDo", cleanReason);
                            voidCmd.Parameters.AddWithValue("@nguoiHuy", req.ManagerAccountId);
                            voidCmd.Parameters.AddWithValue("@thoiDiem", voidedAt);
                            voidCmd.Parameters.AddWithValue("@moTa", updatedDesc);
                            voidCmd.Parameters.AddWithValue("@id", req.ViolationId);
                            await voidCmd.ExecuteNonQueryAsync();
                        }

                        // 4. Ghi dấu vết bắt buộc vào bảng NhatKy
                        string logDetail = receiptId.HasValue
                            ? $"Hủy hiệu lực VP#{req.ViolationId} ({vType}, {amount:N0}đ) của {readerName} ({readerCode}). Lý do: {cleanReason}. Lưu ý: Giữ nguyên chứng từ PhieuThu #{receiptId.Value} đã thu tiền."
                            : $"Hủy hiệu lực VP#{req.ViolationId} ({vType}, {amount:N0}đ) của {readerName} ({readerCode}). Lý do: {cleanReason}.";

                        using (var logCmd = conn.CreateCommand())
                        {
                            logCmd.Transaction = tx;
                            logCmd.CommandText = @"
                                INSERT INTO NhatKy (IdTaiKhoan, HanhDong, DoiTuong, IdDoiTuong, NoiDung, ThoiGian)
                                VALUES (@tk, 'HUY_PHIEU_PHAT', 'ViPham', @doiTuongId, @noiDung, @thoiGian);";
                            logCmd.Parameters.AddWithValue("@tk", req.ManagerAccountId);
                            logCmd.Parameters.AddWithValue("@doiTuongId", req.ViolationId);
                            logCmd.Parameters.AddWithValue("@noiDung", logDetail);
                            logCmd.Parameters.AddWithValue("@thoiGian", voidedAt);
                            await logCmd.ExecuteNonQueryAsync();
                        }

                        // 5. Nếu độc giả không còn khoản vi phạm CHUA_XU_LY nào -> mở khóa quyền mượn
                        bool unlocked = false;
                        using (var checkDebtCmd = conn.CreateCommand())
                        {
                            checkDebtCmd.Transaction = tx;
                            checkDebtCmd.CommandText = "SELECT COUNT(*) FROM ViPham WHERE IdDocGia = @docGia AND TrangThai = 'CHUA_XU_LY' AND SoTien > 0;";
                            checkDebtCmd.Parameters.AddWithValue("@docGia", readerId);
                            int remainDebt = Convert.ToInt32(await checkDebtCmd.ExecuteScalarAsync());

                            if (remainDebt == 0)
                            {
                                using (var unlockCmd = conn.CreateCommand())
                                {
                                    unlockCmd.Transaction = tx;
                                    unlockCmd.CommandText = "UPDATE DocGia SET DuocMuon = 1, LyDoKhoa = NULL WHERE IdDocGia = @docGia;";
                                    unlockCmd.Parameters.AddWithValue("@docGia", readerId);
                                    await unlockCmd.ExecuteNonQueryAsync();
                                    unlocked = true;
                                }
                            }
                        }

                        tx.Commit();

                        return new VoidViolationResult
                        {
                            ViolationId = req.ViolationId,
                            ReaderCode = readerCode,
                            ReaderName = readerName,
                            Amount = amount,
                            PreviousStatus = prevStatus,
                            HasExistingReceipt = receiptId.HasValue,
                            ReceiptId = receiptId,
                            ManagerName = managerName,
                            VoidedAt = voidedAt,
                            Reason = cleanReason,
                            ReaderUnlocked = unlocked
                        };
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}