using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TranMyNhung_LTMMT_TV03.LT02
{
    public class ActiveLoanCopyInfo
    {
        public int DetailId { get; set; }
        public int LoanId { get; set; }
        public int CopyId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string Barcode { get; set; }
        public string RegistrationNumber { get; set; }
        public string TitleName { get; set; }
        public DateTime BorrowedAt { get; set; }
        public DateTime DueDate { get; set; }
        public string CurrentCondition { get; set; }
        public int OverdueDays { get; set; }
    }

    public class ReturnItemInput
    {
        public int DetailId { get; set; }
        public string ReturnOutcome { get; set; } // "BINH_THUONG", "HU_HONG", "MAT"
        public string BookCondition { get; set; } // "TOT", "MOI", "CU", "HU_HONG", "MAT"
        public string Note { get; set; }
    }

    public class ReturnProcessedItem
    {
        public int DetailId { get; set; }
        public int LoanId { get; set; }
        public string Barcode { get; set; }
        public string TitleName { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public DateTime ReturnedAt { get; set; }
        public int OverdueDays { get; set; }
        public string ReturnOutcome { get; set; }
        public bool HasViolationBasis { get; set; }
        public string ViolationSummary { get; set; }
    }

    public class ReturnService
    {
        private const string ConnectionString =
            "Server=127.0.0.1;Port=3306;Database=thuvien_tv03;User ID=root;Password=110704;CharSet=utf8mb4;";

        private static MySqlConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public async Task<ActiveLoanCopyInfo> LookupActiveBorrowByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                throw new ArgumentException("Vui lòng nhập hoặc quét mã vạch / ĐKCB bản sách.");

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT ct.IdChiTiet, ct.IdPhieuMuon, ct.IdBanTaiLieu, ct.IdDocGia,
                               dg.MaDinhDanhTruong, dg.HoTen,
                               b.MaVach, b.SoDangKyCaBiet, d.NhanDe,
                               pm.ThoiDiemMuon, ct.HanTra, b.TinhTrang, ct.TrangThai
                        FROM BanTaiLieu b
                        JOIN DauTaiLieu d ON d.IdDauTaiLieu = b.IdDauTaiLieu
                        LEFT JOIN ChiTietPhieuMuon ct ON ct.IdBanTaiLieu = b.IdBanTaiLieu AND ct.TrangThai = 'DANG_MUON'
                        LEFT JOIN PhieuMuon pm ON pm.IdPhieuMuon = ct.IdPhieuMuon
                        LEFT JOIN DocGia dg ON dg.IdDocGia = ct.IdDocGia
                        WHERE b.MaVach = @code OR b.SoDangKyCaBiet = @code
                        LIMIT 1;";
                    cmd.Parameters.AddWithValue("@code", barcode.Trim());

                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        if (!await r.ReadAsync())
                            throw new KeyNotFoundException($"Không tìm thấy bản tài liệu có mã '{barcode.Trim()}'.");

                        if (r.IsDBNull(0))
                            throw new InvalidOperationException($"Bản sách '{barcode.Trim()}' hiện KHÔNG ở trạng thái đang mượn (có thể đã được trả trước đó).");

                        DateTime dueDate = r.GetDateTime(10);
                        int overdueDays = DateTime.Now > dueDate
                            ? Math.Max(1, (int)Math.Ceiling((DateTime.Now - dueDate).TotalDays))
                            : 0;

                        return new ActiveLoanCopyInfo
                        {
                            DetailId = r.GetInt32(0),
                            LoanId = r.GetInt32(1),
                            CopyId = r.GetInt32(2),
                            ReaderId = r.GetInt32(3),
                            ReaderCode = r.GetString(4),
                            ReaderName = r.GetString(5),
                            Barcode = r.GetString(6),
                            RegistrationNumber = r.GetString(7),
                            TitleName = r.GetString(8),
                            BorrowedAt = r.GetDateTime(9),
                            DueDate = dueDate,
                            CurrentCondition = r.GetString(11),
                            OverdueDays = overdueDays
                        };
                    }
                }
            }
        }

        public async Task<List<ReturnProcessedItem>> ProcessReturnsAsync(List<ReturnItemInput> items)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Danh sách trả sách đang trống.");

            var results = new List<ReturnProcessedItem>();
            var touchedLoanIds = new HashSet<int>();
            var returnedAt = DateTime.Now;

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();
                using (var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        foreach (var input in items)
                        {
                            int loanId, copyId;
                            string detailStatus, barcode, titleName, readerCode, readerName;
                            DateTime dueDate;

                            // 1. Khóa dòng ChiTietPhieuMuon chống trả lặp 2 lần đồng thời
                            using (var lockCmd = conn.CreateCommand())
                            {
                                lockCmd.Transaction = tx;
                                lockCmd.CommandText = @"
                                    SELECT ct.IdPhieuMuon, ct.IdBanTaiLieu, ct.TrangThai, ct.HanTra,
                                           b.MaVach, d.NhanDe, dg.MaDinhDanhTruong, dg.HoTen
                                    FROM ChiTietPhieuMuon ct
                                    JOIN BanTaiLieu b ON b.IdBanTaiLieu = ct.IdBanTaiLieu
                                    JOIN DauTaiLieu d ON d.IdDauTaiLieu = ct.IdDauTaiLieu
                                    JOIN DocGia dg ON dg.IdDocGia = ct.IdDocGia
                                    WHERE ct.IdChiTiet = @detailId
                                    FOR UPDATE;";
                                lockCmd.Parameters.AddWithValue("@detailId", input.DetailId);

                                using (DbDataReader r = await lockCmd.ExecuteReaderAsync())
                                {
                                    if (!await r.ReadAsync())
                                        throw new KeyNotFoundException($"Không tìm thấy chi tiết mượn ID={input.DetailId}.");

                                    loanId = r.GetInt32(0);
                                    copyId = r.GetInt32(1);
                                    detailStatus = r.GetString(2);
                                    dueDate = r.GetDateTime(3);
                                    barcode = r.GetString(4);
                                    titleName = r.GetString(5);
                                    readerCode = r.GetString(6);
                                    readerName = r.GetString(7);
                                }
                            }

                            if (detailStatus != "DANG_MUON")
                                throw new InvalidOperationException($"Bản sách [{barcode}] đã được ghi nhận trả trước đó (Trạng thái: {detailStatus}). Không thể ghi lặp.");

                            // 2. Xác định trạng thái cập nhật cho ChiTietPhieuMuon và BanTaiLieu
                            string newDetailStatus = "DA_TRA";
                            string newCopyStatus = "SAN_SANG";
                            string newCopyCondition = string.IsNullOrWhiteSpace(input.BookCondition) ? "BINH_THUONG" : input.BookCondition;

                            if (input.ReturnOutcome == "HU_HONG")
                            {
                                newDetailStatus = "HU_HONG";
                                newCopyStatus = "HU_HONG";
                                newCopyCondition = "HU_HONG";
                            }
                            else if (input.ReturnOutcome == "MAT")
                            {
                                newDetailStatus = "MAT";
                                newCopyStatus = "MAT";
                                newCopyCondition = "MAT";
                            }

                            // 3. Cập nhật ChiTietPhieuMuon
                            using (var updateDetailCmd = conn.CreateCommand())
                            {
                                updateDetailCmd.Transaction = tx;
                                updateDetailCmd.CommandText = @"
                                    UPDATE ChiTietPhieuMuon
                                    SET TrangThai = @trangThai,
                                        ThoiDiemTra = @thoiDiemTra,
                                        TinhTrangKhiTra = @tinhTrang,
                                        GhiChuTra = @ghiChu
                                    WHERE IdChiTiet = @detailId AND TrangThai = 'DANG_MUON';";
                                updateDetailCmd.Parameters.AddWithValue("@trangThai", newDetailStatus);
                                updateDetailCmd.Parameters.AddWithValue("@thoiDiemTra", returnedAt);
                                updateDetailCmd.Parameters.AddWithValue("@tinhTrang", newCopyCondition);
                                updateDetailCmd.Parameters.AddWithValue("@ghiChu", (object)input.Note ?? DBNull.Value);
                                updateDetailCmd.Parameters.AddWithValue("@detailId", input.DetailId);

                                int affected = await updateDetailCmd.ExecuteNonQueryAsync();
                                if (affected == 0)
                                    throw new InvalidOperationException($"Bản sách [{barcode}] đã được xử lý bởi thao tác khác.");
                            }

                            // 4. Cập nhật BanTaiLieu
                            using (var updateCopyCmd = conn.CreateCommand())
                            {
                                updateCopyCmd.Transaction = tx;
                                updateCopyCmd.CommandText = @"
                                    UPDATE BanTaiLieu
                                    SET TrangThai = @trangThai,
                                        TinhTrang = @tinhTrang
                                    WHERE IdBanTaiLieu = @copyId;";
                                updateCopyCmd.Parameters.AddWithValue("@trangThai", newCopyStatus);
                                updateCopyCmd.Parameters.AddWithValue("@tinhTrang", newCopyCondition);
                                updateCopyCmd.Parameters.AddWithValue("@copyId", copyId);
                                await updateCopyCmd.ExecuteNonQueryAsync();
                            }

                            touchedLoanIds.Add(loanId);

                            int overdueDays = returnedAt > dueDate
                                ? Math.Max(1, (int)Math.Ceiling((returnedAt - dueDate).TotalDays))
                                : 0;

                            var reasons = new List<string>();
                            if (overdueDays > 0) reasons.Add($"Quá hạn {overdueDays} ngày");
                            if (input.ReturnOutcome == "HU_HONG") reasons.Add("Hư hỏng tài liệu");
                            if (input.ReturnOutcome == "MAT") reasons.Add("Làm mất tài liệu");

                            results.Add(new ReturnProcessedItem
                            {
                                DetailId = input.DetailId,
                                LoanId = loanId,
                                Barcode = barcode,
                                TitleName = titleName,
                                ReaderCode = readerCode,
                                ReaderName = readerName,
                                ReturnedAt = returnedAt,
                                OverdueDays = overdueDays,
                                ReturnOutcome = input.ReturnOutcome,
                                HasViolationBasis = reasons.Count > 0,
                                ViolationSummary = reasons.Count > 0 ? string.Join(" + ", reasons) : "Không vi phạm"
                            });
                        }

                        // 5. Hỗ trợ trả một phần: Chỉ đóng PhieuMuon khi TẤT CẢ chi tiết trong phiếu không còn DANG_MUON
                        foreach (int loanId in touchedLoanIds)
                        {
                            using (var checkRemainCmd = conn.CreateCommand())
                            {
                                checkRemainCmd.Transaction = tx;
                                checkRemainCmd.CommandText = "SELECT COUNT(*) FROM ChiTietPhieuMuon WHERE IdPhieuMuon = @loanId AND TrangThai = 'DANG_MUON';";
                                checkRemainCmd.Parameters.AddWithValue("@loanId", loanId);
                                int remaining = Convert.ToInt32(await checkRemainCmd.ExecuteScalarAsync());

                                if (remaining == 0)
                                {
                                    using (var closeLoanCmd = conn.CreateCommand())
                                    {
                                        closeLoanCmd.Transaction = tx;
                                        closeLoanCmd.CommandText = "UPDATE PhieuMuon SET TrangThai = 'DA_DONG' WHERE IdPhieuMuon = @loanId;";
                                        closeLoanCmd.Parameters.AddWithValue("@loanId", loanId);
                                        await closeLoanCmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        await tx.CommitAsync();
                        return results;
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