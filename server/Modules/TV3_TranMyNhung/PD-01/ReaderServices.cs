using ThuVienDAU.Catalog;
using ThuVienDAU.Circulation.PD01;
using ThuVienDAU.Circulation.PH01;

namespace ThuVienDAU.Circulation;

public sealed partial class CirculationService
{
    private async Task<List<ReadingRoomVisitItem>> Visits(CatalogStore s, CirculationQuery q, CancellationToken ct)
        => Map<ReadingRoomVisitItem>(await s.Rows("""
            SELECT l.IdLuotDoc VisitId,l.IdDocGia ReaderId,d.MaDinhDanhTruong ReaderCode,d.HoTen ReaderName,d.LoaiDocGia ReaderType,
            l.ThoiDiemVao CheckInTime,l.ThoiDiemRa CheckOutTime,
            COALESCE((SELECT JSON_UNQUOTE(n.ChiTiet->'$.after.lockerKey') FROM NhatKy n WHERE n.BangDuLieu='LuotVaoPhongDoc' AND n.KhoaBanGhi=CAST(l.IdLuotDoc AS CHAR) AND n.HanhDong='VAO_PHONG_DOC' ORDER BY n.IdNhatKy DESC LIMIT 1),'') LockerKey,
            COALESCE((SELECT JSON_UNQUOTE(n.ChiTiet->'$.after.note') FROM NhatKy n WHERE n.BangDuLieu='LuotVaoPhongDoc' AND n.KhoaBanGhi=CAST(l.IdLuotDoc AS CHAR) AND n.HanhDong='VAO_PHONG_DOC' ORDER BY n.IdNhatKy DESC LIMIT 1),'') Note
            FROM LuotVaoPhongDoc l JOIN DocGia d USING(IdDocGia)
            WHERE (l.ThoiDiemVao>=@from AND l.ThoiDiemVao<@to OR l.ThoiDiemRa IS NULL)
            AND (@inside=0 OR l.ThoiDiemRa IS NULL) AND (d.MaDinhDanhTruong LIKE @term ESCAPE '!' OR d.HoTen LIKE @term ESCAPE '!') ORDER BY l.IdLuotDoc DESC LIMIT 100
            """, ct, ("from", Now.Date), ("to", Now.Date.AddDays(1)), ("inside", q.Flag), ("term", Term(q.Keyword))));
    private async Task<CheckInOutResult> ScanVisit(CatalogStore s, VisitCommand q, int actor, CancellationToken ct)
    {
        if (!q.ConfirmedPhysicalHandover) throw new ArgumentException("Cần xác nhận bàn giao thẻ trường và chìa khóa tại quầy.");
        Need(q.ReaderCode, 50, "Mã trường");
        if (q.LockerKey?.Length > 30 || q.Note?.Length > 500) throw new ArgumentException("Mã chìa khóa/ghi chú quá dài.");
        var readers = await s.Rows("SELECT IdDocGia FROM DocGia WHERE MaDinhDanhTruong=@code", ct, ("code", q.ReaderCode.Trim()));
        if (readers.Count != 1) throw new InvalidOperationException("Không tìm thấy độc giả.");
        var reader = await Reader(s, Id(readers[0], "IdDocGia"), ct);
        var open = await s.Rows("SELECT l.IdLuotDoc,COALESCE((SELECT JSON_UNQUOTE(n.ChiTiet->'$.after.lockerKey') FROM NhatKy n WHERE n.BangDuLieu='LuotVaoPhongDoc' AND n.KhoaBanGhi=CAST(l.IdLuotDoc AS CHAR) AND n.HanhDong='VAO_PHONG_DOC' ORDER BY n.IdNhatKy DESC LIMIT 1),'') LockerKey FROM LuotVaoPhongDoc l WHERE l.IdDocGia=@id AND l.ThoiDiemRa IS NULL FOR UPDATE", ct, ("id", Id(reader, "IdDocGia")));
        int id; string action; string key = q.LockerKey?.Trim() ?? ""; DateTime now = Now;
        if (open.Count > 0)
        {
            if (open.Count != 1) throw new InvalidOperationException("Có nhiều lượt chưa đóng; cần đối chiếu nhật ký trước khi xử lý.");
            string assigned = open[0]["LockerKey"]!.ToString()!;
            if (key.Length > 0 && key != assigned) throw new ArgumentException("Mã chìa khóa trả không khớp lượt vào phòng đọc.");
            key = assigned;
            id = Id(open[0], "IdLuotDoc"); action = "CHECK_OUT";
            await s.Execute("UPDATE LuotVaoPhongDoc SET ThoiDiemRa=@now,DaTraThe=1 WHERE IdLuotDoc=@id", ct, ("now", now), ("id", id));
            await s.Audit(actor, "RA_PHONG_DOC", "LuotVaoPhongDoc", id.ToString(), null, new { DaTraThe = true, q.Note }, ct);
        }
        else
        {
            Rules.RequireOpen(now, false);
            if (reader["TrangThaiHoSo"]?.ToString() != "HOAT_DONG") throw new InvalidOperationException("Hồ sơ độc giả không hoạt động.");
            if (key.Length > 0)
            {
                var used = await s.Rows("""
                    SELECT l.IdLuotDoc FROM LuotVaoPhongDoc l JOIN NhatKy n ON n.BangDuLieu='LuotVaoPhongDoc' AND n.KhoaBanGhi=CAST(l.IdLuotDoc AS CHAR) AND n.HanhDong='VAO_PHONG_DOC'
                    WHERE l.ThoiDiemRa IS NULL AND JSON_UNQUOTE(n.ChiTiet->'$.after.lockerKey')=@key LIMIT 1
                    """, ct, ("key", key));
                if (used.Count > 0) throw new InvalidOperationException("Chìa khóa này đang giao cho độc giả khác.");
            }
            id = await s.Execute("INSERT INTO LuotVaoPhongDoc(IdDocGia,ThoiDiemVao,IdThuThu) VALUES(@reader,@now,@actor)", ct, ("reader", Id(reader, "IdDocGia")), ("now", now), ("actor", actor));
            await s.Audit(actor, "VAO_PHONG_DOC", "LuotVaoPhongDoc", id.ToString(), null, new { LockerKey = key, q.Note }, ct); action = "CHECK_IN";
        }
        return new() { VisitId = id, ActionType = action, ReaderCode = reader["MaDinhDanhTruong"]!.ToString(), ReaderName = reader["HoTen"]!.ToString(), ReaderType = reader["LoaiDocGia"]!.ToString(), Timestamp = now, LockerKey = key,
            ReminderMessage = action == "CHECK_IN" ? "Giữ thẻ trường tại quầy; giao chìa khóa tủ nếu có. Bạn đọc lấy tối đa 02 cuốn/lần và tự xếp lại đúng vị trí." : "Xác nhận nhận lại chìa khóa tủ nếu có và trả thẻ trường cho độc giả trước khi hoàn tất." };
    }
    private async Task<int> SubmitFeedback(CatalogStore s, SubmitFeedbackRequest q, int actor, object? ownReader, CancellationToken ct)
    {
        if(ownReader is not null) {
            var identity=await Reader(s,Convert.ToInt32(ownReader),ct);
            q.ReaderCode=identity["MaDinhDanhTruong"]!.ToString()!;
        }
        Need(q.ReaderCode, 50, "Mã trường"); Need(q.Subject, 300, "Tiêu đề"); Need(q.Content, 10000, "Nội dung");
        if (q.FeedbackType is not ("DE_XUAT" or "GOP_Y" or "DANH_GIA") || q.Rating is < 1 or > 5 || q.FeedbackType == "DANH_GIA" && (q.Rating is null || q.TitleId is null)) throw new ArgumentException("Loại phản hồi, đầu tài liệu hoặc điểm đánh giá không hợp lệ.");
        var refs = await s.Rows("SELECT IdDocGia FROM DocGia WHERE MaDinhDanhTruong=@code", ct, ("code", q.ReaderCode.Trim()));
        if (refs.Count != 1) throw new InvalidOperationException("Không tìm thấy độc giả.");
        int readerId = Id(refs[0], "IdDocGia");
        if (ownReader is not null && Convert.ToInt32(ownReader) != readerId) throw new UnauthorizedAccessException("Độc giả chỉ được gửi phản hồi của chính mình.");
        var reader = await Reader(s, readerId, ct);
        if (reader["TrangThaiHoSo"]?.ToString() != "HOAT_DONG") throw new InvalidOperationException("Hồ sơ độc giả không hoạt động.");
        int id = await s.Execute("INSERT INTO PhanHoiDocGia(IdDocGia,LoaiPhanHoi,IdDauTaiLieu,TieuDe,NoiDung,Diem,ThoiDiemGui) VALUES(@reader,@type,@title,@subject,@content,@rating,@now)", ct, ("reader", readerId), ("type", q.FeedbackType), ("title", q.TitleId), ("subject", q.Subject.Trim()), ("content", q.Content.Trim()), ("rating", q.FeedbackType == "DANH_GIA" ? q.Rating : null), ("now", Now));
        await s.Audit(actor, "GUI_PHAN_HOI", "PhanHoiDocGia", id.ToString(), null, new { q.FeedbackType, ReaderId = readerId }, ct); return id;
    }
    private static async Task<List<FeedbackItem>> Feedbacks(CatalogStore s, CirculationQuery q, object? ownReader, CancellationToken ct)
        => Map<FeedbackItem>(await s.Rows("""
            SELECT p.IdPhanHoi FeedbackId,p.IdDocGia ReaderId,d.MaDinhDanhTruong ReaderCode,d.HoTen ReaderName,p.LoaiPhanHoi FeedbackType,p.IdDauTaiLieu TitleId,t.NhanDe TitleName,
            p.Diem Rating,p.TieuDe Subject,p.NoiDung Content,p.TrangThai Status,p.ThoiDiemGui CreatedAt,
            COALESCE((SELECT JSON_UNQUOTE(n.ChiTiet->'$.after.staffReply') FROM NhatKy n WHERE n.BangDuLieu='PhanHoiDocGia' AND n.KhoaBanGhi=CAST(p.IdPhanHoi AS CHAR) AND n.HanhDong='XU_LY_PHAN_HOI' ORDER BY n.IdNhatKy DESC LIMIT 1),'') StaffReply,
            (SELECT n.ThoiDiem FROM NhatKy n WHERE n.BangDuLieu='PhanHoiDocGia' AND n.KhoaBanGhi=CAST(p.IdPhanHoi AS CHAR) AND n.HanhDong='XU_LY_PHAN_HOI' ORDER BY n.IdNhatKy DESC LIMIT 1) ProcessedAt
            FROM PhanHoiDocGia p JOIN DocGia d USING(IdDocGia) LEFT JOIN DauTaiLieu t USING(IdDauTaiLieu)
            WHERE (@own IS NULL OR p.IdDocGia=@own) AND (d.MaDinhDanhTruong LIKE @term ESCAPE '!' OR d.HoTen LIKE @term ESCAPE '!' OR p.TieuDe LIKE @term ESCAPE '!') ORDER BY p.IdPhanHoi DESC LIMIT 100
            """, ct, ("own", ownReader), ("term", Term(q.Keyword))));
    private async Task<string> ProcessFeedback(CatalogStore s, ProcessFeedbackRequest q, int actor, CancellationToken ct)
    {
        Need(q.StaffReply, 3000, "Nội dung xử lý");
        if (q.NewStatus != "DA_XU_LY") throw new ArgumentException("Trạng thái xử lý không hợp lệ.");
        var row = await s.Rows("SELECT TrangThai FROM PhanHoiDocGia WHERE IdPhanHoi=@id FOR UPDATE", ct, ("id", q.FeedbackId));
        if (row.Count != 1) throw new InvalidOperationException("Không tìm thấy phản hồi.");
        if (row[0]["TrangThai"]?.ToString() == "DA_XU_LY") throw new InvalidOperationException("Phản hồi đã được xử lý; tải lại để xem kết quả.");
        await s.Execute("UPDATE PhanHoiDocGia SET TrangThai='DA_XU_LY',IdNguoiXuLy=@actor WHERE IdPhanHoi=@id", ct, ("actor", actor), ("id", q.FeedbackId));
        await s.Audit(actor, "XU_LY_PHAN_HOI", "PhanHoiDocGia", q.FeedbackId.ToString(), new { Status = row[0]["TrangThai"] }, new { q.StaffReply, q.NewStatus }, ct); return "Đã xử lý và ghi nhật ký.";
    }
}
