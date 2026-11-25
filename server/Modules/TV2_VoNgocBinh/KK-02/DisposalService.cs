namespace ThuVienDAU.Catalog;
internal static class DisposalService
{
    internal static async Task<TableResult> List(CatalogStore s,CatalogQuery q,CancellationToken ct)
    {
        var term=WorkRules.Like(q);return WorkRules.Table(await s.Rows("SELECT t.IdThanhLy,t.IdBanTaiLieu,b.MaVach,t.IdKiemKe,t.LyDo,t.TrangThai,t.IdNguoiDeNghi,t.IdNguoiXuLy,t.ThoiDiemXuLy FROM ThanhLy t JOIN BanTaiLieu b USING(IdBanTaiLieu) WHERE b.MaVach LIKE @q ESCAPE '!' OR t.LyDo LIKE @q ESCAPE '!' ORDER BY t.IdThanhLy DESC LIMIT 100 OFFSET @off",ct,("q",term),("off",q.Offset)),"IdThanhLy","IdBanTaiLieu","MaVach","IdKiemKe","LyDo","TrangThai","IdNguoiDeNghi","IdNguoiXuLy","ThoiDiemXuLy");
    }
    private static async Task CheckCopy(CatalogStore s,int id,CancellationToken ct)
    {
        var b=await s.Rows("SELECT TrangThai FROM BanTaiLieu WHERE IdBanTaiLieu=@id FOR UPDATE",ct,("id",id));
        if(b.Count==0||b[0]["TrangThai"] as string is "DANG_MUON" or "THANH_LY"||(await s.Rows("SELECT IdChiTietMuon FROM ChiTietPhieuMuon WHERE IdBanTaiLieu=@id AND TrangThai='DANG_MUON'",ct,("id",id))).Count>0)throw new InvalidOperationException("Bản sách không tồn tại, đã thanh lý hoặc đang được mượn.");
    }
    internal static async Task<object> Create(CatalogStore s,DisposalCreate q,int actor,string role,CancellationToken ct)
    {
        if(role!="BIEN_MUC")throw new UnauthorizedAccessException("Chỉ Biên mục lập đề nghị thanh lý.");WorkRules.Need(q.LyDo,500,"Lý do đề nghị");
        await CheckCopy(s,q.IdBanTaiLieu,ct);
        if(q.IdKiemKe.HasValue&&(await s.Rows("SELECT c.IdChiTietKiemKe FROM ChiTietKiemKe c JOIN KiemKe k USING(IdKiemKe) WHERE c.IdKiemKe=@stock AND c.IdBanTaiLieu=@copy AND k.TrangThai='HOAN_TAT'",ct,("stock",q.IdKiemKe),("copy",q.IdBanTaiLieu))).Count==0)throw new InvalidOperationException("Bản sách không thuộc đợt kiểm kê đã hoàn tất.");
        var id=await s.Execute("INSERT INTO ThanhLy(IdBanTaiLieu,IdKiemKe,LyDo,IdNguoiDeNghi) VALUES(@copy,@stock,@reason,@actor)",ct,("copy",q.IdBanTaiLieu),("stock",q.IdKiemKe),("reason",q.LyDo.Trim()),("actor",actor));
        await s.Audit(actor,"TV2_DISPOSAL_CREATE","ThanhLy",id.ToString(),null,q,ct);return new{IdThanhLy=id};
    }
    internal static async Task<object> Decide(CatalogStore s,DisposalDecision q,int actor,string role,CancellationToken ct)
    {
        if(role!="QUAN_LY_THU_VIEN")throw new UnauthorizedAccessException("Chỉ Quản lý thư viện duyệt/chốt thanh lý.");WorkRules.Need(q.CanCu,500,"Căn cứ quyết định");
        var rows=await s.Rows("SELECT * FROM ThanhLy WHERE IdThanhLy=@id FOR UPDATE",ct,("id",q.IdThanhLy));
        if(rows.Count!=1||rows[0]["TrangThai"] as string!="DE_NGHI")throw new InvalidOperationException("Đề nghị không tồn tại hoặc đã xử lý.");
        var copy=Convert.ToInt32(rows[0]["IdBanTaiLieu"]);
        if(q.ChapNhan){await CheckCopy(s,copy,ct);await s.Execute("UPDATE BanTaiLieu SET TrangThai='THANH_LY' WHERE IdBanTaiLieu=@copy",ct,("copy",copy));}
        var state=q.ChapNhan?"DA_THANH_LY":"TU_CHOI";
        await s.Execute("UPDATE ThanhLy SET TrangThai=@state,IdNguoiXuLy=@actor,ThoiDiemXuLy=NOW() WHERE IdThanhLy=@id",ct,("state",state),("actor",actor),("id",q.IdThanhLy));
        await s.Audit(actor,"TV2_DISPOSAL_DECIDE","ThanhLy",q.IdThanhLy.ToString(),rows[0],q,ct);return new{q.IdThanhLy,TrangThai=state};
    }
}
