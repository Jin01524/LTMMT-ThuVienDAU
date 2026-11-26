using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TranMyNhung_LTMMT_TV03.LT04
{
    public class LoanHistoryFilter
    {
        public string Keyword { get; set; }
        public string StatusFilter { get; set; } // "TAT_CA", "DANG_MUON", "QUA_HAN", "DA_TRA", "HU_HONG", "MAT"
        public bool UseDateRange { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class LoanHistoryRecord
    {
        public int DetailId { get; set; }
        public int LoanId { get; set; }
        public string LoanStatus { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string ReaderType { get; set; }
        public string Barcode { get; set; }
        public string RegistrationNumber { get; set; }
        public string TitleName { get; set; }
        public DateTime BorrowedAt { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public int RenewCount { get; set; }
        public string DetailStatus { get; set; }
        public string ReturnCondition { get; set; }
        public string Note { get; set; }
        public bool IsOverdue { get; set; }
        public int OverdueDays { get; set; }
    }

    public class LoanHistoryService
    {
        private const string ConnectionString =
            "Server=127.0.0.1;Port=3306;Database=thuvien_tv03;User ID=root;Password=110704;CharSet=utf8mb4;";

        private static MySqlConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public async Task<List<LoanHistoryRecord>> SearchHistoryAsync(LoanHistoryFilter filter)
        {
            var list = new List<LoanHistoryRecord>();

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();

                var sql = new StringBuilder(@"
                    SELECT ct.IdChiTiet, ct.IdPhieuMuon, pm.TrangThai AS TrangThaiPhieu,
                           dg.MaDinhDanhTruong, dg.HoTen, dg.LoaiDocGia,
                           b.MaVach, b.SoDangKyCaBiet, d.NhanDe,
                           pm.ThoiDiemMuon, ct.HanTra, ct.ThoiDiemTra,
                           COALESCE(ct.SoLanGiaHan, 0), ct.TrangThai,
                           ct.TinhTrangKhiTra, ct.GhiChuTra
                    FROM ChiTietPhieuMuon ct
                    JOIN PhieuMuon pm ON pm.IdPhieuMuon = ct.IdPhieuMuon
                    JOIN DocGia dg ON dg.IdDocGia = ct.IdDocGia
                    JOIN BanTaiLieu b ON b.IdBanTaiLieu = ct.IdBanTaiLieu
                    JOIN DauTaiLieu d ON d.IdDauTaiLieu = ct.IdDauTaiLieu
                    WHERE 1 = 1 ");

                using (var cmd = conn.CreateCommand())
                {
                    if (!string.IsNullOrWhiteSpace(filter.Keyword))
                    {
                        sql.Append(@" AND (dg.MaDinhDanhTruong LIKE @kw 
                                        OR dg.HoTen LIKE @kw 
                                        OR b.MaVach LIKE @kw 
                                        OR b.SoDangKyCaBiet LIKE @kw 
                                        OR d.NhanDe LIKE @kw
                                        OR CAST(ct.IdPhieuMuon AS CHAR) = @exactKw) ");
                        cmd.Parameters.AddWithValue("@kw", "%" + filter.Keyword.Trim() + "%");
                        cmd.Parameters.AddWithValue("@exactKw", filter.Keyword.Trim());
                    }

                    if (!string.IsNullOrWhiteSpace(filter.StatusFilter) && filter.StatusFilter != "TAT_CA")
                    {
                        if (filter.StatusFilter == "QUA_HAN")
                        {
                            sql.Append(" AND ct.TrangThai = 'DANG_MUON' AND ct.HanTra < NOW() ");
                        }
                        else
                        {
                            sql.Append(" AND ct.TrangThai = @st ");
                            cmd.Parameters.AddWithValue("@st", filter.StatusFilter);
                        }
                    }

                    if (filter.UseDateRange)
                    {
                        sql.Append(" AND pm.ThoiDiemMuon >= @fromDate AND pm.ThoiDiemMuon <= @toDate ");
                        cmd.Parameters.AddWithValue("@fromDate", filter.FromDate.Date);
                        cmd.Parameters.AddWithValue("@toDate", filter.ToDate.Date.AddDays(1).AddSeconds(-1));
                    }

                    sql.Append(" ORDER BY pm.ThoiDiemMuon DESC, ct.IdChiTiet DESC LIMIT 300;");
                    cmd.CommandText = sql.ToString();

                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        var now = DateTime.Now;
                        while (await r.ReadAsync())
                        {
                            DateTime dueDate = r.GetDateTime(10);
                            DateTime? returnedAt = r.IsDBNull(11) ? (DateTime?)null : r.GetDateTime(11);
                            string status = r.GetString(13);

                            bool isOverdue = status == "DANG_MUON" && now > dueDate;
                            int overdueDays = 0;
                            if (isOverdue)
                            {
                                overdueDays = Math.Max(1, (int)Math.Ceiling((now - dueDate).TotalDays));
                            }
                            else if (returnedAt.HasValue && returnedAt.Value > dueDate)
                            {
                                overdueDays = Math.Max(1, (int)Math.Ceiling((returnedAt.Value - dueDate).TotalDays));
                            }

                            list.Add(new LoanHistoryRecord
                            {
                                DetailId = r.GetInt32(0),
                                LoanId = r.GetInt32(1),
                                LoanStatus = r.GetString(2),
                                ReaderCode = r.GetString(3),
                                ReaderName = r.GetString(4),
                                ReaderType = r.GetString(5),
                                Barcode = r.GetString(6),
                                RegistrationNumber = r.GetString(7),
                                TitleName = r.GetString(8),
                                BorrowedAt = r.GetDateTime(9),
                                DueDate = dueDate,
                                ReturnedAt = returnedAt,
                                RenewCount = r.GetInt32(12),
                                DetailStatus = status,
                                ReturnCondition = r.IsDBNull(14) ? "" : r.GetString(14),
                                Note = r.IsDBNull(15) ? "" : r.GetString(15),
                                IsOverdue = isOverdue,
                                OverdueDays = overdueDays
                            });
                        }
                    }
                }
            }

            return list;
        }
    }
}