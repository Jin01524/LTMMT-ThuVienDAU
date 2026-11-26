using System.Text.Json;
using MySql.Data.MySqlClient;
using ThuVienDAU.Catalog;
using ThuVienDAU.Configuration;
using ThuVienDAU.Data;
using ThuVienDAU.Protocol;
using ThuVienDAU.Circulation.LT01;
using ThuVienDAU.Circulation.LT02;
using ThuVienDAU.Circulation.LT03;
using ThuVienDAU.Circulation.LT04;
using ThuVienDAU.Circulation.VP01;
using ThuVienDAU.Circulation.VP02;
using ThuVienDAU.Circulation.VP03;
using ThuVienDAU.Circulation.PH01;

namespace ThuVienDAU.Circulation;

public sealed partial class CirculationService(ICatalogSessionResolver sessions, TimeProvider? clock = null, CirculationRules? rules = null, DatabaseOptions? options = null)
{
    private DateTime Now => (clock ?? TimeProvider.System).GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime;
    private CirculationRules Rules => rules ?? CirculationRules.Load();
    private static T Read<T>(TcpRequest r) where T : class => r.Payload?.Deserialize<T>(TcpProtocol.Json) ?? throw new ArgumentException("Thiếu dữ liệu yêu cầu.");
    private static T Map<T>(Dictionary<string, object?> row) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(row, TcpProtocol.Json), TcpProtocol.Json)!;
    private static List<T> Map<T>(List<Dictionary<string, object?>> rows) => rows.Select(Map<T>).ToList();
    private static int Id(Dictionary<string, object?> r, string name) => Convert.ToInt32(r[name]);
    private static void Need(string? value, int max, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > max) throw new ArgumentException(name + " bị thiếu hoặc quá dài.");
    }
    private static string Term(string? text)
    {
        if (text?.Length > 100) throw new ArgumentException("Từ khóa tối đa 100 ký tự.");
        return "%" + (text ?? "").Trim().Replace("!", "!!").Replace("%", "!%").Replace("_", "!_") + "%";
    }
    private static async Task<Dictionary<string, object?>> Reader(CatalogStore s, int id, CancellationToken ct)
    {
        var rows = await s.Rows("SELECT * FROM DocGia WHERE IdDocGia=@id FOR UPDATE", ct, ("id", id));
        return rows.Count == 1 ? rows[0] : throw new InvalidOperationException("Không tìm thấy hồ sơ độc giả.");
    }
    private static async Task<Dictionary<string, object?>> Detail(CatalogStore s, int id, CancellationToken ct)
    {
        var rows = await s.Rows("SELECT * FROM ChiTietPhieuMuon WHERE IdChiTietMuon=@id FOR UPDATE", ct, ("id", id));
        return rows.Count == 1 ? rows[0] : throw new InvalidOperationException("Không tìm thấy khoản mượn.");
    }
    private static async Task<int> DetailReader(CatalogStore s, int id, CancellationToken ct)
    {
        var rows = await s.Rows("SELECT IdDocGia FROM ChiTietPhieuMuon WHERE IdChiTietMuon=@id", ct, ("id", id));
        return rows.Count == 1 ? Id(rows[0], "IdDocGia") : throw new InvalidOperationException("Khoản mượn không tồn tại.");
    }
    private async Task CheckReader(CatalogStore s, Dictionary<string, object?> reader, bool renewing, CancellationToken ct)
    {
        if (reader["TrangThaiHoSo"]?.ToString() != "HOAT_DONG" || !Convert.ToBoolean(reader["DuocMuon"])) throw new InvalidOperationException("Hồ sơ hoặc quyền mượn đang bị khóa; liên hệ Quản lý thư viện.");
        int id = Id(reader, "IdDocGia");
        var debt = await s.Rows("SELECT IdViPham FROM ViPham WHERE IdDocGia=@id AND TrangThai='CHUA_XU_LY' AND SoTien>0 LIMIT 1", ct, ("id", id));
        if (debt.Count > 0) throw new InvalidOperationException("Cần thanh toán khoản phạt/bồi thường chưa xử lý.");
        var overdue = await s.Rows("SELECT IdChiTietMuon FROM ChiTietPhieuMuon WHERE IdDocGia=@id AND TrangThai='DANG_MUON' AND HanTra<@now LIMIT 1", ct, ("id", id), ("now", Now));
        if (overdue.Count > 0) throw new InvalidOperationException("Cần xử lý khoản mượn quá hạn trước khi mượn/gia hạn tiếp.");
        var pending = await s.Rows("""
            SELECT c.IdChiTietMuon FROM ChiTietPhieuMuon c WHERE c.IdDocGia=@id AND (
              (c.TrangThai='MAT' AND NOT EXISTS(SELECT 1 FROM ViPham v WHERE v.IdChiTietMuon=c.IdChiTietMuon AND v.LoaiViPham IN ('MAT_SACH','PHI_XU_LY')))
              OR (c.TrangThai='DA_TRA' AND EXISTS(SELECT 1 FROM NhatKy n WHERE n.BangDuLieu='ChiTietPhieuMuon' AND n.KhoaBanGhi=CAST(c.IdChiTietMuon AS CHAR) AND n.HanhDong='TRA_SACH' AND JSON_UNQUOTE(n.ChiTiet->'$.after.condition')='HU_HONG') AND NOT EXISTS(SELECT 1 FROM ViPham v WHERE v.IdChiTietMuon=c.IdChiTietMuon AND v.LoaiViPham='HONG_TRANG'))
              OR (@student=1 AND c.ThoiDiemTra>c.HanTra AND NOT EXISTS(SELECT 1 FROM ViPham v WHERE v.IdChiTietMuon=c.IdChiTietMuon AND v.LoaiViPham='QUA_HAN')))
            LIMIT 1
            """, ct, ("id", id), ("student", reader["LoaiDocGia"]?.ToString() == "SINH_VIEN"));
        if (pending.Count > 0) throw new InvalidOperationException("Có sách mất/hỏng hoặc trả trễ đang chờ lập và xử lý vi phạm.");
        var active = await s.Rows("SELECT IdDauTaiLieu FROM ChiTietPhieuMuon WHERE IdDocGia=@id AND TrangThai='DANG_MUON'", ct, ("id", id));
        int max = reader["LoaiDocGia"]?.ToString() == "SINH_VIEN" ? 2 : 3;
        if (active.Count > max || !renewing && active.Count == max) throw new InvalidOperationException("Đã đạt/vượt hạn mức mượn.");
    }
    public async Task<TcpResponse> HandleAsync(TcpRequest request, CancellationToken ct)
    {
        try
        {
            if (request.Version != 1 || string.IsNullOrWhiteSpace(request.RequestId) || request.RequestId.Length > 64) throw new ArgumentException("Phiên bản/mã yêu cầu không hợp lệ.");
            int actor = sessions.Resolve(request.SessionToken) ?? throw new UnauthorizedAccessException("Cần đăng nhập; phiên đã hết hạn hoặc đã đăng xuất.");
            await using var db = await new MySqlConnectionFactory(options ?? DatabaseOptions.Load()).OpenAsync(ct);
            await using var tx = await db.BeginTransactionAsync(ct);
            var s = new CatalogStore(db, tx);
            // One stable gate for room/key handovers; acquired before actor/reader locks.
            if (request.Command == "TV3_VISIT_SCAN") await s.Rows("SELECT IdTaiKhoan FROM TaiKhoan ORDER BY IdTaiKhoan LIMIT 1 FOR UPDATE", ct);
            var accounts = await s.Rows("SELECT VaiTro,IdDocGia,TenDangNhap FROM TaiKhoan WHERE IdTaiKhoan=@id AND DangHoatDong=1 FOR UPDATE", ct, ("id", actor));
            if (accounts.Count != 1) throw new UnauthorizedAccessException("Tài khoản đã khóa hoặc không tồn tại.");
            string role = accounts[0]["VaiTro"]!.ToString()!;
            if (role == "DOC_GIA" && accounts[0]["IdDocGia"] is null) throw new UnauthorizedAccessException("Tài khoản chưa liên kết hồ sơ độc giả.");
            bool staff = role is "THU_THU" or "QUAN_LY_THU_VIEN";
            bool feedback = request.Command is "TV3_FEEDBACK_SUBMIT" or "TV3_FEEDBACK_LIST";
            if (!staff && !(feedback && role == "DOC_GIA")) throw new UnauthorizedAccessException("Không có quyền sử dụng chức năng này.");
            if (request.Command == "TV3_VIOLATION_VOID" && role != "QUAN_LY_THU_VIEN") throw new UnauthorizedAccessException("Chỉ Quản lý thư viện được hủy hiệu lực phiếu phạt.");
            object result = request.Command switch
            {
                "TV3_READER_LOOKUP" => await LookupReader(s, Read<CirculationQuery>(request).Keyword, ct),
                "TV3_COPY_LOOKUP" => await LookupCopy(s, Read<CirculationQuery>(request).Keyword, ct),
                "TV3_BORROW" => await Borrow(s, Read<BorrowCommand>(request), actor, ct),
                "TV3_RETURN_LOOKUP" => await ReturnLookup(s, Read<CirculationQuery>(request).Keyword, ct),
                "TV3_RETURN" => await Return(s, Read<ReturnCommand>(request), actor, ct),
                "TV3_RENEW_LIST" => await RenewList(s, Read<CirculationQuery>(request).Keyword, ct),
                "TV3_RENEW" => await Renew(s, Read<RenewCommand>(request), actor, ct),
                "TV3_HISTORY" => await History(s, Read<LoanHistoryFilter>(request), ct),
                "TV3_VIOLATION_CANDIDATES" => await Candidates(s, Read<CirculationQuery>(request).Keyword, ct),
                "TV3_VIOLATION_CREATE" => await CreateViolation(s, Read<CreateViolationInput>(request), actor, role, ct),
                "TV3_UNPAID" => await Unpaid(s, Read<CirculationQuery>(request).Keyword, ct),
                "TV3_COLLECT" => await Collect(s, Read<CollectFineRequest>(request), actor, accounts[0]["TenDangNhap"]!.ToString()!, ct),
                "TV3_VIOLATION_LIST" => await Violations(s, Read<CirculationQuery>(request), ct),
                "TV3_VIOLATION_VOID" => await Void(s, Read<VoidViolationRequest>(request), actor, accounts[0]["TenDangNhap"]!.ToString()!, ct),
                "TV3_VISITS" => await Visits(s, Read<CirculationQuery>(request), ct),
                "TV3_VISIT_SCAN" => await ScanVisit(s, Read<VisitCommand>(request), actor, ct),
                "TV3_FEEDBACK_SUBMIT" => await SubmitFeedback(s, Read<SubmitFeedbackRequest>(request), actor, staff ? null : accounts[0]["IdDocGia"], ct),
                "TV3_FEEDBACK_LIST" => await Feedbacks(s, Read<CirculationQuery>(request), staff ? null : accounts[0]["IdDocGia"], ct),
                "TV3_FEEDBACK_PROCESS" => await ProcessFeedback(s, Read<ProcessFeedbackRequest>(request), actor, ct),
                "TV3_STATISTICS" => await Statistics(s, Read<StatisticsQuery>(request), accounts[0]["TenDangNhap"]!.ToString()!, role, ct),
                "TV3_RECEIPT" => await Receipt(s, Read<ReceiptQuery>(request), ct),
                _ => throw new ArgumentException("Lệnh TV3 không hợp lệ.")
            };
            await tx.CommitAsync(ct);
            return new(1, request.RequestId, true, JsonSerializer.SerializeToElement(result, TcpProtocol.Json));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or ArgumentException or JsonException)
        { return new(1, request.RequestId, false, Error: ex.Message); }
        catch (MySqlException ex)
        {
            string message = ex.Number switch { 1062 => "Thao tác trùng hoặc sách đang được mượn; tải lại dữ liệu.", 1644 => ex.Message, 1205 or 1213 => "Dữ liệu đang được xử lý đồng thời; tải lại và thử lại.", 3819 => "Dữ liệu vi phạm ràng buộc CSDL.", _ => "Không thao tác được CSDL; kiểm tra cấu hình server." };
            return new(1, request.RequestId, false, Error: message, ErrorNumber: ex.Number);
        }
        catch (IOException) { return new(1, request.RequestId, false, Error: "Không đọc được cấu hình nghiệp vụ trên server."); }
    }
}
