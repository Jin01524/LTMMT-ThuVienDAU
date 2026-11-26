using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TranMyNhung_LTMMT_TV03.VP01
{
    public class ViolationCandidate
    {
        public int DetailId { get; set; }
        public int LoanId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ReaderType { get; set; }
        public string Barcode { get; set; }
        public string TitleName { get; set; }
        public decimal BookPrice { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public string DetailStatus { get; set; }
        public int RawOverdueDays { get; set; }
        public int HolidayDaysExcluded { get; set; }
        public int ChargeableDays { get; set; }
        public bool IsCalendarFinalized { get; set; }
        public string ViolationType { get; set; } // "QUA_HAN", "THONG_BAO_DON_VI", "HU_HONG", "MAT"
        public decimal SuggestedAmount { get; set; }
        public string CalculationNote { get; set; }
    }

    public class CreateViolationInput
    {
        public int DetailId { get; set; }
        public string ViolationType { get; set; }
        public int ChargeableDays { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public bool ConfirmUnfinalizedCalendar { get; set; }
    }

    public class ViolationRecordResult
    {
        public int ViolationId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ViolationType { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
    }

    public class ViolationService
    {
        private const string ConnectionString =
            "Server=127.0.0.1;Port=3306;Database=thuvien_tv03;User ID=root;Password=110704;CharSet=utf8mb4;";

        private static MySqlConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        private async Task<(HashSet<DateTime> Holidays, bool IsFinalized)> LoadHolidaysAsync(MySqlConnection conn)
        {
            var holidays = new HashSet<DateTime>();
            bool isFinalized = false;

            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT NgayNghi, COALESCE(DaChot, 1) FROM NgayNghiLe;";
                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            holidays.Add(r.GetDateTime(0).Date);
                            if (r.GetBoolean(1)) isFinalized = true;
                        }
                    }
                }
            }
            catch
            {
                // Nếu bảng NgayNghiLe chưa được tạo từ HT-03, coi như chưa chốt lịch nghỉ lễ
                isFinalized = false;
            }

            return (holidays, isFinalized);
        }

        private static (int RawDays, int ExcludedHolidays, int NetDays) CalculateOverdueDays(
            DateTime dueDate, DateTime endDate, HashSet<DateTime> holidays)
        {
            if (endDate <= dueDate) return (0, 0, 0);

            int rawDays = Math.Max(1, (int)Math.Ceiling((endDate - dueDate).TotalDays));
            int excluded = 0;

            for (int i = 1; i <= rawDays; i++)
            {
                DateTime checkDay = dueDate.Date.AddDays(i);
                if (holidays.Contains(checkDay))
                    excluded++;
            }

            int netDays = Math.Max(0, rawDays - excluded);
            return (rawDays, excluded, netDays);
        }

        public async Task<List<ViolationCandidate>> GetPendingCandidatesAsync(string keyword = "")
        {
            var list = new List<ViolationCandidate>();

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();
                var holidayInfo = await LoadHolidaysAsync(conn);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT ct.IdChiTiet, ct.IdPhieuMuon, ct.IdDocGia,
                               dg.MaDinhDanhTruong, dg.HoTen, dg.LoaiDocGia,
                               b.MaVach, d.NhanDe, COALESCE(d.GiaBia, 100000) AS GiaBia,
                               ct.HanTra, ct.ThoiDiemTra, ct.TrangThai
                        FROM ChiTietPhieuMuon ct
                        JOIN DocGia dg ON dg.IdDocGia = ct.IdDocGia
                        JOIN BanTaiLieu b ON b.IdBanTaiLieu = ct.IdBanTaiLieu
                        JOIN DauTaiLieu d ON d.IdDauTaiLieu = ct.IdDauTaiLieu
                        WHERE NOT EXISTS (
                            SELECT 1 FROM ViPham vp WHERE vp.IdChiTietPhieuMuon = ct.IdChiTiet
                        )
                        AND (
                            ct.TrangThai IN ('HU_HONG', 'MAT')
                            OR (ct.TrangThai = 'DANG_MUON' AND ct.HanTra < NOW())
                            OR (ct.TrangThai = 'DA_TRA' AND ct.ThoiDiemTra > ct.HanTra)
                        )
                        AND (@kw = '' OR dg.MaDinhDanhTruong LIKE @likeKw OR dg.HoTen LIKE @likeKw OR b.MaVach LIKE @likeKw)
                        ORDER BY ct.HanTra ASC;";

                    string kw = (keyword ?? "").Trim();
                    cmd.Parameters.AddWithValue("@kw", kw);
                    cmd.Parameters.AddWithValue("@likeKw", "%" + kw + "%");

                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        var now = DateTime.Now;
                        while (await r.ReadAsync())
                        {
                            string readerType = r.GetString(5);
                            decimal bookPrice = r.GetDecimal(8);
                            DateTime dueDate = r.GetDateTime(9);
                            DateTime? returnedAt = r.IsDBNull(10) ? (DateTime?)null : r.GetDateTime(10);
                            string status = r.GetString(11);

                            DateTime comparePoint = returnedAt ?? now;
                            var days = CalculateOverdueDays(dueDate, comparePoint, holidayInfo.Holidays);

                            string vType;
                            decimal amount = 0;
                            string note;

                            if (status == "MAT")
                            {
                                vType = "MAT";
                                amount = bookPrice;
                                note = $"Mất sách: Bồi thường 100% giá bìa ({bookPrice:N0} đ).";
                            }
                            else if (status == "HU_HONG")
                            {
                                vType = "HU_HONG";
                                amount = Math.Round(bookPrice * 0.5m, 0);
                                note = $"Hư hỏng sách: Bồi thường 50% giá bìa ({amount:N0} đ), có thể điều chỉnh thực tế.";
                            }
                            else if (readerType != "SINH_VIEN")
                            {
                                vType = "THONG_BAO_DON_VI";
                                amount = 0;
                                note = $"CB/GV trễ {days.NetDays} ngày: Lập thông báo gửi đơn vị công tác (không thu tiền mặt).";
                            }
                            else
                            {
                                vType = "QUA_HAN";
                                if (!holidayInfo.IsFinalized)
                                {
                                    amount = 0;
                                    note = $"CẢNH BÁO: Chưa chốt lịch nghỉ lễ/Tết (HT-03). Trễ thô {days.RawDays} ngày — không tự tính đủ ngày!";
                                }
                                else
                                {
                                    amount = days.NetDays * 2000m;
                                    note = $"SV quá hạn: {days.NetDays} ngày (trừ {days.ExcludedHolidays} ngày lễ) × 2.000đ = {amount:N0} đ.";
                                }
                            }

                            list.Add(new ViolationCandidate
                            {
                                DetailId = r.GetInt32(0),
                                LoanId = r.GetInt32(1),
                                ReaderId = r.GetInt32(2),
                                ReaderCode = r.GetString(3),
                                ReaderName = r.GetString(4),
                                ReaderType = readerType,
                                Barcode = r.GetString(6),
                                TitleName = r.GetString(7),
                                BookPrice = bookPrice,
                                DueDate = dueDate,
                                ReturnedAt = returnedAt,
                                DetailStatus = status,
                                RawOverdueDays = days.RawDays,
                                HolidayDaysExcluded = days.ExcludedHolidays,
                                ChargeableDays = days.NetDays,
                                IsCalendarFinalized = holidayInfo.IsFinalized,
                                ViolationType = vType,
                                SuggestedAmount = amount,
                                CalculationNote = note
                            });
                        }
                    }
                }
            }

            return list;
        }

        public async Task<ViolationRecordResult> CreateViolationAsync(CreateViolationInput input)
        {
            if (input.ViolationType == "QUA_HAN" && input.Amount <= 0 && !input.ConfirmUnfinalizedCalendar)
                throw new InvalidOperationException("Chưa chốt lịch nghỉ lễ/Tết hoặc số tiền phạt bằng 0. Vui lòng kiểm tra lại số ngày tính phạt hoặc xác nhận thủ công.");

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();
                using (var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        int readerId;
                        string readerCode, readerName;

                        using (var checkCmd = conn.CreateCommand())
                        {
                            checkCmd.Transaction = tx;
                            checkCmd.CommandText = @"
                                SELECT ct.IdDocGia, dg.MaDinhDanhTruong, dg.HoTen,
                                       (SELECT COUNT(*) FROM ViPham vp WHERE vp.IdChiTietPhieuMuon = ct.IdChiTiet) AS DaLap
                                FROM ChiTietPhieuMuon ct
                                JOIN DocGia dg ON dg.IdDocGia = ct.IdDocGia
                                WHERE ct.IdChiTiet = @detailId
                                FOR UPDATE;";
                            checkCmd.Parameters.AddWithValue("@detailId", input.DetailId);

                            using (DbDataReader r = await checkCmd.ExecuteReaderAsync())
                            {
                                if (!await r.ReadAsync())
                                    throw new KeyNotFoundException("Không tìm thấy khoản mượn cần lập vi phạm.");

                                readerId = r.GetInt32(0);
                                readerCode = r.GetString(1);
                                readerName = r.GetString(2);
                                int alreadyExists = r.GetInt32(3);

                                if (alreadyExists > 0)
                                    throw new InvalidOperationException("Khoản mượn này đã được lập biên bản vi phạm trước đó.");
                            }
                        }

                        string status = input.ViolationType == "THONG_BAO_DON_VI" ? "DA_GUI_THONG_BAO" : "CHUA_XU_LY";
                        int violationId;

                        using (var insertCmd = conn.CreateCommand())
                        {
                            insertCmd.Transaction = tx;
                            insertCmd.CommandText = @"
                                INSERT INTO ViPham (IdDocGia, IdChiTietPhieuMuon, LoaiViPham, SoNgayTre, SoTien, MoTa, TrangThai, NgayLap)
                                VALUES (@docGia, @chiTiet, @loai, @soNgay, @soTien, @moTa, @trangThai, NOW());
                                SELECT LAST_INSERT_ID();";
                            insertCmd.Parameters.AddWithValue("@docGia", readerId);
                            insertCmd.Parameters.AddWithValue("@chiTiet", input.DetailId);
                            insertCmd.Parameters.AddWithValue("@loai", input.ViolationType);
                            insertCmd.Parameters.AddWithValue("@soNgay", input.ChargeableDays);
                            insertCmd.Parameters.AddWithValue("@soTien", input.Amount);
                            insertCmd.Parameters.AddWithValue("@moTa", input.Description ?? "");
                            insertCmd.Parameters.AddWithValue("@trangThai", status);
                            violationId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                        }

                        // Nếu phát sinh phạt tiền / bồi thường chưa thanh toán -> khóa quyền mượn của độc giả
                        if (input.Amount > 0)
                        {
                            using (var lockReaderCmd = conn.CreateCommand())
                            {
                                lockReaderCmd.Transaction = tx;
                                lockReaderCmd.CommandText = @"
                                    UPDATE DocGia
                                    SET DuocMuon = 0,
                                        LyDoKhoa = @lyDo
                                    WHERE IdDocGia = @docGia;";
                                lockReaderCmd.Parameters.AddWithValue("@lyDo", $"Vi phạm #{violationId}: {input.ViolationType} ({input.Amount:N0} đ)");
                                lockReaderCmd.Parameters.AddWithValue("@docGia", readerId);
                                await lockReaderCmd.ExecuteNonQueryAsync();
                            }
                        }

                        await tx.CommitAsync();

                        return new ViolationRecordResult
                        {
                            ViolationId = violationId,
                            ReaderCode = readerCode,
                            ReaderName = readerName,
                            ViolationType = input.ViolationType,
                            Amount = input.Amount,
                            Status = status,
                            Description = input.Description
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