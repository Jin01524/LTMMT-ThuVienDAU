using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TranMyNhung_LTMMT_TV03.BC03
{
    public class DailyCirculationRow
    {
        public DateTime ReportDate { get; set; }
        public int BorrowedCount { get; set; }
        public int ReturnedCount { get; set; }
        public int ReadingRoomVisits { get; set; }
        public decimal FineAssessed { get; set; }
        public decimal FineCollected { get; set; }
    }

    public class CirculationDashboardReport
    {
        public string ViewerName { get; set; }
        public string ViewerRole { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // 1. Lưu thông mượn / trả / gia hạn
        public int TotalLoanTickets { get; set; }
        public int TotalBorrowedCopies { get; set; }
        public int TotalReturnedCopies { get; set; }
        public int TotalRenewals { get; set; }

        // 2. Quá hạn
        public int CurrentOverdueCopies { get; set; }
        public int LateReturnedInPeriod { get; set; }

        // 3. Phòng đọc
        public int ReadingRoomVisitsInPeriod { get; set; }
        public int CurrentlyInReadingRoom { get; set; }

        // 4. Đối chiếu tiền phạt với thực thu (không đếm trùng)
        public decimal TotalAssessedFinesInPeriod { get; set; }   // Tổng phạt phát sinh trong kỳ (trừ DA_HUY)
        public decimal TotalCollectedReceiptsInPeriod { get; set; } // Tổng thực thu trên PhieuThu trong kỳ
        public int ReceiptCountInPeriod { get; set; }
        public decimal TotalOutstandingUnpaidFines { get; set; }    // Tổng nợ phạt chưa thu toàn hệ thống
        public decimal CashCollectedInPeriod { get; set; }
        public decimal TransferCollectedInPeriod { get; set; }

        public List<DailyCirculationRow> DailyRows { get; set; } = new List<DailyCirculationRow>();
    }

    public class CirculationStatsService
    {
        private const string ConnectionString =
            "Server=127.0.0.1;Port=3306;Database=thuvien_tv03;User ID=root;Password=110704;CharSet=utf8mb4;";

        private static MySqlConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        private async Task<(string Name, string Role)> VerifyViewerPermissionAsync(MySqlConnection conn, int accountId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT HoTen, VaiTro, TrangThai
                    FROM TaiKhoan
                    WHERE IdTaiKhoan = @id
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", accountId);

                using (DbDataReader r = await cmd.ExecuteReaderAsync())
                {
                    if (!await r.ReadAsync())
                        throw new UnauthorizedAccessException("Tài khoản xem báo cáo không tồn tại.");

                    string name = r.GetString(0);
                    string role = r.GetString(1);
                    string status = r.GetString(2);

                    if (status != "HOAT_DONG")
                        throw new UnauthorizedAccessException("Tài khoản đang bị khóa, không thể truy cập Dashboard thống kê.");

                    if (role != "THU_THU" && role != "QUAN_LY_THU_VIEN")
                        throw new UnauthorizedAccessException($"Vai trò '{role}' không được phép xem báo cáo thống kê lưu thông & thu tiền.");

                    return (name, role);
                }
            }
        }

        public async Task<CirculationDashboardReport> GetDashboardReportAsync(DateTime fromDate, DateTime toDate, int viewerAccountId = 1)
        {
            if (toDate.Date < fromDate.Date)
                throw new ArgumentException("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");

            DateTime start = fromDate.Date;
            DateTime end = toDate.Date.AddDays(1).AddSeconds(-1);

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();

                // 1. Kiểm tra quyền xem Dashboard
                var viewer = await VerifyViewerPermissionAsync(conn, viewerAccountId);

                var report = new CirculationDashboardReport
                {
                    ViewerName = viewer.Name,
                    ViewerRole = viewer.Role,
                    FromDate = start,
                    ToDate = end
                };

                // 2. Thống kê Phiếu mượn & Lượt mượn / gia hạn trong kỳ (truy vấn độc lập, không JOIN chéo bảng khác)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COUNT(DISTINCT pm.IdPhieuMuon),
                               COUNT(ct.IdChiTiet),
                               COALESCE(SUM(COALESCE(ct.SoLanGiaHan, 0)), 0)
                        FROM PhieuMuon pm
                        JOIN ChiTietPhieuMuon ct ON ct.IdPhieuMuon = pm.IdPhieuMuon
                        WHERE pm.ThoiDiemMuon >= @start AND pm.ThoiDiemMuon <= @end;";
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);

                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        if (await r.ReadAsync())
                        {
                            report.TotalLoanTickets = r.GetInt32(0);
                            report.TotalBorrowedCopies = r.GetInt32(1);
                            report.TotalRenewals = Convert.ToInt32(r.GetDecimal(2));
                        }
                    }
                }

                // 3. Thống kê Lượt trả & Trả trễ trong kỳ
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COUNT(*),
                               COALESCE(SUM(CASE WHEN ThoiDiemTra > HanTra THEN 1 ELSE 0 END), 0)
                        FROM ChiTietPhieuMuon
                        WHERE ThoiDiemTra IS NOT NULL
                          AND ThoiDiemTra >= @start AND ThoiDiemTra <= @end;";
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);

                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        if (await r.ReadAsync())
                        {
                            report.TotalReturnedCopies = r.GetInt32(0);
                            report.LateReturnedInPeriod = Convert.ToInt32(r.GetDecimal(1));
                        }
                    }
                }

                // 4. Số cuốn đang quá hạn hiện tại
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM ChiTietPhieuMuon WHERE TrangThai = 'DANG_MUON' AND HanTra < NOW();";
                    report.CurrentOverdueCopies = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // 5. Thống kê Lượt vào/ra phòng đọc
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COUNT(*),
                               (SELECT COUNT(*) FROM LuotVaoPhongDoc WHERE ThoiDiemRa IS NULL)
                        FROM LuotVaoPhongDoc
                        WHERE ThoiDiemVao >= @start AND ThoiDiemVao <= @end;";
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);

                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        if (await r.ReadAsync())
                        {
                            report.ReadingRoomVisitsInPeriod = r.GetInt32(0);
                            report.CurrentlyInReadingRoom = r.GetInt32(1);
                        }
                    }
                }

                // 6. Đối chiếu Tiền phạt phát sinh (bảng ViPham độc lập - không JOIN PhieuThu để tránh đếm trùng)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COALESCE(SUM(CASE WHEN NgayLap >= @start AND NgayLap <= @end AND TrangThai <> 'DA_HUY' THEN SoTien ELSE 0 END), 0),
                               COALESCE(SUM(CASE WHEN TrangThai = 'CHUA_XU_LY' THEN SoTien ELSE 0 END), 0)
                        FROM ViPham;";
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);

                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        if (await r.ReadAsync())
                        {
                            report.TotalAssessedFinesInPeriod = r.GetDecimal(0);
                            report.TotalOutstandingUnpaidFines = r.GetDecimal(1);
                        }
                    }
                }

                // 7. Đối chiếu Thực thu trên PhieuThu (bảng PhieuThu độc lập - mỗi biên lai tính đúng 1 lần)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT COUNT(*),
                               COALESCE(SUM(TongTien), 0),
                               COALESCE(SUM(CASE WHEN HinhThucThu = 'TIEN_MAT' THEN TongTien ELSE 0 END), 0),
                               COALESCE(SUM(CASE WHEN HinhThucThu = 'CHUYEN_KHOAN' THEN TongTien ELSE 0 END), 0)
                        FROM PhieuThu
                        WHERE TrangThai = 'DA_THU'
                          AND ThoiDiemThu >= @start AND ThoiDiemThu <= @end;";
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);

                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        if (await r.ReadAsync())
                        {
                            report.ReceiptCountInPeriod = r.GetInt32(0);
                            report.TotalCollectedReceiptsInPeriod = r.GetDecimal(1);
                            report.CashCollectedInPeriod = r.GetDecimal(2);
                            report.TransferCollectedInPeriod = r.GetDecimal(3);
                        }
                    }
                }

                // 8. Bảng tổng hợp theo từng ngày trong khoảng lọc (gom từ các sub-query độc lập chống nhân bản)
                var dailyMap = new SortedDictionary<DateTime, DailyCirculationRow>();
                for (DateTime d = start.Date; d <= end.Date; d = d.AddDays(1))
                {
                    dailyMap[d] = new DailyCirculationRow { ReportDate = d };
                }

                // 8a. Mượn theo ngày
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT DATE(pm.ThoiDiemMuon) AS Ngay, COUNT(ct.IdChiTiet)
                        FROM PhieuMuon pm
                        JOIN ChiTietPhieuMuon ct ON ct.IdPhieuMuon = pm.IdPhieuMuon
                        WHERE pm.ThoiDiemMuon >= @start AND pm.ThoiDiemMuon <= @end
                        GROUP BY DATE(pm.ThoiDiemMuon);";
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);
                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            DateTime d = r.GetDateTime(0).Date;
                            if (dailyMap.ContainsKey(d)) dailyMap[d].BorrowedCount = r.GetInt32(1);
                        }
                    }
                }

                // 8b. Trả theo ngày
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT DATE(ThoiDiemTra) AS Ngay, COUNT(*)
                        FROM ChiTietPhieuMuon
                        WHERE ThoiDiemTra IS NOT NULL AND ThoiDiemTra >= @start AND ThoiDiemTra <= @end
                        GROUP BY DATE(ThoiDiemTra);";
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);
                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            DateTime d = r.GetDateTime(0).Date;
                            if (dailyMap.ContainsKey(d)) dailyMap[d].ReturnedCount = r.GetInt32(1);
                        }
                    }
                }

                // 8c. Lượt phòng đọc theo ngày
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT DATE(ThoiDiemVao) AS Ngay, COUNT(*)
                        FROM LuotVaoPhongDoc
                        WHERE ThoiDiemVao >= @start AND ThoiDiemVao <= @end
                        GROUP BY DATE(ThoiDiemVao);";
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);
                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            DateTime d = r.GetDateTime(0).Date;
                            if (dailyMap.ContainsKey(d)) dailyMap[d].ReadingRoomVisits = r.GetInt32(1);
                        }
                    }
                }

                // 8d. Phạt phát sinh theo ngày
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT DATE(NgayLap) AS Ngay, COALESCE(SUM(SoTien), 0)
                        FROM ViPham
                        WHERE TrangThai <> 'DA_HUY' AND NgayLap >= @start AND NgayLap <= @end
                        GROUP BY DATE(NgayLap);";
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);
                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            DateTime d = r.GetDateTime(0).Date;
                            if (dailyMap.ContainsKey(d)) dailyMap[d].FineAssessed = r.GetDecimal(1);
                        }
                    }
                }

                // 8e. Thực thu theo ngày
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT DATE(ThoiDiemThu) AS Ngay, COALESCE(SUM(TongTien), 0)
                        FROM PhieuThu
                        WHERE TrangThai = 'DA_THU' AND ThoiDiemThu >= @start AND ThoiDiemThu <= @end
                        GROUP BY DATE(ThoiDiemThu);";
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);
                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            DateTime d = r.GetDateTime(0).Date;
                            if (dailyMap.ContainsKey(d)) dailyMap[d].FineCollected = r.GetDecimal(1);
                        }
                    }
                }

                foreach (var item in dailyMap.Values)
                {
                    if (item.BorrowedCount > 0 || item.ReturnedCount > 0 || item.ReadingRoomVisits > 0 || item.FineAssessed > 0 || item.FineCollected > 0)
                        report.DailyRows.Add(item);
                }

                return report;
            }
        }
    }
}