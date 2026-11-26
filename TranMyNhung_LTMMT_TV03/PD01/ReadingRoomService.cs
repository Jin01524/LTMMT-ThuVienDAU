using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TranMyNhung_LTMMT_TV03.PD01
{
    public class ReadingRoomVisitItem
    {
        public int VisitId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ReaderType { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string LockerKey { get; set; }
        public string Note { get; set; }
        public bool IsInside => !CheckOutTime.HasValue;
    }

    public class CheckInOutResult
    {
        public int VisitId { get; set; }
        public string ActionType { get; set; } // "CHECK_IN" hoặc "CHECK_OUT"
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ReaderType { get; set; }
        public DateTime Timestamp { get; set; }
        public string LockerKey { get; set; }
        public string ReminderMessage { get; set; }
    }

    public class ReadingRoomService
    {
        private const string ConnectionString =
            "Server=127.0.0.1;Port=3306;Database=thuvien_tv03;User ID=root;Password=;CharSet=utf8mb4;";

        private static MySqlConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public async Task<List<ReadingRoomVisitItem>> GetTodayVisitsAsync(string keyword = "", bool onlyInside = false)
        {
            var list = new List<ReadingRoomVisitItem>();
            string kw = (keyword ?? "").Trim();

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT lv.IdLuot, lv.IdDocGia, dg.MaDinhDanhTruong, dg.HoTen, dg.LoaiDocGia,
                               lv.ThoiDiemVao, lv.ThoiDiemRa, COALESCE(lv.MaChiaKhoaTu, ''), COALESCE(lv.GhiChu, '')
                        FROM LuotVaoPhongDoc lv
                        JOIN DocGia dg ON dg.IdDocGia = lv.IdDocGia
                        WHERE DATE(lv.ThoiDiemVao) = CURDATE()
                          AND (@onlyIn = 0 OR lv.ThoiDiemRa IS NULL)
                          AND (@kw = '' OR dg.MaDinhDanhTruong LIKE @likeKw OR dg.HoTen LIKE @likeKw OR lv.MaChiaKhoaTu LIKE @likeKw)
                        ORDER BY (lv.ThoiDiemRa IS NULL) DESC, lv.ThoiDiemVao DESC;";
                    cmd.Parameters.AddWithValue("@onlyIn", onlyInside ? 1 : 0);
                    cmd.Parameters.AddWithValue("@kw", kw);
                    cmd.Parameters.AddWithValue("@likeKw", "%" + kw + "%");

                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            list.Add(new ReadingRoomVisitItem
                            {
                                VisitId = r.GetInt32(0),
                                ReaderId = r.GetInt32(1),
                                ReaderCode = r.GetString(2),
                                ReaderName = r.GetString(3),
                                ReaderType = r.GetString(4),
                                CheckInTime = r.GetDateTime(5),
                                CheckOutTime = r.IsDBNull(6) ? (DateTime?)null : r.GetDateTime(6),
                                LockerKey = r.GetString(7),
                                Note = r.GetString(8)
                            });
                        }
                    }
                }
            }

            return list;
        }

        public async Task<CheckInOutResult> ScanCardCheckInOrOutAsync(string readerCode, string lockerKey = "", string note = "", int librarianAccountId = 1)
        {
            if (string.IsNullOrWhiteSpace(readerCode))
                throw new ArgumentException("Vui lòng quét hoặc nhập mã thẻ trường của độc giả.");

            string cleanCode = readerCode.Trim();
            string cleanLocker = (lockerKey ?? "").Trim();
            string cleanNote = (note ?? "").Trim();

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();
                using (var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        int readerId;
                        string fullName, readerType, profileStatus;

                        // 1. Định danh độc giả qua thẻ trường
                        using (var readerCmd = conn.CreateCommand())
                        {
                            readerCmd.Transaction = tx;
                            readerCmd.CommandText = @"
                                SELECT IdDocGia, HoTen, LoaiDocGia, TrangThaiHoSo
                                FROM DocGia
                                WHERE MaDinhDanhTruong = @code
                                FOR UPDATE;";
                            readerCmd.Parameters.AddWithValue("@code", cleanCode);

                            using (DbDataReader r = await readerCmd.ExecuteReaderAsync())
                            {
                                if (!await r.ReadAsync())
                                    throw new KeyNotFoundException($"Không tìm thấy hồ sơ độc giả với mã thẻ '{cleanCode}'.");

                                readerId = r.GetInt32(0);
                                fullName = r.GetString(1);
                                readerType = r.GetString(2);
                                profileStatus = r.GetString(3);
                            }
                        }

                        if (profileStatus != "HOAT_DONG")
                            throw new InvalidOperationException($"Hồ sơ độc giả '{fullName}' ({cleanCode}) hiện không hoạt động (Trạng thái: {profileStatus}).");

                        // 2. Kiểm tra độc giả đang có lượt ở trong phòng đọc chưa ra hay không
                        int? openVisitId = null;
                        string existingLocker = "";
                        using (var openCmd = conn.CreateCommand())
                        {
                            openCmd.Transaction = tx;
                            openCmd.CommandText = @"
                                SELECT IdLuot, COALESCE(MaChiaKhoaTu, '')
                                FROM LuotVaoPhongDoc
                                WHERE IdDocGia = @docGia AND ThoiDiemRa IS NULL
                                ORDER BY ThoiDiemVao DESC
                                LIMIT 1
                                FOR UPDATE;";
                            openCmd.Parameters.AddWithValue("@docGia", readerId);

                            using (DbDataReader r = await openCmd.ExecuteReaderAsync())
                            {
                                if (await r.ReadAsync())
                                {
                                    openVisitId = r.GetInt32(0);
                                    existingLocker = r.GetString(1);
                                }
                            }
                        }

                        var now = DateTime.Now;

                        // 3A. Nếu đang ở trong phòng -> Ghi nhận RA phòng đọc
                        if (openVisitId.HasValue)
                        {
                            using (var outCmd = conn.CreateCommand())
                            {
                                outCmd.Transaction = tx;
                                outCmd.CommandText = @"
                                    UPDATE LuotVaoPhongDoc
                                    SET ThoiDiemRa = @ra,
                                        GhiChu = CASE WHEN @ghiChu = '' THEN GhiChu ELSE CONCAT(COALESCE(GhiChu, ''), ' | Ra: ', @ghiChu) END
                                    WHERE IdLuot = @id;";
                                outCmd.Parameters.AddWithValue("@ra", now);
                                outCmd.Parameters.AddWithValue("@ghiChu", cleanNote);
                                outCmd.Parameters.AddWithValue("@id", openVisitId.Value);
                                await outCmd.ExecuteNonQueryAsync();
                            }

                            tx.Commit();

                            string keyReminder = string.IsNullOrEmpty(existingLocker)
                                ? "Không có chìa khóa tủ đồ cần thu hồi."
                                : $"THU HỒI CHÌA KHÓA TỦ SỐ [{existingLocker}].";

                            return new CheckInOutResult
                            {
                                VisitId = openVisitId.Value,
                                ActionType = "CHECK_OUT",
                                ReaderCode = cleanCode,
                                ReaderName = fullName,
                                ReaderType = readerType,
                                Timestamp = now,
                                LockerKey = existingLocker,
                                ReminderMessage = $"NHẮC THAO TÁC RA PHÒNG ĐỌC:\n1. {keyReminder}\n2. Kiểm tra sách đọc tại chỗ đã đặt lại đúng nơi quy định.\n3. TRẢ LẠI THẺ TRƯỜNG [{cleanCode}] cho độc giả."
                            };
                        }

                        // 3B. Nếu chưa vào phòng -> Ghi nhận VÀO phòng đọc
                        if (!string.IsNullOrEmpty(cleanLocker))
                        {
                            using (var lockerCheckCmd = conn.CreateCommand())
                            {
                                lockerCheckCmd.Transaction = tx;
                                lockerCheckCmd.CommandText = @"
                                    SELECT COUNT(*) FROM LuotVaoPhongDoc
                                    WHERE MaChiaKhoaTu = @tu AND ThoiDiemRa IS NULL;";
                                lockerCheckCmd.Parameters.AddWithValue("@tu", cleanLocker);
                                int inUse = Convert.ToInt32(await lockerCheckCmd.ExecuteScalarAsync());
                                if (inUse > 0)
                                    throw new InvalidOperationException($"Chìa khóa tủ số [{cleanLocker}] hiện đang được độc giả khác giữ trong phòng.");
                            }
                        }

                        int newVisitId;
                        using (var inCmd = conn.CreateCommand())
                        {
                            inCmd.Transaction = tx;
                            inCmd.CommandText = @"
                                INSERT INTO LuotVaoPhongDoc (IdDocGia, ThoiDiemVao, MaChiaKhoaTu, IdThuThu, GhiChu)
                                VALUES (@docGia, @vao, @tu, @thuThu, @ghiChu);
                                SELECT LAST_INSERT_ID();";
                            inCmd.Parameters.AddWithValue("@docGia", readerId);
                            inCmd.Parameters.AddWithValue("@vao", now);
                            inCmd.Parameters.AddWithValue("@tu", string.IsNullOrEmpty(cleanLocker) ? (object)DBNull.Value : cleanLocker);
                            inCmd.Parameters.AddWithValue("@thuThu", librarianAccountId);
                            inCmd.Parameters.AddWithValue("@ghiChu", cleanNote);
                            newVisitId = Convert.ToInt32(await inCmd.ExecuteScalarAsync());
                        }

                        tx.Commit();

                        string giveKeyMsg = string.IsNullOrEmpty(cleanLocker)
                            ? "Độc giả không gửi túi/tủ đồ."
                            : $"GIAO CHÌA KHÓA TỦ ĐỒ SỐ [{cleanLocker}].";

                        return new CheckInOutResult
                        {
                            VisitId = newVisitId,
                            ActionType = "CHECK_IN",
                            ReaderCode = cleanCode,
                            ReaderName = fullName,
                            ReaderType = readerType,
                            Timestamp = now,
                            LockerKey = cleanLocker,
                            ReminderMessage = $"NHẮC THAO TÁC VÀO PHÒNG ĐỌC:\n1. GIỮ LẠI THẺ TRƯỜNG [{cleanCode}] tại quầy.\n2. {giveKeyMsg}\n3. Nhắc nội quy: Chỉ lấy TỐI ĐA 2 CUỐN tài liệu đọc tại chỗ mỗi lượt."
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