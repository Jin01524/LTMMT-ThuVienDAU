using ThuVienDAU.Catalog;
using ThuVienDAU.Circulation.BC03;

namespace ThuVienDAU.Circulation;

public sealed partial class CirculationService
{
    private async Task<CirculationDashboardReport> Statistics(CatalogStore s, StatisticsQuery q, string name, string role, CancellationToken ct)
    {
        if (q.FromDate.Date > q.ToDate.Date || (q.ToDate.Date - q.FromDate.Date).TotalDays > 366 || q.ToDate.Year >= 9999) throw new ArgumentException("Khoảng thống kê phải hợp lệ và không quá 367 ngày.");
        var args = new (string, object?)[] { ("from", q.FromDate.Date), ("to", q.ToDate.Date.AddDays(1)), ("now", Now) };
        var totals = await s.Rows("""
            SELECT
            (SELECT COUNT(*) FROM PhieuMuon WHERE ThoiDiemMuon>=@from AND ThoiDiemMuon<@to AND TrangThai<>'HUY') TotalLoanTickets,
            (SELECT COUNT(*) FROM ChiTietPhieuMuon c JOIN PhieuMuon p USING(IdPhieuMuon) WHERE p.ThoiDiemMuon>=@from AND p.ThoiDiemMuon<@to AND c.TrangThai<>'HUY') TotalBorrowedCopies,
            (SELECT COUNT(*) FROM ChiTietPhieuMuon WHERE ThoiDiemTra>=@from AND ThoiDiemTra<@to AND TrangThai='DA_TRA') TotalReturnedCopies,
            (SELECT COUNT(*) FROM NhatKy WHERE HanhDong='GIA_HAN' AND ThoiDiem>=@from AND ThoiDiem<@to) TotalRenewals,
            (SELECT COUNT(*) FROM ChiTietPhieuMuon WHERE TrangThai='DANG_MUON' AND HanTra<@now) CurrentOverdueCopies,
            (SELECT COUNT(*) FROM ChiTietPhieuMuon WHERE TrangThai='DA_TRA' AND ThoiDiemTra>HanTra AND ThoiDiemTra>=@from AND ThoiDiemTra<@to) LateReturnedInPeriod,
            (SELECT COUNT(*) FROM LuotVaoPhongDoc WHERE ThoiDiemVao>=@from AND ThoiDiemVao<@to) ReadingRoomVisitsInPeriod,
            (SELECT COUNT(*) FROM LuotVaoPhongDoc WHERE ThoiDiemRa IS NULL) CurrentlyInReadingRoom,
            (SELECT COALESCE(SUM(SoTien),0) FROM ViPham WHERE ThoiDiemLap>=@from AND ThoiDiemLap<@to AND TrangThai<>'DA_HUY') TotalAssessedFinesInPeriod,
            (SELECT COALESCE(SUM(SoTienThu),0) FROM PhieuThu WHERE ThoiDiemThu>=@from AND ThoiDiemThu<@to) TotalCollectedReceiptsInPeriod,
            (SELECT COUNT(*) FROM PhieuThu WHERE ThoiDiemThu>=@from AND ThoiDiemThu<@to) ReceiptCountInPeriod,
            (SELECT COALESCE(SUM(SoTien),0) FROM ViPham WHERE TrangThai='CHUA_XU_LY') TotalOutstandingUnpaidFines,
            (SELECT COALESCE(SUM(SoTienThu),0) FROM PhieuThu WHERE ThoiDiemThu>=@from AND ThoiDiemThu<@to AND HinhThuc='TIEN_MAT') CashCollectedInPeriod,
            (SELECT COALESCE(SUM(SoTienThu),0) FROM PhieuThu WHERE ThoiDiemThu>=@from AND ThoiDiemThu<@to AND HinhThuc='CHUYEN_KHOAN') TransferCollectedInPeriod
            """, ct, args);
        var report = Map<CirculationDashboardReport>(totals.Single()); report.ViewerName = name; report.ViewerRole = role; report.FromDate = q.FromDate.Date; report.ToDate = q.ToDate.Date;
        report.DailyRows = Map<DailyCirculationRow>(await s.Rows("""
            SELECT ReportDate,SUM(BorrowedCount) BorrowedCount,SUM(ReturnedCount) ReturnedCount,SUM(ReadingRoomVisits) ReadingRoomVisits,SUM(FineAssessed) FineAssessed,SUM(FineCollected) FineCollected FROM (
              SELECT DATE(p.ThoiDiemMuon) ReportDate,COUNT(*) BorrowedCount,0 ReturnedCount,0 ReadingRoomVisits,0 FineAssessed,0 FineCollected FROM ChiTietPhieuMuon c JOIN PhieuMuon p USING(IdPhieuMuon) WHERE p.ThoiDiemMuon>=@from AND p.ThoiDiemMuon<@to AND c.TrangThai<>'HUY' GROUP BY DATE(p.ThoiDiemMuon)
              UNION ALL SELECT DATE(ThoiDiemTra),0,COUNT(*),0,0,0 FROM ChiTietPhieuMuon WHERE TrangThai='DA_TRA' AND ThoiDiemTra>=@from AND ThoiDiemTra<@to GROUP BY DATE(ThoiDiemTra)
              UNION ALL SELECT DATE(ThoiDiemVao),0,0,COUNT(*),0,0 FROM LuotVaoPhongDoc WHERE ThoiDiemVao>=@from AND ThoiDiemVao<@to GROUP BY DATE(ThoiDiemVao)
              UNION ALL SELECT DATE(ThoiDiemLap),0,0,0,SUM(SoTien),0 FROM ViPham WHERE TrangThai<>'DA_HUY' AND ThoiDiemLap>=@from AND ThoiDiemLap<@to GROUP BY DATE(ThoiDiemLap)
              UNION ALL SELECT DATE(ThoiDiemThu),0,0,0,0,SUM(SoTienThu) FROM PhieuThu WHERE ThoiDiemThu>=@from AND ThoiDiemThu<@to GROUP BY DATE(ThoiDiemThu)
            ) x GROUP BY ReportDate ORDER BY ReportDate
            """, ct, args));
        return report;
    }
    private static async Task<ReceiptDocument> Receipt(CatalogStore s, ReceiptQuery q, CancellationToken ct)
    {
        if (q.Id <= 0) throw new ArgumentException("Mã phiếu không hợp lệ.");
        if (q.Kind == "LOAN")
        {
            var rows = await s.Rows("SELECT p.*,d.MaDinhDanhTruong,d.HoTen,a.TenDangNhap FROM PhieuMuon p JOIN DocGia d USING(IdDocGia) JOIN TaiKhoan a ON a.IdTaiKhoan=p.IdThuThu WHERE p.IdPhieuMuon=@id", ct, ("id", q.Id));
            if (rows.Count != 1) throw new InvalidOperationException("Không tìm thấy phiếu mượn.");
            var row = rows[0]; var lines = new List<string> { "THƯ VIỆN TRƯỜNG ĐẠI HỌC KIẾN TRÚC ĐÀ NẴNG", $"PHIẾU MƯỢN #{q.Id}", $"Độc giả: {row["HoTen"]} ({row["MaDinhDanhTruong"]})", $"Thủ thư: {row["TenDangNhap"]} | Ngày mượn: {row["ThoiDiemMuon"]:dd/MM/yyyy HH:mm}", "" };
            var copies = await s.Rows("SELECT c.IdDauTaiLieu,b.MaVach,b.SoDangKyCaBiet,c.HanTra,c.TrangThai FROM ChiTietPhieuMuon c JOIN BanTaiLieu b USING(IdBanTaiLieu) WHERE c.IdPhieuMuon=@id ORDER BY c.IdChiTietMuon", ct, ("id", q.Id));
            foreach (var copy in copies)
            {
                var bib = await MarcService.Summary(s, Id(copy, "IdDauTaiLieu"), ct);
                lines.Add($"{bib.NhanDe} | {bib.TacGia}"); lines.Add($"ISBN: {bib.ISBN} | Barcode: {copy["MaVach"]} | ĐKCB: {copy["SoDangKyCaBiet"]}"); lines.Add($"Hạn trả hiện tại: {copy["HanTra"]:dd/MM/yyyy} | {copy["TrangThai"]}"); lines.Add("");
            }
            lines.Add("Bạn đọc kiểm tra số lượng, tình trạng tài liệu và hạn trả trước khi rời quầy."); return new("Phiếu mượn", lines.ToArray());
        }
        if (q.Kind == "RECEIPT")
        {
            var rows = await s.Rows("SELECT p.*,d.HoTen,d.MaDinhDanhTruong,a.TenDangNhap FROM PhieuThu p JOIN DocGia d USING(IdDocGia) JOIN TaiKhoan a ON a.IdTaiKhoan=p.IdNguoiThu WHERE p.IdPhieuThu=@id", ct, ("id", q.Id));
            if (rows.Count != 1) throw new InvalidOperationException("Không tìm thấy biên lai."); var row = rows[0];
            var lines = new List<string> { "THƯ VIỆN TRƯỜNG ĐẠI HỌC KIẾN TRÚC ĐÀ NẴNG", $"BIÊN LAI THU TIỀN #{q.Id}", $"Độc giả: {row["HoTen"]} ({row["MaDinhDanhTruong"]})", $"Người thu: {row["TenDangNhap"]} | {row["ThoiDiemThu"]:dd/MM/yyyy HH:mm}", $"Hình thức: {row["HinhThuc"]}", "" };
            var items = await s.Rows("SELECT IdViPham,CanCu,SoTien,TrangThai,LyDoHuy FROM ViPham WHERE IdPhieuThu=@id ORDER BY IdViPham", ct, ("id", q.Id));
            foreach (var item in items) lines.Add($"VP#{item["IdViPham"]}: {item["SoTien"]:N0}đ | {item["TrangThai"]} | {item["CanCu"]}" + (item["LyDoHuy"] is null ? "" : $" | Lý do hủy: {item["LyDoHuy"]}"));
            lines.Add($"TỔNG THỰC THU: {row["SoTienThu"]:N0} đồng"); lines.Add("Hủy hiệu lực vi phạm không xóa biên lai đã thu và không tự động hoàn tiền."); return new("Biên lai thu tiền", lines.ToArray());
        }
        if (q.Kind == "NOTICE")
        {
            var rows = await s.Rows("SELECT v.*,d.HoTen,d.MaDinhDanhTruong,d.LoaiDocGia,k.TenKhoa FROM ViPham v JOIN DocGia d USING(IdDocGia) LEFT JOIN Khoa k USING(IdKhoa) WHERE v.IdViPham=@id AND v.LoaiViPham='QUA_HAN' AND v.SoTien=0 AND d.LoaiDocGia<>'SINH_VIEN'", ct, ("id", q.Id));
            if (rows.Count != 1) throw new InvalidOperationException("Không tìm thấy thông báo đơn vị."); var row = rows[0];
            return new("Thông báo đơn vị", new[] { "THƯ VIỆN TRƯỜNG ĐẠI HỌC KIẾN TRÚC ĐÀ NẴNG", $"THÔNG BÁO VI PHẠM THỜI HẠN MƯỢN #{q.Id}", $"Kính gửi: {row["TenKhoa"] ?? "Đơn vị quản lý (cần bổ sung trước khi gửi)"}", $"Bạn đọc: {row["HoTen"]} ({row["MaDinhDanhTruong"]})", row["CanCu"]!.ToString()!, "Đề nghị đơn vị quản lý phối hợp xử lý theo nội quy; báo cáo lãnh đạo trong báo cáo định kỳ.", "Bản này được lập để in/chuyển tới đơn vị; hệ thống chưa gửi thông báo tự động." });
        }
        throw new ArgumentException("Loại phiếu không hợp lệ.");
    }
}
