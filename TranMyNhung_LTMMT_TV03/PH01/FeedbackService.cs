using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TranMyNhung_LTMMT_TV03.PH01
{
    public class SubmitFeedbackRequest
    {
        public string ReaderCode { get; set; }
        public string FeedbackType { get; set; } // "GOP_Y", "DANH_GIA", "DE_XUAT_SACH"
        public int? TitleId { get; set; }
        public int? Rating { get; set; } // 1..5 cho DANH_GIA
        public string Subject { get; set; }
        public string Content { get; set; }
    }

    public class FeedbackItem
    {
        public int FeedbackId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; }
        public string ReaderName { get; set; }
        public string FeedbackType { get; set; }
        public int? TitleId { get; set; }
        public string TitleName { get; set; }
        public int? Rating { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public string Status { get; set; } // "MOI_GUI", "DA_TIEP_NHAN", "DA_PHAN_HOI", "TU_CHOI"
        public string StaffReply { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public class ProcessFeedbackRequest
    {
        public int FeedbackId { get; set; }
        public int StaffAccountId { get; set; } = 1;
        public string NewStatus { get; set; } // "DA_TIEP_NHAN", "DA_PHAN_HOI", "TU_CHOI"
        public string StaffReply { get; set; }
    }

    public class FeedbackService
    {
        private const string ConnectionString =
            "Server=127.0.0.1;Port=3306;Database=thuvien_tv03;User ID=root;Password=110704;CharSet=utf8mb4;";

        private static MySqlConnection CreateConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        private async Task<string> VerifyStaffPermissionAsync(MySqlConnection conn, MySqlTransaction tx, int accountId)
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
                        throw new UnauthorizedAccessException("Tài khoản xử lý không tồn tại.");

                    string name = r.GetString(0);
                    string role = r.GetString(1);
                    string status = r.GetString(2);

                    if (status != "HOAT_DONG")
                        throw new UnauthorizedAccessException("Tài khoản đang bị khóa, không có quyền xử lý phản hồi.");

                    if (role != "THU_THU" && role != "QUAN_LY_THU_VIEN")
                        throw new UnauthorizedAccessException($"Vai trò '{role}' không có quyền duyệt/xử lý phản hồi độc giả.");

                    return name;
                }
            }
        }

        public async Task<int> SubmitFeedbackAsync(SubmitFeedbackRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.ReaderCode))
                throw new ArgumentException("Vui lòng nhập mã thẻ độc giả.");
            if (string.IsNullOrWhiteSpace(req.Subject))
                throw new ArgumentException("Vui lòng nhập tiêu đề hoặc tên sách đề xuất.");
            if (string.IsNullOrWhiteSpace(req.Content))
                throw new ArgumentException("Vui lòng nhập nội dung góp ý / đánh giá / lý do đề xuất.");

            string type = string.IsNullOrWhiteSpace(req.FeedbackType) ? "GOP_Y" : req.FeedbackType;
            if (type != "GOP_Y" && type != "DANH_GIA" && type != "DE_XUAT_SACH")
                throw new ArgumentException("Loại phản hồi không hợp lệ.");

            if (type == "DANH_GIA" && (!req.Rating.HasValue || req.Rating.Value < 1 || req.Rating.Value > 5))
                throw new ArgumentException("Đánh giá tài liệu phải chọn số sao từ 1 đến 5.");

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();
                using (var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        int readerId;
                        string profileStatus;

                        using (var readerCmd = conn.CreateCommand())
                        {
                            readerCmd.Transaction = tx;
                            readerCmd.CommandText = "SELECT IdDocGia, TrangThaiHoSo FROM DocGia WHERE MaDinhDanhTruong = @code LIMIT 1;";
                            readerCmd.Parameters.AddWithValue("@code", req.ReaderCode.Trim());

                            using (DbDataReader r = await readerCmd.ExecuteReaderAsync())
                            {
                                if (!await r.ReadAsync())
                                    throw new KeyNotFoundException($"Không tìm thấy độc giả với mã thẻ '{req.ReaderCode.Trim()}'.");

                                readerId = r.GetInt32(0);
                                profileStatus = r.GetString(1);
                            }
                        }

                        if (profileStatus != "HOAT_DONG")
                            throw new InvalidOperationException("Hồ sơ độc giả hiện không hoạt động, không thể gửi phản hồi.");

                        if (req.TitleId.HasValue)
                        {
                            using (var titleCmd = conn.CreateCommand())
                            {
                                titleCmd.Transaction = tx;
                                titleCmd.CommandText = "SELECT COUNT(*) FROM DauTaiLieu WHERE IdDauTaiLieu = @tid;";
                                titleCmd.Parameters.AddWithValue("@tid", req.TitleId.Value);
                                int exists = Convert.ToInt32(await titleCmd.ExecuteScalarAsync());
                                if (exists == 0)
                                    throw new KeyNotFoundException($"Không tìm thấy đầu tài liệu ID={req.TitleId.Value}.");
                            }
                        }

                        // Đề xuất sách chỉ lưu vào PhanHoiDocGia để bổ sung kho, KHÔNG tạo đặt chỗ/giữ sách
                        string finalContent = req.Content.Trim();
                        if (type == "DE_XUAT_SACH")
                            finalContent += " [Lưu ý: Đề xuất bổ sung kho sách, không phải yêu cầu đặt trước tài liệu]";

                        int feedbackId;
                        using (var insertCmd = conn.CreateCommand())
                        {
                            insertCmd.Transaction = tx;
                            insertCmd.CommandText = @"
                                INSERT INTO PhanHoiDocGia (IdDocGia, LoaiPhanHoi, IdDauTaiLieu, DiemDanhGia, TieuDe, NoiDung, TrangThai, ThoiDiemGui)
                                VALUES (@docGia, @loai, @dauTaiLieu, @diem, @tieuDe, @noiDung, 'MOI_GUI', NOW());
                                SELECT LAST_INSERT_ID();";
                            insertCmd.Parameters.AddWithValue("@docGia", readerId);
                            insertCmd.Parameters.AddWithValue("@loai", type);
                            insertCmd.Parameters.AddWithValue("@dauTaiLieu", req.TitleId.HasValue ? (object)req.TitleId.Value : DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@diem", type == "DANH_GIA" ? (object)req.Rating.Value : DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@tieuDe", req.Subject.Trim());
                            insertCmd.Parameters.AddWithValue("@noiDung", finalContent);
                            feedbackId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                        }

                        tx.Commit();
                        return feedbackId;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<List<FeedbackItem>> SearchFeedbacksAsync(string keyword = "", string typeFilter = "TAT_CA", string statusFilter = "TAT_CA")
        {
            var list = new List<FeedbackItem>();
            string kw = (keyword ?? "").Trim();

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT ph.IdPhanHoi, ph.IdDocGia, dg.MaDinhDanhTruong, dg.HoTen,
                               ph.LoaiPhanHoi, ph.IdDauTaiLieu, COALESCE(d.NhanDe, '—'),
                               ph.DiemDanhGia, ph.TieuDe, ph.NoiDung, ph.TrangThai,
                               COALESCE(ph.NoiDungPhanHoi, ''), ph.ThoiDiemGui, ph.ThoiDiemXuLy
                        FROM PhanHoiDocGia ph
                        JOIN DocGia dg ON dg.IdDocGia = ph.IdDocGia
                        LEFT JOIN DauTaiLieu d ON d.IdDauTaiLieu = ph.IdDauTaiLieu
                        WHERE (@type = 'TAT_CA' OR ph.LoaiPhanHoi = @type)
                          AND (@st = 'TAT_CA' OR ph.TrangThai = @st)
                          AND (@kw = '' OR dg.MaDinhDanhTruong LIKE @likeKw OR dg.HoTen LIKE @likeKw OR ph.TieuDe LIKE @likeKw OR ph.NoiDung LIKE @likeKw)
                        ORDER BY (ph.TrangThai = 'MOI_GUI') DESC, ph.ThoiDiemGui DESC
                        LIMIT 200;";
                    cmd.Parameters.AddWithValue("@type", typeFilter ?? "TAT_CA");
                    cmd.Parameters.AddWithValue("@st", statusFilter ?? "TAT_CA");
                    cmd.Parameters.AddWithValue("@kw", kw);
                    cmd.Parameters.AddWithValue("@likeKw", "%" + kw + "%");

                    using (DbDataReader r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            list.Add(new FeedbackItem
                            {
                                FeedbackId = r.GetInt32(0),
                                ReaderId = r.GetInt32(1),
                                ReaderCode = r.GetString(2),
                                ReaderName = r.GetString(3),
                                FeedbackType = r.GetString(4),
                                TitleId = r.IsDBNull(5) ? (int?)null : r.GetInt32(5),
                                TitleName = r.GetString(6),
                                Rating = r.IsDBNull(7) ? (int?)null : r.GetInt32(7),
                                Subject = r.GetString(8),
                                Content = r.GetString(9),
                                Status = r.GetString(10),
                                StaffReply = r.GetString(11),
                                CreatedAt = r.GetDateTime(12),
                                ProcessedAt = r.IsDBNull(13) ? (DateTime?)null : r.GetDateTime(13)
                            });
                        }
                    }
                }
            }

            return list;
        }

        public async Task<string> ProcessFeedbackAsync(ProcessFeedbackRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.StaffReply))
                throw new ArgumentException("Vui lòng nhập nội dung phản hồi / ghi chú xử lý cho độc giả.");

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();
                using (var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        string staffName = await VerifyStaffPermissionAsync(conn, tx, req.StaffAccountId);

                        using (var lockCmd = conn.CreateCommand())
                        {
                            lockCmd.Transaction = tx;
                            lockCmd.CommandText = "SELECT IdPhanHoi FROM PhanHoiDocGia WHERE IdPhanHoi = @id FOR UPDATE;";
                            lockCmd.Parameters.AddWithValue("@id", req.FeedbackId);
                            object found = await lockCmd.ExecuteScalarAsync();
                            if (found == null)
                                throw new KeyNotFoundException($"Không tìm thấy phản hồi #{req.FeedbackId}.");
                        }

                        using (var updateCmd = conn.CreateCommand())
                        {
                            updateCmd.Transaction = tx;
                            updateCmd.CommandText = @"
                                UPDATE PhanHoiDocGia
                                SET TrangThai = @st,
                                    NoiDungPhanHoi = @reply,
                                    IdNguoiXuLy = @staffId,
                                    ThoiDiemXuLy = NOW()
                                WHERE IdPhanHoi = @id;";
                            updateCmd.Parameters.AddWithValue("@st", req.NewStatus);
                            updateCmd.Parameters.AddWithValue("@reply", $"[{staffName}] {req.StaffReply.Trim()}");
                            updateCmd.Parameters.AddWithValue("@staffId", req.StaffAccountId);
                            updateCmd.Parameters.AddWithValue("@id", req.FeedbackId);
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        tx.Commit();
                        return staffName;
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