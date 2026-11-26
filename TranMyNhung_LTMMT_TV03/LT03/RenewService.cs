using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TranMyNhung_LTMMT_TV03.LT03
{
    public class RenewableLoanItem
    {
        public int DetailId { get; set; }
        public int LoanId { get; set; }
        public int ReaderId { get; set; }
        public int TitleId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ReaderType { get; set; }
        public string Barcode { get; set; }
        public string TitleName { get; set; }
        public DateTime DueDate { get; set; }
        public int RenewCount { get; set; }
        public bool HasReservation { get; set; }
        public bool CanRenew { get; set; }
        public string BlockReason { get; set; }
    }

    public class RenewResult
    {
        public int DetailId { get; set; }
        public string Barcode { get; set; }
        public string TitleName { get; set; }
        public string Channel { get; set; }
        public int NewRenewCount { get; set; }
        public DateTime OldDueDate { get; set; }
        public DateTime NewDueDate { get; set; }
    }

    public class RenewService
    {
        private const string ConnectionString =
            "Server=127.0.0.1;Port=3306;Database=thuvien_tv03;User ID=root;Password=;CharSet=utf8mb4;";

        private static MySqlConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public async Task<List<RenewableLoanItem>> GetActiveLoansAsync(string keyword, string channel)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                throw new ArgumentException("Vui lòng nhập mã thẻ độc giả hoặc mã vạch sách.");

            var list = new List<RenewableLoanItem>();
            string key = keyword.Trim();

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT ct.IdChiTiet, ct.IdPhieuMuon, ct.IdDocGia, ct.IdDauTaiLieu,
                               dg.MaDinhDanhTruong, dg.HoTen, dg.LoaiDocGia, dg.DuocMuon,
                               b.MaVach, d.NhanDe, ct.HanTra, COALESCE(ct.SoLanGiaHan, 0) AS SoLanGiaHan,
                               (SELECT COUNT(*) FROM DatCho dc 
                                WHERE dc.IdDauTaiLieu = ct.IdDauTaiLieu 
                                  AND dc.IdDocGia <> ct.IdDocGia 
                                  AND dc.TrangThai IN ('DANG_CHO', 'SAN_SANG_NHAN')) AS SoDatCho,
                               (SELECT COALESCE(SUM(vp.SoTien), 0) FROM ViPham vp 
                                WHERE vp.IdDocGia = ct.IdDocGia AND vp.TrangThai = 'CHUA_XU_LY') AS NoPhat
                        FROM ChiTietPhieuMuon ct
                        JOIN DocGia dg ON dg.IdDocGia = ct.IdDocGia
                        JOIN BanTaiLieu b ON b.IdBanTaiLieu = ct.IdBanTaiLieu
                        JOIN DauTaiLieu d ON d.IdDauTaiLieu = ct.IdDauTaiLieu
                        WHERE ct.TrangThai = 'DANG_MUON'
                          AND (dg.MaDinhDanhTruong = @key OR b.MaVach = @key OR b.SoDangKyCaBiet = @key)
                        ORDER BY ct.HanTra ASC;";
                    cmd.Parameters.AddWithValue("@key", key);

                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        var now = DateTime.Now;
                        while (await r.ReadAsync())
                        {
                            bool canBorrow = r.GetBoolean(7);
                            DateTime dueDate = r.GetDateTime(10);
                            int renewCount = r.GetInt32(11);
                            int reservationCount = r.GetInt32(12);
                            decimal unpaidFine = r.GetDecimal(13);

                            int maxRenew = channel == "TAI_QUAY" ? 2 : 1;
                            var reasons = new List<string>();

                            if (!canBorrow) reasons.Add("Thẻ độc giả đang bị khóa");
                            if (unpaidFine > 0) reasons.Add($"Đang nợ phạt {unpaidFine:N0} đ");
                            if (dueDate < now && channel != "TAI_QUAY") reasons.Add("Đã quá hạn trả (cổng độc giả không thể tự gia hạn)");
                            if (renewCount >= maxRenew) reasons.Add($"Đã đạt tối đa {maxRenew} lần gia hạn");
                            if (reservationCount > 0) reasons.Add("Đầu sách đang có người đặt chỗ trước");

                            list.Add(new RenewableLoanItem
                            {
                                DetailId = r.GetInt32(0),
                                LoanId = r.GetInt32(1),
                                ReaderId = r.GetInt32(2),
                                TitleId = r.GetInt32(3),
                                ReaderCode = r.GetString(4),
                                ReaderName = r.GetString(5),
                                ReaderType = r.GetString(6),
                                Barcode = r.GetString(8),
                                TitleName = r.GetString(9),
                                DueDate = dueDate,
                                RenewCount = renewCount,
                                HasReservation = reservationCount > 0,
                                CanRenew = reasons.Count == 0,
                                BlockReason = reasons.Count == 0 ? "Đủ điều kiện gia hạn" : string.Join("; ", reasons)
                            });
                        }
                    }
                }
            }

            if (list.Count == 0)
                throw new KeyNotFoundException($"Không tìm thấy tài liệu đang mượn nào khớp với '{key}'.");

            return list;
        }

        public async Task<RenewResult> RenewAsync(int detailId, string channel, int extraDays = 7)
        {
            if (extraDays <= 0 || extraDays > 15)
                throw new ArgumentException("Số ngày gia hạn thêm phải từ 1 đến 15 ngày.");

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();
                using (var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        int readerId, titleId, renewCount;
                        string status, barcode, titleName;
                        bool canBorrow;
                        DateTime oldDueDate;

                        using (var lockCmd = conn.CreateCommand())
                        {
                            lockCmd.Transaction = tx;
                            lockCmd.CommandText = @"
                                SELECT ct.IdDocGia, ct.IdDauTaiLieu, ct.TrangThai, ct.HanTra,
                                       COALESCE(ct.SoLanGiaHan, 0), dg.DuocMuon, b.MaVach, d.NhanDe
                                FROM ChiTietPhieuMuon ct
                                JOIN DocGia dg ON dg.IdDocGia = ct.IdDocGia
                                JOIN BanTaiLieu b ON b.IdBanTaiLieu = ct.IdBanTaiLieu
                                JOIN DauTaiLieu d ON d.IdDauTaiLieu = ct.IdDauTaiLieu
                                WHERE ct.IdChiTiet = @id
                                FOR UPDATE;";
                            lockCmd.Parameters.AddWithValue("@id", detailId);

                            using (DbDataReader r = await lockCmd.ExecuteReaderAsync())
                            {
                                if (!await r.ReadAsync())
                                    throw new KeyNotFoundException("Không tìm thấy chi tiết phiếu mượn.");

                                readerId = r.GetInt32(0);
                                titleId = r.GetInt32(1);
                                status = r.GetString(2);
                                oldDueDate = r.GetDateTime(3);
                                renewCount = r.GetInt32(4);
                                canBorrow = r.GetBoolean(5);
                                barcode = r.GetString(6);
                                titleName = r.GetString(7);
                            }
                        }

                        if (status != "DANG_MUON")
                            throw new InvalidOperationException("Chỉ có thể gia hạn tài liệu đang ở trạng thái DANG_MUON.");

                        if (!canBorrow)
                            throw new InvalidOperationException("Thẻ độc giả đang bị khóa quyền mượn, không thể gia hạn.");

                        int maxRenew = channel == "TAI_QUAY" ? 2 : 1;
                        if (renewCount >= maxRenew)
                            throw new InvalidOperationException($"Tài liệu đã gia hạn {renewCount}/{maxRenew} lần, không thể gia hạn thêm.");

                        if (oldDueDate < DateTime.Now && channel != "TAI_QUAY")
                            throw new InvalidOperationException("Sách đã quá hạn trả, độc giả không thể tự gia hạn qua cổng trực tuyến.");

                        // Kiểm tra đặt chỗ
                        using (var resCmd = conn.CreateCommand())
                        {
                            resCmd.Transaction = tx;
                            resCmd.CommandText = @"
                                SELECT COUNT(*) FROM DatCho 
                                WHERE IdDauTaiLieu = @titleId AND IdDocGia <> @readerId 
                                  AND TrangThai IN ('DANG_CHO', 'SAN_SANG_NHAN');";
                            resCmd.Parameters.AddWithValue("@titleId", titleId);
                            resCmd.Parameters.AddWithValue("@readerId", readerId);
                            int reserveCount = Convert.ToInt32(await resCmd.ExecuteScalarAsync());
                            if (reserveCount > 0)
                                throw new InvalidOperationException("Từ chối gia hạn: Đầu sách này đang có độc giả khác đặt chỗ.");
                        }

                        // Tính hạn trả mới (lấy mốc lớn hơn giữa hạn cũ và hiện tại + extraDays, chốt 21:00)
                        DateTime baseDate = oldDueDate > DateTime.Now ? oldDueDate.Date : DateTime.Now.Date;
                        DateTime newDueDate = baseDate.AddDays(extraDays).AddHours(21);
                        int newRenewCount = renewCount + 1;

                        using (var updateCmd = conn.CreateCommand())
                        {
                            updateCmd.Transaction = tx;
                            updateCmd.CommandText = @"
                                UPDATE ChiTietPhieuMuon
                                SET HanTra = @newDue,
                                    SoLanGiaHan = @newCount
                                WHERE IdChiTiet = @id;";
                            updateCmd.Parameters.AddWithValue("@newDue", newDueDate);
                            updateCmd.Parameters.AddWithValue("@newCount", newRenewCount);
                            updateCmd.Parameters.AddWithValue("@id", detailId);
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        await tx.CommitAsync();

                        return new RenewResult
                        {
                            DetailId = detailId,
                            Barcode = barcode,
                            TitleName = titleName,
                            Channel = channel,
                            NewRenewCount = newRenewCount,
                            OldDueDate = oldDueDate,
                            NewDueDate = newDueDate
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