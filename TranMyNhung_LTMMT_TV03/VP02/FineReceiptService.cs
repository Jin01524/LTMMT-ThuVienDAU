using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TranMyNhung_LTMMT_TV03.VP02
{
    public class UnpaidViolationItem
    {
        public int ViolationId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ViolationType { get; set; }
        public int OverdueDays { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Barcode { get; set; }
        public string TitleName { get; set; }
    }

    public class CollectFineRequest
    {
        public int LibrarianAccountId { get; set; } = 1;
        public string PaymentMethod { get; set; } // "TIEN_MAT", "CHUYEN_KHOAN"
        public string ReferenceNote { get; set; }
        public List<int> ViolationIds { get; set; } = new List<int>();
    }

    public class FineReceiptResult
    {
        public int ReceiptId { get; set; }
        public string ReceiptCode { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string CollectorName { get; set; }
        public DateTime CollectedAt { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public string BasisSummary { get; set; }
        public bool ReaderUnlocked { get; set; }
    }

    public class FineReceiptService
    {
        private const string ConnectionString =
            "Server=127.0.0.1;Port=3306;Database=thuvien_tv03;User ID=root;Password=110704;CharSet=utf8mb4;";

        private static MySqlConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        private async Task<string> VerifyCollectorPermissionAsync(MySqlConnection conn, MySqlTransaction tx, int accountId)
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
                        throw new UnauthorizedAccessException("Tài khoản thu ngân không tồn tại trong hệ thống.");

                    string name = r.GetString(0);
                    string role = r.GetString(1);
                    string status = r.GetString(2);

                    if (status != "HOAT_DONG")
                        throw new UnauthorizedAccessException("Tài khoản thủ thư đang bị khóa, không có quyền lập phiếu thu.");

                    if (role != "THU_THU" && role != "QUAN_LY_THU_VIEN")
                        throw new UnauthorizedAccessException($"Vai trò '{role}' không được phép thu phí phạt (chỉ dành cho THU_THU hoặc QUAN_LY_THU_VIEN).");

                    return name;
                }
            }
        }

        public async Task<List<UnpaidViolationItem>> GetUnpaidViolationsAsync(string keyword = "")
        {
            var list = new List<UnpaidViolationItem>();
            string kw = (keyword ?? "").Trim();

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT vp.IdViPham, vp.IdDocGia, dg.MaDinhDanhTruong, dg.HoTen,
                               vp.LoaiViPham, COALESCE(vp.SoNgayTre, 0), vp.SoTien,
                               COALESCE(vp.MoTa, ''), vp.NgayLap,
                               COALESCE(b.MaVach, '—'), COALESCE(d.NhanDe, '—')
                        FROM ViPham vp
                        JOIN DocGia dg ON dg.IdDocGia = vp.IdDocGia
                        LEFT JOIN ChiTietPhieuMuon ct ON ct.IdChiTiet = vp.IdChiTietPhieuMuon
                        LEFT JOIN BanTaiLieu b ON b.IdBanTaiLieu = ct.IdBanTaiLieu
                        LEFT JOIN DauTaiLieu d ON d.IdDauTaiLieu = ct.IdDauTaiLieu
                        WHERE vp.TrangThai = 'CHUA_XU_LY'
                          AND vp.SoTien > 0
                          AND (@kw = '' OR dg.MaDinhDanhTruong LIKE @likeKw OR dg.HoTen LIKE @likeKw OR CAST(vp.IdViPham AS CHAR) = @kw)
                        ORDER BY dg.MaDinhDanhTruong ASC, vp.NgayLap ASC;";
                    cmd.Parameters.AddWithValue("@kw", kw);
                    cmd.Parameters.AddWithValue("@likeKw", "%" + kw + "%");

                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            list.Add(new UnpaidViolationItem
                            {
                                ViolationId = r.GetInt32(0),
                                ReaderId = r.GetInt32(1),
                                ReaderCode = r.GetString(2),
                                ReaderName = r.GetString(3),
                                ViolationType = r.GetString(4),
                                OverdueDays = r.GetInt32(5),
                                Amount = r.GetDecimal(6),
                                Description = r.GetString(7),
                                CreatedAt = r.GetDateTime(8),
                                Barcode = r.GetString(9),
                                TitleName = r.GetString(10)
                            });
                        }
                    }
                }
            }

            return list;
        }

        public async Task<FineReceiptResult> CollectFinesAsync(CollectFineRequest req)
        {
            if (req == null || req.ViolationIds == null || req.ViolationIds.Count == 0)
                throw new ArgumentException("Vui lòng chọn ít nhất một khoản vi phạm để thu tiền.");

            var distinctIds = req.ViolationIds.Distinct().ToList();
            if (distinctIds.Count != req.ViolationIds.Count)
                throw new ArgumentException("Danh sách khoản thu bị trùng lặp mã vi phạm.");

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();
                using (var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        // 1. Kiểm tra quyền thủ thư ngay tại Service
                        string collectorName = await VerifyCollectorPermissionAsync(conn, tx, req.LibrarianAccountId);

                        int? targetReaderId = null;
                        string readerCode = "", readerName = "";
                        decimal totalAmount = 0;
                        var basisParts = new List<string>();

                        // 2. Khóa từng khoản vi phạm để chống thu lặp & đảm bảo cùng 1 độc giả
                        foreach (int vId in distinctIds)
                        {
                            using (var lockCmd = conn.CreateCommand())
                            {
                                lockCmd.Transaction = tx;
                                lockCmd.CommandText = @"
                                    SELECT vp.IdDocGia, dg.MaDinhDanhTruong, dg.HoTen,
                                           vp.LoaiViPham, vp.SoTien, vp.TrangThai, vp.IdPhieuThu
                                    FROM ViPham vp
                                    JOIN DocGia dg ON dg.IdDocGia = vp.IdDocGia
                                    WHERE vp.IdViPham = @id
                                    FOR UPDATE;";
                                lockCmd.Parameters.AddWithValue("@id", vId);

                                using (DbDataReader r = await lockCmd.ExecuteReaderAsync())
                                {
                                    if (!await r.ReadAsync())
                                        throw new KeyNotFoundException($"Không tìm thấy khoản vi phạm #{vId}.");

                                    int rId = r.GetInt32(0);
                                    string rCode = r.GetString(1);
                                    string rName = r.GetString(2);
                                    string vType = r.GetString(3);
                                    decimal amt = r.GetDecimal(4);
                                    string status = r.GetString(5);
                                    bool hasReceipt = !r.IsDBNull(6);

                                    if (status != "CHUA_XU_LY" || hasReceipt)
                                        throw new InvalidOperationException($"Khoản vi phạm #{vId} đã được thu tiền trước đó. Không thể thu lặp!");

                                    if (amt <= 0)
                                        throw new InvalidOperationException($"Khoản vi phạm #{vId} có số tiền bằng 0, không thuộc diện thu tiền.");

                                    if (targetReaderId == null)
                                    {
                                        targetReaderId = rId;
                                        readerCode = rCode;
                                        readerName = rName;
                                    }
                                    else if (targetReaderId.Value != rId)
                                    {
                                        throw new InvalidOperationException("Một biên lai chỉ được lập cho các khoản vi phạm của cùng một độc giả.");
                                    }

                                    totalAmount += amt;
                                    basisParts.Add($"VP#{vId} ({vType}: {amt:N0}đ)");
                                }
                            }
                        }

                        var collectedAt = DateTime.Now;
                        string basisSummary = string.Join("; ", basisParts);
                        if (!string.IsNullOrWhiteSpace(req.ReferenceNote))
                            basisSummary += $" | Ghi chú: {req.ReferenceNote.Trim()}";

                        string paymentMethod = string.IsNullOrWhiteSpace(req.PaymentMethod) ? "TIEN_MAT" : req.PaymentMethod;

                        // 3. Tạo 1 bản ghi PhieuThu cho nhiều khoản vi phạm (lưu người thu, ngày thu, căn cứ)
                        int receiptId;
                        using (var insertReceiptCmd = conn.CreateCommand())
                        {
                            insertReceiptCmd.Transaction = tx;
                            insertReceiptCmd.CommandText = @"
                                INSERT INTO PhieuThu (IdDocGia, IdThuThu, ThoiDiemThu, TongTien, HinhThucThu, CanCuThu, TrangThai)
                                VALUES (@docGia, @thuThu, @thoiDiem, @tongTien, @hinhThuc, @canCu, 'DA_THU');
                                SELECT LAST_INSERT_ID();";
                            insertReceiptCmd.Parameters.AddWithValue("@docGia", targetReaderId.Value);
                            insertReceiptCmd.Parameters.AddWithValue("@thuThu", req.LibrarianAccountId);
                            insertReceiptCmd.Parameters.AddWithValue("@thoiDiem", collectedAt);
                            insertReceiptCmd.Parameters.AddWithValue("@tongTien", totalAmount);
                            insertReceiptCmd.Parameters.AddWithValue("@hinhThuc", paymentMethod);
                            insertReceiptCmd.Parameters.AddWithValue("@canCu", basisSummary);
                            receiptId = Convert.ToInt32(await insertReceiptCmd.ExecuteScalarAsync());
                        }

                        // 4. Cập nhật tất cả khoản ViPham được chọn sang DA_XU_LY & gắn IdPhieuThu
                        foreach (int vId in distinctIds)
                        {
                            using (var updateVpCmd = conn.CreateCommand())
                            {
                                updateVpCmd.Transaction = tx;
                                updateVpCmd.CommandText = @"
                                    UPDATE ViPham
                                    SET TrangThai = 'DA_XU_LY',
                                        IdPhieuThu = @phieuThu,
                                        NgayXuLy = @ngayXuLy
                                    WHERE IdViPham = @id AND TrangThai = 'CHUA_XU_LY';";
                                updateVpCmd.Parameters.AddWithValue("@phieuThu", receiptId);
                                updateVpCmd.Parameters.AddWithValue("@ngayXuLy", collectedAt);
                                updateVpCmd.Parameters.AddWithValue("@id", vId);

                                int affected = await updateVpCmd.ExecuteNonQueryAsync();
                                if (affected == 0)
                                    throw new InvalidOperationException($"Khoản vi phạm #{vId} vừa được xử lý bởi giao dịch khác.");
                            }
                        }

                        // 5. Nếu độc giả hết nợ phạt -> tự động mở khóa quyền mượn
                        bool unlocked = false;
                        using (var remainCmd = conn.CreateCommand())
                        {
                            remainCmd.Transaction = tx;
                            remainCmd.CommandText = "SELECT COUNT(*) FROM ViPham WHERE IdDocGia = @docGia AND TrangThai = 'CHUA_XU_LY' AND SoTien > 0;";
                            remainCmd.Parameters.AddWithValue("@docGia", targetReaderId.Value);
                            int remainCount = Convert.ToInt32(await remainCmd.ExecuteScalarAsync());

                            if (remainCount == 0)
                            {
                                using (var unlockCmd = conn.CreateCommand())
                                {
                                    unlockCmd.Transaction = tx;
                                    unlockCmd.CommandText = "UPDATE DocGia SET DuocMuon = 1, LyDoKhoa = NULL WHERE IdDocGia = @docGia;";
                                    unlockCmd.Parameters.AddWithValue("@docGia", targetReaderId.Value);
                                    await unlockCmd.ExecuteNonQueryAsync();
                                    unlocked = true;
                                }
                            }
                        }

                        await tx.CommitAsync();

                        return new FineReceiptResult
                        {
                            ReceiptId = receiptId,
                            ReceiptCode = $"PT-{collectedAt:yyyyMMdd}-{receiptId:D4}",
                            ReaderId = targetReaderId.Value,
                            ReaderCode = readerCode,
                            ReaderName = readerName,
                            CollectorName = collectorName,
                            CollectedAt = collectedAt,
                            PaymentMethod = paymentMethod,
                            TotalAmount = totalAmount,
                            ItemCount = distinctIds.Count,
                            BasisSummary = basisSummary,
                            ReaderUnlocked = unlocked
                        };
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                }
            }
        }
    }
}