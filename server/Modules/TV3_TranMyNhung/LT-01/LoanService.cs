using ThuVienDAU.Catalog;
using ThuVienDAU.Circulation.LT01;
using ThuVienDAU.Circulation.LT02;
using ThuVienDAU.Circulation.LT03;
using ThuVienDAU.Circulation.LT04;

namespace ThuVienDAU.Circulation;

public sealed partial class CirculationService
{
    private async Task<BorrowReaderLookup> LookupReader(CatalogStore s, string code, CancellationToken ct)
    {
        Need(code, 50, "Mã trường");
        var rows = await s.Rows("""
            SELECT d.IdDocGia ReaderId,d.MaDinhDanhTruong ReaderCode,d.HoTen FullName,d.LoaiDocGia ReaderType,
            d.TrangThaiHoSo ProfileStatus,d.DuocMuon CanBorrow,d.LyDoKhoa LockReason,
            (SELECT COUNT(*) FROM ChiTietPhieuMuon c WHERE c.IdDocGia=d.IdDocGia AND c.TrangThai='DANG_MUON') ActiveBorrowCount,
            (SELECT COUNT(*) FROM ChiTietPhieuMuon c WHERE c.IdDocGia=d.IdDocGia AND c.TrangThai='DANG_MUON' AND c.HanTra<@now) OverdueCount,
            (SELECT COALESCE(SUM(v.SoTien),0) FROM ViPham v WHERE v.IdDocGia=d.IdDocGia AND v.TrangThai='CHUA_XU_LY') UnpaidFineTotal
            FROM DocGia d WHERE d.MaDinhDanhTruong=@code
            """, ct, ("code", code.Trim()), ("now", Now));
        if (rows.Count != 1) throw new InvalidOperationException("Không tìm thấy độc giả theo mã trường.");
        var result = Map<BorrowReaderLookup>(rows[0]);
        result.MaxBorrowLimit = result.ReaderType == "SINH_VIEN" ? 2 : 3;
        result.LoanDays = result.ReaderType == "SINH_VIEN" ? 10 : 15;
        result.ActiveTitleIds = (await s.Rows("SELECT IdDauTaiLieu FROM ChiTietPhieuMuon WHERE IdDocGia=@id AND TrangThai='DANG_MUON'", ct, ("id", result.ReaderId))).Select(x => Id(x, "IdDauTaiLieu")).ToList();
        return result;
    }
    private static async Task<BorrowCopyLookup> LookupCopy(CatalogStore s, string code, CancellationToken ct)
    {
        Need(code, 100, "Mã bản sách");
        var rows = await s.Rows("""
            SELECT b.IdBanTaiLieu CopyId,b.IdDauTaiLieu TitleId,b.MaVach Barcode,b.SoDangKyCaBiet RegistrationNumber,
            d.NhanDe TitleName,b.TrangThai Status,b.TinhTrang `Condition`,CONCAT(COALESCE(v.DayKe,''),' / ',COALESCE(v.Ngan,'')) LocationText
            FROM BanTaiLieu b JOIN DauTaiLieu d USING(IdDauTaiLieu) LEFT JOIN ViTriLuuTru v USING(IdViTri)
            WHERE b.MaVach=@code OR b.SoDangKyCaBiet=@code LIMIT 1
            """, ct, ("code", code.Trim()));
        return rows.Count == 1 ? Map<BorrowCopyLookup>(rows[0]) : throw new InvalidOperationException("Không tìm thấy bản sách.");
    }
    private async Task<BorrowReceiptResult> Borrow(CatalogStore s, BorrowCommand q, int actor, CancellationToken ct)
    {
        Rules.RequireOpen(Now, true);
        if (q.CopyIds is null || q.CopyIds.Length is < 1 or > 3 || q.CopyIds.Distinct().Count() != q.CopyIds.Length || q.CopyIds.Any(x => x <= 0)) throw new ArgumentException("Danh sách mượn phải gồm 1–3 bản sách khác nhau.");
        var reader = await Reader(s, q.ReaderId, ct);
        await CheckReader(s, reader, false, ct);
        bool student = reader["LoaiDocGia"]?.ToString() == "SINH_VIEN";
        var active = await s.Rows("SELECT IdDauTaiLieu FROM ChiTietPhieuMuon WHERE IdDocGia=@id AND TrangThai='DANG_MUON'", ct, ("id", q.ReaderId));
        if (active.Count + q.CopyIds.Length > (student ? 2 : 3)) throw new InvalidOperationException("Số sách vượt hạn mức độc giả.");
        var titles = active.Select(x => Id(x, "IdDauTaiLieu")).ToHashSet();
        var copies = new List<Dictionary<string, object?>>();
        foreach (int id in q.CopyIds.Order())
        {
            var rows = await s.Rows("SELECT b.*,d.NhanDe FROM BanTaiLieu b JOIN DauTaiLieu d USING(IdDauTaiLieu) WHERE b.IdBanTaiLieu=@id FOR UPDATE", ct, ("id", id));
            if (rows.Count != 1 || rows[0]["TrangThai"]?.ToString() != "SAN_SANG" || rows[0]["TinhTrang"]?.ToString() == "HU_HONG") throw new InvalidOperationException("Có sách không sẵn sàng để mượn.");
            if (!titles.Add(Id(rows[0], "IdDauTaiLieu"))) throw new InvalidOperationException("Không mượn hai bản cùng nhan đề.");
            copies.Add(rows[0]);
        }
        DateTime now = Now, due = CirculationRules.EndOfDay(now.AddDays(student ? 10 : 15));
        int loanId = await s.Execute("INSERT INTO PhieuMuon(IdDocGia,IdThuThu,ThoiDiemMuon) VALUES(@reader,@actor,@now)", ct, ("reader", q.ReaderId), ("actor", actor), ("now", now));
        var result = new BorrowReceiptResult { LoanId = loanId, ReaderId = q.ReaderId, ReaderCode = reader["MaDinhDanhTruong"]!.ToString(), ReaderName = reader["HoTen"]!.ToString(), BorrowedAt = now, Items = new() };
        foreach (var copy in copies)
        {
            int copyId = Id(copy, "IdBanTaiLieu");
            int detailId = await s.Execute("INSERT INTO ChiTietPhieuMuon(IdPhieuMuon,IdDocGia,IdBanTaiLieu,IdDauTaiLieu,HanTra) VALUES(@loan,@reader,@copy,@title,@due)", ct, ("loan", loanId), ("reader", q.ReaderId), ("copy", copyId), ("title", Id(copy, "IdDauTaiLieu")), ("due", due));
            await s.Execute("UPDATE BanTaiLieu SET TrangThai='DANG_MUON' WHERE IdBanTaiLieu=@id", ct, ("id", copyId));
            result.Items.Add(new() { DetailId = detailId, Barcode = copy["MaVach"]!.ToString(), RegistrationNumber = copy["SoDangKyCaBiet"]!.ToString(), TitleName = copy["NhanDe"]!.ToString(), DueDate = due });
        }
        await s.Audit(actor, "MUON_SACH", "PhieuMuon", loanId.ToString(), null, result, ct);
        return result;
    }
    private async Task<ActiveLoanCopyInfo> ReturnLookup(CatalogStore s, string code, CancellationToken ct)
    {
        Need(code, 100, "Mã bản sách");
        var rows = await s.Rows("""
            SELECT c.IdChiTietMuon DetailId,c.IdPhieuMuon LoanId,c.IdBanTaiLieu CopyId,c.IdDocGia ReaderId,
            d.MaDinhDanhTruong ReaderCode,d.HoTen ReaderName,b.MaVach Barcode,b.SoDangKyCaBiet RegistrationNumber,
            t.NhanDe TitleName,p.ThoiDiemMuon BorrowedAt,c.HanTra DueDate,b.TinhTrang CurrentCondition
            FROM ChiTietPhieuMuon c JOIN PhieuMuon p USING(IdPhieuMuon) JOIN DocGia d ON d.IdDocGia=c.IdDocGia
            JOIN BanTaiLieu b ON b.IdBanTaiLieu=c.IdBanTaiLieu JOIN DauTaiLieu t ON t.IdDauTaiLieu=c.IdDauTaiLieu
            WHERE c.TrangThai='DANG_MUON' AND (b.MaVach=@code OR b.SoDangKyCaBiet=@code) LIMIT 1
            """, ct, ("code", code.Trim()));
        if (rows.Count != 1) throw new InvalidOperationException("Không tìm thấy khoản đang mượn của bản sách.");
        var item = Map<ActiveLoanCopyInfo>(rows[0]); item.OverdueDays = Rules.Overdue(item.DueDate, Now).Raw; return item;
    }
    private async Task<List<ReturnProcessedItem>> Return(CatalogStore s, ReturnCommand q, int actor, CancellationToken ct)
    {
        if (q.Items is null || q.Items.Count is < 1 or > 50 || q.Items.Select(x => x.DetailId).Distinct().Count() != q.Items.Count) throw new ArgumentException("Danh sách trả phải có 1–50 khoản khác nhau.");
        var readers = new HashSet<int>();
        foreach (var input in q.Items) readers.Add(await DetailReader(s, input.DetailId, ct));
        foreach (int id in readers.Order()) await Reader(s, id, ct);
        var result = new List<ReturnProcessedItem>(); var loanIds = new HashSet<int>();
        foreach (var input in q.Items.OrderBy(x => x.DetailId))
        {
            if (input.ReturnOutcome is not ("BINH_THUONG" or "HU_HONG" or "MAT") || input.Note?.Length > 500) throw new ArgumentException("Kết quả trả/ghi chú không hợp lệ.");
            var detail = await Detail(s, input.DetailId, ct);
            if (detail["TrangThai"]?.ToString() != "DANG_MUON") throw new InvalidOperationException("Khoản mượn đã được xử lý trả/mất; tải lại danh sách.");
            var copies = await s.Rows("SELECT * FROM BanTaiLieu WHERE IdBanTaiLieu=@id FOR UPDATE", ct, ("id", Id(detail, "IdBanTaiLieu")));
            var copy = copies.Single(); var info = await ReturnLookup(s, copy["MaVach"]!.ToString()!, ct);
            string copyStatus = input.ReturnOutcome == "BINH_THUONG" ? "SAN_SANG" : input.ReturnOutcome;
            string condition = input.ReturnOutcome == "HU_HONG" ? "HU_HONG" : copy["TinhTrang"]!.ToString()!;
            DateTime now = Now;
            await s.Execute("UPDATE ChiTietPhieuMuon SET TrangThai=@status,ThoiDiemTra=@now,IdNguoiNhanTra=@actor WHERE IdChiTietMuon=@id", ct, ("status", input.ReturnOutcome == "MAT" ? "MAT" : "DA_TRA"), ("now", now), ("actor", actor), ("id", input.DetailId));
            await s.Execute("UPDATE BanTaiLieu SET TrangThai=@status,TinhTrang=@condition WHERE IdBanTaiLieu=@id", ct, ("status", copyStatus), ("condition", condition), ("id", info.CopyId));
            await s.Audit(actor, "TRA_SACH", "ChiTietPhieuMuon", input.DetailId.ToString(), new { Status = "DANG_MUON" }, new { Condition = condition, Outcome = input.ReturnOutcome, input.Note, ReturnedAt = now }, ct);
            loanIds.Add(info.LoanId);
            result.Add(new() { DetailId = input.DetailId, LoanId = info.LoanId, Barcode = info.Barcode, TitleName = info.TitleName, ReaderCode = info.ReaderCode, ReaderName = info.ReaderName, ReturnedAt = now, OverdueDays = info.OverdueDays, ReturnOutcome = input.ReturnOutcome, HasViolationBasis = info.OverdueDays > 0 || input.ReturnOutcome != "BINH_THUONG", ViolationSummary = $"{input.ReturnOutcome}; quá hạn thô {info.OverdueDays} ngày; cần đối chiếu lễ/Tết tại VP-01." });
        }
        foreach (int id in loanIds) await s.Execute("UPDATE PhieuMuon SET TrangThai='DA_DONG' WHERE IdPhieuMuon=@id AND NOT EXISTS(SELECT 1 FROM ChiTietPhieuMuon WHERE IdPhieuMuon=@id AND TrangThai='DANG_MUON')", ct, ("id", id));
        return result;
    }
    private async Task<List<RenewableLoanItem>> RenewList(CatalogStore s, string keyword, CancellationToken ct)
    {
        var rows = await s.Rows("""
            SELECT c.IdChiTietMuon DetailId,c.IdPhieuMuon LoanId,c.IdDocGia ReaderId,c.IdDauTaiLieu TitleId,
            d.MaDinhDanhTruong ReaderCode,d.HoTen ReaderName,d.LoaiDocGia ReaderType,b.MaVach Barcode,t.NhanDe TitleName,
            c.HanTra DueDate,c.SoLanGiaHan RenewCount FROM ChiTietPhieuMuon c JOIN DocGia d ON d.IdDocGia=c.IdDocGia
            JOIN BanTaiLieu b ON b.IdBanTaiLieu=c.IdBanTaiLieu JOIN DauTaiLieu t ON t.IdDauTaiLieu=c.IdDauTaiLieu
            WHERE c.TrangThai='DANG_MUON' AND (d.MaDinhDanhTruong LIKE @term ESCAPE '!' OR b.MaVach LIKE @term ESCAPE '!') ORDER BY c.IdChiTietMuon LIMIT 100
            """, ct, ("term", Term(keyword)));
        var items = Map<RenewableLoanItem>(rows);
        foreach (var item in items)
        {
            try { await CheckReader(s, await Reader(s, item.ReaderId, ct), true, ct); item.CanRenew = true; item.BlockReason = "Cần xác nhận không có người khác đăng ký và chọn hạn mới."; }
            catch (InvalidOperationException ex) { item.CanRenew = false; item.BlockReason = ex.Message; }
        }
        return items;
    }
    private async Task<RenewResult> Renew(CatalogStore s, RenewCommand q, int actor, CancellationToken ct)
    {
        Rules.RequireOpen(Now, false);
        if (!q.ConfirmedNoOtherRequest) throw new ArgumentException("Thủ thư phải xác nhận không có bạn đọc khác đăng ký mượn tài liệu.");
        Need(q.Reason, 300, "Căn cứ gia hạn");
        int readerId = await DetailReader(s, q.DetailId, ct); var reader = await Reader(s, readerId, ct); await CheckReader(s, reader, true, ct);
        var detail = await Detail(s, q.DetailId, ct);
        if (detail["TrangThai"]?.ToString() != "DANG_MUON") throw new InvalidOperationException("Chỉ gia hạn khoản đang mượn.");
        DateTime old = Convert.ToDateTime(detail["HanTra"]), next = CirculationRules.EndOfDay(q.NewDueDate);
        if (next <= old || next <= Now) throw new ArgumentException("Hạn mới phải lớn hơn hạn hiện tại và thời điểm hiện tại.");
        if (Id(detail, "SoLanGiaHan") >= short.MaxValue) throw new InvalidOperationException("Số lần gia hạn vượt giới hạn lưu trữ.");
        await s.Execute("UPDATE ChiTietPhieuMuon SET HanTra=@next,SoLanGiaHan=SoLanGiaHan+1 WHERE IdChiTietMuon=@id", ct, ("next", next), ("id", q.DetailId));
        await s.Audit(actor, "GIA_HAN", "ChiTietPhieuMuon", q.DetailId.ToString(), new { DueDate = old }, new { DueDate = next, q.Reason, q.ConfirmedNoOtherRequest }, ct);
        var copy = (await s.Rows("SELECT b.MaVach,t.NhanDe FROM BanTaiLieu b JOIN DauTaiLieu t USING(IdDauTaiLieu) WHERE b.IdBanTaiLieu=@id", ct, ("id", Id(detail, "IdBanTaiLieu")))).Single();
        return new() { DetailId = q.DetailId, Barcode = copy["MaVach"]!.ToString(), TitleName = copy["NhanDe"]!.ToString(), Channel = "TAI_QUAY", NewRenewCount = Id(detail, "SoLanGiaHan") + 1, OldDueDate = old, NewDueDate = next };
    }
    private async Task<List<LoanHistoryRecord>> History(CatalogStore s, LoanHistoryFilter q, CancellationToken ct)
    {
        if (q.StatusFilter is not (null or "TAT_CA" or "DANG_MUON" or "QUA_HAN" or "DA_TRA" or "HU_HONG" or "MAT")) throw new ArgumentException("Trạng thái tra cứu không hợp lệ.");
        if (q.UseDateRange && (q.FromDate > q.ToDate || q.ToDate.Year >= 9999)) throw new ArgumentException("Khoảng ngày không hợp lệ.");
        var rows = await s.Rows("""
            SELECT c.IdChiTietMuon DetailId,c.IdPhieuMuon LoanId,p.TrangThai LoanStatus,d.MaDinhDanhTruong ReaderCode,d.HoTen ReaderName,d.LoaiDocGia ReaderType,
            b.MaVach Barcode,b.SoDangKyCaBiet RegistrationNumber,t.NhanDe TitleName,p.ThoiDiemMuon BorrowedAt,c.HanTra DueDate,c.ThoiDiemTra ReturnedAt,c.SoLanGiaHan RenewCount,c.TrangThai DetailStatus,
            COALESCE((SELECT JSON_UNQUOTE(n.ChiTiet->'$.after.condition') FROM NhatKy n WHERE n.BangDuLieu='ChiTietPhieuMuon' AND n.KhoaBanGhi=CAST(c.IdChiTietMuon AS CHAR) AND n.HanhDong='TRA_SACH' ORDER BY n.IdNhatKy DESC LIMIT 1),'') ReturnCondition,
            COALESCE((SELECT JSON_UNQUOTE(n.ChiTiet->'$.after.note') FROM NhatKy n WHERE n.BangDuLieu='ChiTietPhieuMuon' AND n.KhoaBanGhi=CAST(c.IdChiTietMuon AS CHAR) AND n.HanhDong='TRA_SACH' ORDER BY n.IdNhatKy DESC LIMIT 1),'') Note
            FROM ChiTietPhieuMuon c JOIN PhieuMuon p USING(IdPhieuMuon) JOIN DocGia d ON d.IdDocGia=c.IdDocGia JOIN BanTaiLieu b ON b.IdBanTaiLieu=c.IdBanTaiLieu JOIN DauTaiLieu t ON t.IdDauTaiLieu=c.IdDauTaiLieu
            WHERE (d.MaDinhDanhTruong LIKE @term ESCAPE '!' OR d.HoTen LIKE @term ESCAPE '!' OR b.MaVach LIKE @term ESCAPE '!' OR CAST(p.IdPhieuMuon AS CHAR) LIKE @term ESCAPE '!')
            AND (@range=0 OR (p.ThoiDiemMuon>=@from AND p.ThoiDiemMuon<@to)) ORDER BY c.IdChiTietMuon DESC LIMIT 100
            """, ct, ("term", Term(q.Keyword)), ("range", q.UseDateRange), ("from", q.FromDate.Date), ("to", q.UseDateRange ? q.ToDate.Date.AddDays(1) : DateTime.MaxValue));
        var items = Map<LoanHistoryRecord>(rows);
        foreach (var item in items) { item.IsOverdue = item.DetailStatus == "DANG_MUON" && item.DueDate < Now; item.OverdueDays = Rules.Overdue(item.DueDate, item.ReturnedAt ?? Now).Raw; }
        return items.Where(x => string.IsNullOrEmpty(q.StatusFilter) || q.StatusFilter == "TAT_CA" || q.StatusFilter == "QUA_HAN" && x.IsOverdue || q.StatusFilter == "HU_HONG" && x.ReturnCondition == "HU_HONG" || q.StatusFilter != "QUA_HAN" && q.StatusFilter != "HU_HONG" && x.DetailStatus == q.StatusFilter).ToList();
    }
}
