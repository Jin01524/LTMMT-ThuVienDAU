namespace ThuVienDAU.Catalog;
internal static class StocktakeService
{
    internal static async Task<TableResult> List(CatalogStore s,CatalogQuery q,CancellationToken ct)
    {
        var term=WorkRules.Like(q);
        return WorkRules.Table(await s.Rows("SELECT k.IdKiemKe,k.TenKiemKe,k.TrangThai,k.ThoiDiemBatDau,k.ThoiDiemKetThuc,COUNT(c.IdChiTietKiemKe) SoDong,COALESCE(SUM(c.CoTrongDanhSach=1),0) DuKien,COALESCE(SUM(c.KetQua='THIEU'),0) Thieu FROM KiemKe k LEFT JOIN ChiTietKiemKe c USING(IdKiemKe) WHERE k.TenKiemKe LIKE @q ESCAPE '!' GROUP BY k.IdKiemKe ORDER BY k.IdKiemKe DESC LIMIT 100 OFFSET @off",ct,("q",term),("off",q.Offset)),"IdKiemKe","TenKiemKe","TrangThai","ThoiDiemBatDau","ThoiDiemKetThuc","SoDong","DuKien","Thieu");
    }
    private static async Task<Dictionary<string,object?>> Lock(CatalogStore s,int id,CancellationToken ct)
    {
        var rows=await s.Rows("SELECT * FROM KiemKe WHERE IdKiemKe=@id FOR UPDATE",ct,("id",id));return rows.Count==1?rows[0]:throw new InvalidOperationException("Không tìm thấy đợt kiểm kê.");
    }
    internal static async Task<object> Create(CatalogStore s,StocktakeCreate q,int actor,CancellationToken ct)
    {
        WorkRules.Need(q.TenKiemKe,200,"Tên đợt kiểm kê");
        if(q.IdViTri is <=0)throw new InvalidOperationException("Vị trí không hợp lệ.");
        if(q.IdViTri.HasValue&&(await s.Rows("SELECT IdViTri FROM ViTriLuuTru WHERE IdViTri=@id",ct,("id",q.IdViTri))).Count==0)throw new InvalidOperationException("Không tìm thấy vị trí.");
        // Snapshot and lock physical copies in the same order as circulation (copy before loan).
        var expected=await s.Rows("SELECT b.IdBanTaiLieu,b.IdViTri FROM BanTaiLieu b WHERE b.TrangThai NOT IN ('DANG_MUON','THANH_LY') AND (@loc IS NULL OR b.IdViTri=@loc) AND NOT EXISTS(SELECT 1 FROM ChiTietPhieuMuon c WHERE c.IdBanTaiLieu=b.IdBanTaiLieu AND c.TrangThai='DANG_MUON') ORDER BY b.IdBanTaiLieu FOR UPDATE",ct,("loc",q.IdViTri));
        if(expected.Count==0)throw new InvalidOperationException("Không có bản sách phù hợp để lập danh sách.");
        var id=await s.Execute("INSERT INTO KiemKe(TenKiemKe,IdNguoiLap) VALUES(@name,@actor)",ct,("name",q.TenKiemKe.Trim()),("actor",actor));
        foreach(var copy in expected)await s.Execute("INSERT INTO ChiTietKiemKe(IdKiemKe,IdBanTaiLieu,IdViTriDuKien) VALUES(@id,@copy,@loc)",ct,("id",id),("copy",copy["IdBanTaiLieu"]),("loc",copy["IdViTri"]));
        var result=new {IdKiemKe=id,SoBanDuKien=expected.Count};await s.Audit(actor,"TV2_STOCK_CREATE","KiemKe",id.ToString(),null,result,ct);return result;
    }
    internal static async Task<TableResult> Detail(CatalogStore s,StocktakeQuery q,CancellationToken ct)
    {
        if(q.Offset<0||q.Offset>1000000)throw new InvalidOperationException("Trang không hợp lệ.");await Lock(s,q.IdKiemKe,ct);
        return WorkRules.Table(await s.Rows("SELECT c.IdChiTietKiemKe,c.IdBanTaiLieu,b.SoDangKyCaBiet,COALESCE(c.MaVachQuet,b.MaVach) MaVach,c.CoTrongDanhSach,c.IdViTriDuKien,c.IdViTriThucTe,c.KetQua,c.IdNguoiKiemKe,c.ThoiDiemKiem FROM ChiTietKiemKe c LEFT JOIN BanTaiLieu b USING(IdBanTaiLieu) WHERE c.IdKiemKe=@id ORDER BY c.IdChiTietKiemKe LIMIT 100 OFFSET @off",ct,("id",q.IdKiemKe),("off",q.Offset)),"IdChiTietKiemKe","IdBanTaiLieu","SoDangKyCaBiet","MaVach","CoTrongDanhSach","IdViTriDuKien","IdViTriThucTe","KetQua","IdNguoiKiemKe","ThoiDiemKiem");
    }
    internal static async Task<object> Action(CatalogStore s,StocktakeAction q,int actor,CancellationToken ct)
    {
        var row=await Lock(s,q.IdKiemKe,ct);var old=(string)row["TrangThai"]!;
        if(!(old=="DU_KIEN"&&q.TrangThai is "DANG_KIEM" or "HUY"||old=="DANG_KIEM"&&q.TrangThai is "HOAN_TAT" or "HUY"))throw new InvalidOperationException("Chuyển trạng thái kiểm kê không hợp lệ.");
        if(q.TrangThai=="HOAN_TAT")
        {
            var copies=await s.Rows("SELECT b.IdBanTaiLieu FROM BanTaiLieu b JOIN ChiTietKiemKe c USING(IdBanTaiLieu) WHERE c.IdKiemKe=@id AND c.KetQua='CHUA_KIEM' ORDER BY b.IdBanTaiLieu FOR UPDATE",ct,("id",q.IdKiemKe));
            foreach(var copy in copies)if((await s.Rows("SELECT IdChiTietMuon FROM ChiTietPhieuMuon WHERE IdBanTaiLieu=@copy AND TrangThai='DANG_MUON'",ct,("copy",copy["IdBanTaiLieu"]))).Count>0)throw new InvalidOperationException("Có bản chưa kiểm đang được mượn. Đợi trả/kiểm hoặc hủy đợt; không tự ghi thiếu.");
            await s.Execute("UPDATE ChiTietKiemKe SET KetQua='THIEU',IdNguoiKiemKe=@actor,ThoiDiemKiem=NOW() WHERE IdKiemKe=@id AND CoTrongDanhSach=1 AND KetQua='CHUA_KIEM'",ct,("actor",actor),("id",q.IdKiemKe));
        }
        await s.Execute("UPDATE KiemKe SET TrangThai=@state,ThoiDiemKetThuc=IF(@state IN ('HOAN_TAT','HUY'),NOW(),NULL) WHERE IdKiemKe=@id",ct,("state",q.TrangThai),("id",q.IdKiemKe));
        await s.Audit(actor,"TV2_STOCK_ACTION","KiemKe",q.IdKiemKe.ToString(),row,q,ct);return new {q.IdKiemKe,q.TrangThai};
    }
    internal static async Task<object> Scan(CatalogStore s,StocktakeScan q,int actor,CancellationToken ct)
    {
        WorkRules.Need(q.MaVach,100,"Barcode");var stock=await Lock(s,q.IdKiemKe,ct);
        if(stock["TrangThai"] as string!="DANG_KIEM")throw new InvalidOperationException("Chỉ quét khi đợt đang kiểm.");
        if((await s.Rows("SELECT IdViTri FROM ViTriLuuTru WHERE IdViTri=@loc",ct,("loc",q.IdViTriThucTe))).Count==0)throw new InvalidOperationException("Vị trí thực tế không tồn tại.");
        var books=await s.Rows("SELECT IdBanTaiLieu FROM BanTaiLieu WHERE MaVach=@barcode FOR UPDATE",ct,("barcode",q.MaVach.Trim()));
        int? copy=books.Count==0?null:Convert.ToInt32(books[0]["IdBanTaiLieu"]);
        var rows=await s.Rows("SELECT * FROM ChiTietKiemKe WHERE IdKiemKe=@id AND ((@copy IS NOT NULL AND IdBanTaiLieu=@copy) OR MaVachQuet=@barcode) FOR UPDATE",ct,("id",q.IdKiemKe),("copy",copy),("barcode",q.MaVach.Trim()));
        if(rows.Count>0&&rows[0]["KetQua"] as string!="CHUA_KIEM")throw new InvalidOperationException("Barcode đã được ghi nhận trong đợt này.");
        var expected=rows.Count>0&&Convert.ToBoolean(rows[0]["CoTrongDanhSach"]);
        var result=!expected?"THUA":q.HuHong?"HU_HONG":Convert.ToInt32(rows[0]["IdViTriDuKien"]??0)==q.IdViTriThucTe?"KHOP":"SAI_VI_TRI";
        if(rows.Count==0)await s.Execute("INSERT INTO ChiTietKiemKe(IdKiemKe,IdBanTaiLieu,MaVachQuet,CoTrongDanhSach,IdViTriThucTe,KetQua,IdNguoiKiemKe,ThoiDiemKiem) VALUES(@id,@copy,@barcode,0,@loc,@result,@actor,NOW())",ct,("id",q.IdKiemKe),("copy",copy),("barcode",q.MaVach.Trim()),("loc",q.IdViTriThucTe),("result",result),("actor",actor));
        else await s.Execute("UPDATE ChiTietKiemKe SET MaVachQuet=@barcode,IdViTriThucTe=@loc,KetQua=@result,IdNguoiKiemKe=@actor,ThoiDiemKiem=NOW() WHERE IdChiTietKiemKe=@id",ct,("id",rows[0]["IdChiTietKiemKe"]),("barcode",q.MaVach.Trim()),("loc",q.IdViTriThucTe),("result",result),("actor",actor));
        await s.Audit(actor,"TV2_STOCK_SCAN","KiemKe",q.IdKiemKe.ToString(),rows.FirstOrDefault(),new{q.MaVach,KetQua=result,q.IdViTriThucTe,q.HuHong},ct);return new{KetQua=result};
    }
}
