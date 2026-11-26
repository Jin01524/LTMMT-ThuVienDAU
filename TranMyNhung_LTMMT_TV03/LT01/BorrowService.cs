using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TranMyNhung_LTMMT_TV03.LT01
{
    public class BorrowReaderLookup
    {
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string FullName { get; set; }
        public string ReaderType { get; set; }
        public string ProfileStatus { get; set; }
        public bool CanBorrow { get; set; }
        public string LockReason { get; set; }
        public int ActiveBorrowCount { get; set; }
        public int MaxBorrowLimit { get; set; }
        public int LoanDays { get; set; }
        public decimal UnpaidFineTotal { get; set; }
        public int OverdueCount { get; set; }
        public List<int> ActiveTitleIds { get; set; } = new List<int>();
    }

    public class BorrowCopyLookup
    {
        public int CopyId { get; set; }
        public int TitleId { get; set; }
        public string Barcode { get; set; }
        public string RegistrationNumber { get; set; }
        public string TitleName { get; set; }
        public string Status { get; set; }
        public string Condition { get; set; }
        public string LocationText { get; set; }
    }

    public class BorrowReceiptItem
    {
        public int DetailId { get; set; }
        public string Barcode { get; set; }
        public string RegistrationNumber { get; set; }
        public string TitleName { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class BorrowReceiptResult
    {
        public int LoanId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public DateTime BorrowedAt { get; set; }
        public List<BorrowReceiptItem> Items { get; set; } = new List<BorrowReceiptItem>();
    }

    public class BorrowService
    {
        private const string ConnectionString =
            "Server=127.0.0.1;Port=3306;Database=thuvien_tv03;User ID=root;Password=;CharSet=utf8mb4;";

        private static MySqlConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public async Task<BorrowReaderLookup> LookupReaderAsync(string readerCode)
        {
            if (string.IsNullOrWhiteSpace(readerCode))
                throw new ArgumentException("Vui lòng nhập hoặc quét mã thẻ sinh viên / cán bộ.");

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();

                var info = new BorrowReaderLookup();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT IdDocGia, MaDinhDanhTruong, HoTen, LoaiDocGia, TrangThaiHoSo, DuocMuon, LyDoKhoa
                        FROM DocGia
                        WHERE MaDinhDanhTruong = @code
                        LIMIT 1;";
                    cmd.Parameters.AddWithValue("@code", readerCode.Trim());

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                            throw new KeyNotFoundException($"Không tìm thấy hồ sơ độc giả với mã thẻ '{readerCode.Trim()}'.");

                        info.ReaderId = reader.GetInt32(0);
                        info.ReaderCode = reader.GetString(1);
                        info.FullName = reader.GetString(2);
                        info.ReaderType = reader.GetString(3);
                        info.ProfileStatus = reader.GetString(4);
                        info.CanBorrow = reader.GetBoolean(5);
                        info.LockReason = reader.IsDBNull(6) ? null : reader.GetString(6);
                    }
                }

                int overdueCount = 0;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT IdDauTaiLieu, HanTra FROM ChiTietPhieuMuon WHERE IdDocGia = @id AND TrangThai = 'DANG_MUON';";
                    cmd.Parameters.AddWithValue("@id", info.ReaderId);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        var now = DateTime.Now;
                        while (await reader.ReadAsync())
                        {
                            info.ActiveTitleIds.Add(reader.GetInt32(0));
                            if (reader.GetDateTime(1) < now) overdueCount++;
                        }
                    }
                }

                decimal unpaidFine = 0;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COALESCE(SUM(SoTien), 0) FROM ViPham WHERE IdDocGia = @id AND TrangThai = 'CHUA_XU_LY' AND SoTien > 0;";
                    cmd.Parameters.AddWithValue("@id", info.ReaderId);
                    unpaidFine = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
                }

                info.ActiveBorrowCount = info.ActiveTitleIds.Count;
                info.MaxBorrowLimit = info.ReaderType == "SINH_VIEN" ? 2 : 3;
                info.LoanDays = info.ReaderType == "SINH_VIEN" ? 10 : 15;
                info.UnpaidFineTotal = unpaidFine;
                info.OverdueCount = overdueCount;

                return info;
            }
        }

        public async Task<BorrowCopyLookup> LookupCopyAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                throw new ArgumentException("Vui lòng nhập hoặc quét mã vạch bản sách.");

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT b.IdBanTaiLieu, b.IdDauTaiLieu, b.MaVach, b.SoDangKyCaBiet,
                               d.NhanDe, b.TrangThai, b.TinhTrang,
                               CONCAT(v.DayKe, ' - Ngăn ', v.Ngan) AS ViTri
                        FROM BanTaiLieu b
                        JOIN DauTaiLieu d ON d.IdDauTaiLieu = b.IdDauTaiLieu
                        LEFT JOIN ViTriLuuTru v ON v.IdViTri = b.IdViTri
                        WHERE b.MaVach = @barcode OR b.SoDangKyCaBiet = @barcode
                        LIMIT 1;";
                    cmd.Parameters.AddWithValue("@barcode", barcode.Trim());

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                            throw new KeyNotFoundException($"Không tìm thấy bản tài liệu có mã '{barcode.Trim()}'.");

                        return new BorrowCopyLookup
                        {
                            CopyId = reader.GetInt32(0),
                            TitleId = reader.GetInt32(1),
                            Barcode = reader.GetString(2),
                            RegistrationNumber = reader.GetString(3),
                            TitleName = reader.GetString(4),
                            Status = reader.GetString(5),
                            Condition = reader.GetString(6),
                            LocationText = reader.IsDBNull(7) ? "Chưa xếp kệ" : reader.GetString(7)
                        };
                    }
                }
            }
        }

        public async Task<BorrowReceiptResult> CreateBorrowAsync(int readerId, List<int> copyIds, int librarianAccountId = 1)
        {
            if (copyIds == null || copyIds.Count == 0)
                throw new ArgumentException("Danh sách tài liệu mượn đang trống.");

            var distinctCopyIds = copyIds.Distinct().ToList();
            if (distinctCopyIds.Count != copyIds.Count)
                throw new ArgumentException("Không thể quét trùng cùng một bản sách vật lý.");

            var now = DateTime.Now;
            if (now >= now.Date.AddHours(20).AddMinutes(45))
                throw new InvalidOperationException("Ngừng tiếp nhận thủ tục mượn sách trong 15 phút cuối trước giờ đóng cửa (sau 20:45).");

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();
                using (var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        string readerCode, readerName, readerType;
                        using (var lockReaderCmd = conn.CreateCommand())
                        {
                            lockReaderCmd.Transaction = tx;
                            lockReaderCmd.CommandText = "SELECT MaDinhDanhTruong, HoTen, LoaiDocGia FROM DocGia WHERE IdDocGia = @id FOR UPDATE;";
                            lockReaderCmd.Parameters.AddWithValue("@id", readerId);
                            using (DbDataReader r = await lockReaderCmd.ExecuteReaderAsync())
                            {
                                if (!await r.ReadAsync()) throw new KeyNotFoundException("Hồ sơ độc giả không tồn tại.");
                                readerCode = r.GetString(0);
                                readerName = r.GetString(1);
                                readerType = r.GetString(2);
                            }
                        }

                        using (var checkCmd = conn.CreateCommand())
                        {
                            checkCmd.Transaction = tx;
                            checkCmd.CommandText = "CALL KiemTraDuocMuon(@id);";
                            checkCmd.Parameters.AddWithValue("@id", readerId);
                            await checkCmd.ExecuteNonQueryAsync();
                        }

                        int maxLimit = readerType == "SINH_VIEN" ? 2 : 3;
                        int loanDays = readerType == "SINH_VIEN" ? 10 : 15;
                        var existingTitles = new HashSet<int>();

                        using (var activeCmd = conn.CreateCommand())
                        {
                            activeCmd.Transaction = tx;
                            activeCmd.CommandText = "SELECT IdDauTaiLieu FROM ChiTietPhieuMuon WHERE IdDocGia = @id AND TrangThai = 'DANG_MUON';";
                            activeCmd.Parameters.AddWithValue("@id", readerId);
                            using (DbDataReader r = await activeCmd.ExecuteReaderAsync())
                            {
                                while (await r.ReadAsync()) existingTitles.Add(r.GetInt32(0));
                            }
                        }

                        if (existingTitles.Count + distinctCopyIds.Count > maxLimit)
                            throw new InvalidOperationException($"Vượt hạn mức mượn: Độc giả đang mượn {existingTitles.Count} cuốn, chỉ được mượn thêm tối đa {maxLimit - existingTitles.Count} cuốn.");

                        var lockedCopies = new List<BorrowCopyLookup>();
                        var requestTitleIds = new HashSet<int>();

                        foreach (var copyId in distinctCopyIds)
                        {
                            using (var lockCopyCmd = conn.CreateCommand())
                            {
                                lockCopyCmd.Transaction = tx;
                                lockCopyCmd.CommandText = @"
                                    SELECT b.IdBanTaiLieu, b.IdDauTaiLieu, b.MaVach, b.SoDangKyCaBiet, b.TrangThai, d.NhanDe
                                    FROM BanTaiLieu b
                                    JOIN DauTaiLieu d ON d.IdDauTaiLieu = b.IdDauTaiLieu
                                    WHERE b.IdBanTaiLieu = @copyId
                                    FOR UPDATE;";
                                lockCopyCmd.Parameters.AddWithValue("@copyId", copyId);

                                using (DbDataReader r = await lockCopyCmd.ExecuteReaderAsync())
                                {
                                    if (!await r.ReadAsync()) throw new KeyNotFoundException($"Bản sách ID={copyId} không tồn tại.");

                                    int titleId = r.GetInt32(1);
                                    string barcode = r.GetString(2);
                                    string regNo = r.GetString(3);
                                    string status = r.GetString(4);
                                    string titleName = r.GetString(5);

                                    if (status != "SAN_SANG")
                                        throw new InvalidOperationException($"Bản sách [{barcode}] - '{titleName}' hiện không sẵn sàng (Trạng thái: {status}).");

                                    if (existingTitles.Contains(titleId) || !requestTitleIds.Add(titleId))
                                        throw new InvalidOperationException($"Chặn trùng nhan đề: Độc giả không được mượn 2 cuốn cùng nhan đề '{titleName}'.");

                                    lockedCopies.Add(new BorrowCopyLookup
                                    {
                                        CopyId = copyId,
                                        TitleId = titleId,
                                        Barcode = barcode,
                                        RegistrationNumber = regNo,
                                        TitleName = titleName
                                    });
                                }
                            }
                        }

                        int loanId;
                        var borrowedAt = DateTime.Now;
                        var dueDate = borrowedAt.Date.AddDays(loanDays).AddHours(21);

                        using (var insertLoanCmd = conn.CreateCommand())
                        {
                            insertLoanCmd.Transaction = tx;
                            insertLoanCmd.CommandText = @"
                                INSERT INTO PhieuMuon (IdDocGia, ThoiDiemMuon, IdThuThu, TrangThai)
                                VALUES (@docGia, @thoiDiem, @thuThu, 'DANG_MO');
                                SELECT LAST_INSERT_ID();";
                            insertLoanCmd.Parameters.AddWithValue("@docGia", readerId);
                            insertLoanCmd.Parameters.AddWithValue("@thoiDiem", borrowedAt);
                            insertLoanCmd.Parameters.AddWithValue("@thuThu", librarianAccountId);
                            loanId = Convert.ToInt32(await insertLoanCmd.ExecuteScalarAsync());
                        }

                        var receiptItems = new List<BorrowReceiptItem>();
                        foreach (var item in lockedCopies)
                        {
                            int detailId;
                            using (var insertDetailCmd = conn.CreateCommand())
                            {
                                insertDetailCmd.Transaction = tx;
                                insertDetailCmd.CommandText = @"
                                    INSERT INTO ChiTietPhieuMuon (IdPhieuMuon, IdDocGia, IdBanTaiLieu, IdDauTaiLieu, HanTra, TrangThai)
                                    VALUES (@phieu, @docGia, @ban, @dau, @hanTra, 'DANG_MUON');
                                    SELECT LAST_INSERT_ID();";
                                insertDetailCmd.Parameters.AddWithValue("@phieu", loanId);
                                insertDetailCmd.Parameters.AddWithValue("@docGia", readerId);
                                insertDetailCmd.Parameters.AddWithValue("@ban", item.CopyId);
                                insertDetailCmd.Parameters.AddWithValue("@dau", item.TitleId);
                                insertDetailCmd.Parameters.AddWithValue("@hanTra", dueDate);
                                detailId = Convert.ToInt32(await insertDetailCmd.ExecuteScalarAsync());
                            }

                            using (var updateCopyCmd = conn.CreateCommand())
                            {
                                updateCopyCmd.Transaction = tx;
                                updateCopyCmd.CommandText = "UPDATE BanTaiLieu SET TrangThai = 'DANG_MUON' WHERE IdBanTaiLieu = @ban;";
                                updateCopyCmd.Parameters.AddWithValue("@ban", item.CopyId);
                                await updateCopyCmd.ExecuteNonQueryAsync();
                            }

                            receiptItems.Add(new BorrowReceiptItem
                            {
                                DetailId = detailId,
                                Barcode = item.Barcode,
                                RegistrationNumber = item.RegistrationNumber,
                                TitleName = item.TitleName,
                                DueDate = dueDate
                            });
                        }

                        await tx.CommitAsync();
                        return new BorrowReceiptResult
                        {
                            LoanId = loanId,
                            ReaderId = readerId,
                            ReaderCode = readerCode,
                            ReaderName = readerName,
                            BorrowedAt = borrowedAt,
                            Items = receiptItems
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