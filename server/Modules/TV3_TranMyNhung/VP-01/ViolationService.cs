using ThuVienDAU.Catalog;
using ThuVienDAU.Circulation.VP01;
using ThuVienDAU.Circulation.VP02;
using ThuVienDAU.Circulation.VP03;

namespace ThuVienDAU.Circulation;

public sealed partial class CirculationService
{
    private async Task<List<ViolationCandidate>> Candidates(CatalogStore s, string keyword, CancellationToken ct)
    {
        var rows = await s.Rows("""
            SELECT c.IdChiTietMuon DetailId,c.IdPhieuMuon LoanId,c.IdDocGia ReaderId,d.MaDinhDanhTruong ReaderCode,d.HoTen ReaderName,d.LoaiDocGia ReaderType,
            b.MaVach Barcode,t.NhanDe TitleName,COALESCE(b.GiaBia,0) BookPrice,(b.GiaBia IS NOT NULL) HasBookPrice,c.HanTra DueDate,c.ThoiDiemTra ReturnedAt,c.TrangThai DetailStatus,
            COALESCE((SELECT JSON_UNQUOTE(n.ChiTiet->'$.after.condition') FROM NhatKy n WHERE n.BangDuLieu='ChiTietPhieuMuon' AND n.KhoaBanGhi=CAST(c.IdChiTietMuon AS CHAR) AND n.HanhDong='TRA_SACH' ORDER BY n.IdNhatKy DESC LIMIT 1),'') ReturnCondition
            FROM ChiTietPhieuMuon c JOIN DocGia d ON d.IdDocGia=c.IdDocGia JOIN BanTaiLieu b ON b.IdBanTaiLieu=c.IdBanTaiLieu JOIN DauTaiLieu t ON t.IdDauTaiLieu=c.IdDauTaiLieu
            WHERE (d.MaDinhDanhTruong LIKE @term ESCAPE '!' OR d.HoTen LIKE @term ESCAPE '!' OR b.MaVach LIKE @term ESCAPE '!')
            AND c.TrangThai IN ('DANG_MUON','DA_TRA','MAT') AND (
              (c.TrangThai='MAT' AND NOT EXISTS(SELECT 1 FROM ViPham v WHERE v.IdChiTietMuon=c.IdChiTietMuon AND v.LoaiViPham IN ('MAT_SACH','PHI_XU_LY')))
              OR ((c.ThoiDiemTra>c.HanTra OR c.TrangThai='DANG_MUON' AND c.HanTra<@now) AND NOT EXISTS(SELECT 1 FROM ViPham v WHERE v.IdChiTietMuon=c.IdChiTietMuon AND v.LoaiViPham='QUA_HAN'))
              OR (EXISTS(SELECT 1 FROM NhatKy n WHERE n.BangDuLieu='ChiTietPhieuMuon' AND n.KhoaBanGhi=CAST(c.IdChiTietMuon AS CHAR) AND n.HanhDong='TRA_SACH' AND JSON_UNQUOTE(n.ChiTiet->'$.after.condition')='HU_HONG') AND NOT EXISTS(SELECT 1 FROM ViPham v WHERE v.IdChiTietMuon=c.IdChiTietMuon AND v.LoaiViPham='HONG_TRANG')))
            ORDER BY c.IdChiTietMuon DESC LIMIT 100
            """, ct, ("term", Term(keyword)), ("now", Now));
        var result = new List<ViolationCandidate>(); var rule = Rules;
        foreach (var row in rows)
        {
            row["HasBookPrice"] = Convert.ToBoolean(row["HasBookPrice"]);
            var existing = await s.Rows("SELECT LoaiViPham FROM ViPham WHERE IdChiTietMuon=@id", ct, ("id", Id(row, "DetailId")));
            var types = existing.Select(x => x["LoaiViPham"]!.ToString()).ToHashSet();
            var item = Map<ViolationCandidate>(row); var days = rule.Overdue(item.DueDate, item.ReturnedAt ?? Now);
            if (days.Raw > 0 && !types.Contains("QUA_HAN"))
            {
                var late = Map<ViolationCandidate>(row); late.ViolationType = "QUA_HAN"; late.RawOverdueDays = days.Raw; late.HolidayDaysExcluded = days.Excluded; late.ChargeableDays = days.Net; late.IsCalendarFinalized = rule.CalendarConfirmed;
                late.SuggestedAmount = late.ReaderType == "SINH_VIEN" && rule.CalendarConfirmed ? days.Net * 2000m : 0;
                late.CalculationNote = late.ReaderType != "SINH_VIEN" ? "Lập thông báo tới đơn vị quản lý; không phạt tiền cán bộ/giảng viên." : rule.CalendarConfirmed ? $"{days.Net} ngày tính phạt × 2.000đ; loại {days.Excluded} ngày lễ/Tết." : "Chưa xác nhận lịch miễn phạt; không được tự chốt tiền phạt sinh viên.";
                result.Add(late);
            }
            if (item.DetailStatus == "MAT" && !types.Contains("MAT_SACH") && !types.Contains("PHI_XU_LY"))
            {
                item.ViolationType = "MAT_SACH"; item.SuggestedAmount = item.HasBookPrice ? item.BookPrice * 2 : 0;
                item.CalculationNote = item.HasBookPrice ? "Đền tiền: 2 × giá bìa; đền sách: phí kỹ thuật 20.000đ, xác nhận đã nhận sách thay thế." : "Chưa có giá bìa: Quản lý xác định giá sách tương đương để nhân 2, hoặc nhận sách thay thế và thu phí 20.000đ.";
                result.Add(item);
            }
            if (row["ReturnCondition"]?.ToString() == "HU_HONG" && !types.Contains("HONG_TRANG"))
            {
                var damaged = Map<ViolationCandidate>(row); damaged.ViolationType = "HONG_TRANG"; damaged.CalculationNote = "Quản lý thư viện xác định chi phí tái tạo/thay thế trang; không tự lấy phần trăm giá bìa."; result.Add(damaged);
            }
        }
        return result;
    }
    private async Task<ViolationRecordResult> CreateViolation(CatalogStore s, CreateViolationInput q, int actor, string role, CancellationToken ct)
    {
        Need(q.Description, 700, "Căn cứ xử lý");
        int readerId = await DetailReader(s, q.DetailId, ct); var reader = await Reader(s, readerId, ct); var detail = await Detail(s, q.DetailId, ct);
        // Re-read source data while holding the reader/detail/copy locks. Client suggestions are never authoritative.
        await s.Rows("SELECT IdBanTaiLieu FROM BanTaiLieu WHERE IdBanTaiLieu=@id FOR UPDATE", ct, ("id", Id(detail, "IdBanTaiLieu")));
        var candidates = await Candidates(s, reader["MaDinhDanhTruong"]!.ToString()!, ct);
        var candidate = candidates.FirstOrDefault(x => x.DetailId == q.DetailId && x.ViolationType == q.ViolationType) ?? throw new InvalidOperationException("Không còn căn cứ vi phạm hoặc khoản này đã được lập/miễn xử lý; tải lại danh sách.");
        decimal amount; string type = q.ViolationType; string basis;
        bool staffNotice = type == "QUA_HAN" && candidate.ReaderType != "SINH_VIEN";
        if (type == "QUA_HAN")
        {
            if (!staffNotice && !Rules.CalendarConfirmed) throw new InvalidOperationException("Chưa xác nhận lịch lễ/Tết miễn phạt trên server; chưa được chốt phạt sinh viên.");
            amount = staffNotice ? 0 : candidate.ChargeableDays * 2000m;
            basis = candidate.CalculationNote + " " + q.Description.Trim();
        }
        else if (type == "MAT_SACH")
        {
            if (q.SettlementMethod == "DEN_SACH") { type = "PHI_XU_LY"; amount = 20000; basis = "Đã nhận sách thay thế; phí xử lý kỹ thuật 20.000đ. " + q.Description.Trim(); }
            else if (q.SettlementMethod == "DEN_TIEN")
            {
                decimal price = candidate.BookPrice;
                if (!candidate.HasBookPrice)
                {
                    if (role != "QUAN_LY_THU_VIEN") throw new UnauthorizedAccessException("Sách không ghi giá: Quản lý thư viện phải xác định giá sách tương đương.");
                    if (q.AssessedEquivalentPrice is null or <= 0 or > 499999999999m) throw new ArgumentException("Nhập giá sách tương đương đã được Quản lý xác định.");
                    price = q.AssessedEquivalentPrice.Value;
                }
                amount = price * 2; basis = $"Đền tiền: 2 × {price:N0}đ = {amount:N0}đ. " + q.Description.Trim();
            }
            else throw new ArgumentException("Chọn đền tiền hoặc đền sách.");
        }
        else if (type == "HONG_TRANG")
        {
            if (role != "QUAN_LY_THU_VIEN") throw new UnauthorizedAccessException("Chi phí tái tạo/thay thế trang do Quản lý thư viện xác định.");
            if (q.Amount <= 0 || q.Amount > 999999999999m || decimal.Truncate(q.Amount) != q.Amount) throw new ArgumentException("Nhập chi phí tái tạo/thay thế trang hợp lệ.");
            amount = q.Amount; basis = "Chi phí tái tạo/thay thế trang theo quyết định thư viện. " + q.Description.Trim();
        }
        else throw new ArgumentException("Loại vi phạm không hợp lệ.");
        if (amount > 999999999999m || decimal.Truncate(amount) != amount) throw new ArgumentException("Số tiền vượt giới hạn lưu trữ hoặc không phải số nguyên đồng.");
        string status = amount == 0 ? "DA_XU_LY" : "CHUA_XU_LY";
        int id = await s.Execute("INSERT INTO ViPham(IdDocGia,IdChiTietMuon,LoaiViPham,SoTien,CanCu,ThoiDiemLap,IdNguoiLap,TrangThai) VALUES(@reader,@detail,@type,@amount,@basis,@now,@actor,@status)", ct, ("reader", readerId), ("detail", q.DetailId), ("type", type), ("amount", amount), ("basis", basis), ("now", Now), ("actor", actor), ("status", status));
        await s.Audit(actor, staffNotice ? "LAP_THONG_BAO_DON_VI" : "LAP_VI_PHAM", "ViPham", id.ToString(), null, new { q.DetailId, Type = type, Amount = amount, Basis = basis, candidate.ChargeableDays }, ct);
        return new() { ViolationId = id, ReaderCode = candidate.ReaderCode, ReaderName = candidate.ReaderName, ViolationType = staffNotice ? "THONG_BAO_DON_VI" : type, Amount = amount, Status = status, Description = basis };
    }
    private static async Task<List<UnpaidViolationItem>> Unpaid(CatalogStore s, string keyword, CancellationToken ct)
        => Map<UnpaidViolationItem>(await s.Rows("""
            SELECT v.IdViPham ViolationId,v.IdDocGia ReaderId,d.MaDinhDanhTruong ReaderCode,d.HoTen ReaderName,v.LoaiViPham ViolationType,
            v.SoTien Amount,v.CanCu Description,v.ThoiDiemLap CreatedAt,COALESCE(b.MaVach,'') Barcode,COALESCE(t.NhanDe,'') TitleName,
            COALESCE(CAST((SELECT JSON_UNQUOTE(n.ChiTiet->'$.after.chargeableDays') FROM NhatKy n WHERE n.BangDuLieu='ViPham' AND n.KhoaBanGhi=CAST(v.IdViPham AS CHAR) AND n.HanhDong='LAP_VI_PHAM' ORDER BY n.IdNhatKy DESC LIMIT 1) AS UNSIGNED),0) OverdueDays
            FROM ViPham v JOIN DocGia d USING(IdDocGia) LEFT JOIN ChiTietPhieuMuon c USING(IdChiTietMuon)
            LEFT JOIN BanTaiLieu b ON b.IdBanTaiLieu=c.IdBanTaiLieu LEFT JOIN DauTaiLieu t ON t.IdDauTaiLieu=c.IdDauTaiLieu
            WHERE v.TrangThai='CHUA_XU_LY' AND v.SoTien>0 AND (d.MaDinhDanhTruong LIKE @term ESCAPE '!' OR d.HoTen LIKE @term ESCAPE '!') ORDER BY v.IdViPham LIMIT 100
            """, ct, ("term", Term(keyword))));
    private async Task<FineReceiptResult> Collect(CatalogStore s, CollectFineRequest q, int actor, string name, CancellationToken ct)
    {
        if (q.ViolationIds is null || q.ViolationIds.Count is < 1 or > 100 || q.ViolationIds.Distinct().Count() != q.ViolationIds.Count) throw new ArgumentException("Chọn 1–100 khoản khác nhau.");
        if (q.PaymentMethod is not ("TIEN_MAT" or "CHUYEN_KHOAN") || q.ReferenceNote?.Length > 500) throw new ArgumentException("Hình thức thu/ghi chú không hợp lệ.");
        var refs = new List<Dictionary<string, object?>>();
        foreach (int id in q.ViolationIds.Order())
        {
            var rows = await s.Rows("SELECT IdDocGia FROM ViPham WHERE IdViPham=@id", ct, ("id", id));
            if (rows.Count != 1) throw new InvalidOperationException("Có khoản vi phạm không tồn tại."); refs.Add(rows[0]);
        }
        int readerId = Id(refs[0], "IdDocGia");
        if (refs.Any(x => Id(x, "IdDocGia") != readerId)) throw new ArgumentException("Một phiếu thu chỉ thu cho một độc giả.");
        var reader = await Reader(s, readerId, ct); decimal total = 0; var basis = new List<string>();
        foreach (int id in q.ViolationIds.Order())
        {
            var row = (await s.Rows("SELECT * FROM ViPham WHERE IdViPham=@id FOR UPDATE", ct, ("id", id))).Single();
            if (row["TrangThai"]?.ToString() != "CHUA_XU_LY" || row["IdPhieuThu"] is not null || Convert.ToDecimal(row["SoTien"]) <= 0) throw new InvalidOperationException("Khoản đã thu/hủy hoặc không có tiền cần thu; tải lại danh sách.");
            total += Convert.ToDecimal(row["SoTien"]); basis.Add($"VP#{id}: {row["CanCu"]}");
        }
        if (total > 999999999999m) throw new ArgumentException("Tổng tiền vượt giới hạn lưu trữ.");
        DateTime now = Now;
        int receipt = await s.Execute("INSERT INTO PhieuThu(IdDocGia,IdNguoiThu,HinhThuc,SoTienThu,ThoiDiemThu) VALUES(@reader,@actor,@method,@total,@now)", ct, ("reader", readerId), ("actor", actor), ("method", q.PaymentMethod), ("total", total), ("now", now));
        foreach (int id in q.ViolationIds.Order()) await s.Execute("UPDATE ViPham SET TrangThai='DA_XU_LY',IdPhieuThu=@receipt WHERE IdViPham=@id", ct, ("receipt", receipt), ("id", id));
        await s.Audit(actor, "THU_TIEN", "PhieuThu", receipt.ToString(), null, new { q.ViolationIds, Total = total, q.ReferenceNote }, ct);
        return new() { ReceiptId = receipt, ReceiptCode = receipt.ToString(), ReaderId = readerId, ReaderCode = reader["MaDinhDanhTruong"]!.ToString(), ReaderName = reader["HoTen"]!.ToString(), CollectorName = name, CollectedAt = now, PaymentMethod = q.PaymentMethod, TotalAmount = total, ItemCount = q.ViolationIds.Count, BasisSummary = string.Join("; ", basis), ReaderUnlocked = false };
    }
    private static async Task<List<ViolationLookupItem>> Violations(CatalogStore s, CirculationQuery q, CancellationToken ct)
        => Map<ViolationLookupItem>(await s.Rows("""
            SELECT v.IdViPham ViolationId,v.IdDocGia ReaderId,d.MaDinhDanhTruong ReaderCode,d.HoTen ReaderName,v.LoaiViPham ViolationType,v.SoTien Amount,
            v.TrangThai Status,v.IdPhieuThu ReceiptId,v.ThoiDiemLap CreatedAt,CONCAT(v.CanCu,COALESCE(CONCAT(' | Hủy: ',v.LyDoHuy),'')) Description
            FROM ViPham v JOIN DocGia d USING(IdDocGia) WHERE (@all=1 OR v.TrangThai<>'DA_HUY')
            AND (d.MaDinhDanhTruong LIKE @term ESCAPE '!' OR d.HoTen LIKE @term ESCAPE '!' OR CAST(v.IdViPham AS CHAR) LIKE @term ESCAPE '!') ORDER BY v.IdViPham DESC LIMIT 100
            """, ct, ("all", q.Flag), ("term", Term(q.Keyword))));
    private async Task<VoidViolationResult> Void(CatalogStore s, VoidViolationRequest q, int actor, string name, CancellationToken ct)
    {
        Need(q.Reason, 300, "Lý do hủy");
        var refs = await s.Rows("SELECT IdDocGia FROM ViPham WHERE IdViPham=@id", ct, ("id", q.ViolationId));
        if (refs.Count != 1) throw new InvalidOperationException("Không tìm thấy phiếu phạt.");
        var reader = await Reader(s, Id(refs[0], "IdDocGia"), ct);
        var row = (await s.Rows("SELECT * FROM ViPham WHERE IdViPham=@id FOR UPDATE", ct, ("id", q.ViolationId))).Single();
        if (row["TrangThai"]?.ToString() == "DA_HUY") throw new InvalidOperationException("Phiếu đã bị hủy hiệu lực.");
        await s.Execute("UPDATE ViPham SET TrangThai='DA_HUY',IdNguoiHuy=@actor,LyDoHuy=@reason WHERE IdViPham=@id", ct, ("actor", actor), ("reason", q.Reason.Trim()), ("id", q.ViolationId));
        return new() { ViolationId = q.ViolationId, ReaderCode = reader["MaDinhDanhTruong"]!.ToString(), ReaderName = reader["HoTen"]!.ToString(), Amount = Convert.ToDecimal(row["SoTien"]), PreviousStatus = row["TrangThai"]!.ToString(), HasExistingReceipt = row["IdPhieuThu"] is not null, ReceiptId = row["IdPhieuThu"] is null ? null : Id(row, "IdPhieuThu"), ManagerName = name, VoidedAt = Now, Reason = q.Reason.Trim(), ReaderUnlocked = false };
    }
}
