namespace ThuVienDAU.Catalog;
internal static class StockReportService
{
    internal static async Task<TableResult> Get(CatalogStore s,ReportFilter q,CancellationToken ct)
    {
        if(q.TuNgay>q.DenNgay||q.MaDDC?.Length>64||q.LoaiTaiLieu is not (null or "" or "SACH" or "DO_AN" or "LUAN_VAN" or "KHAC"))throw new InvalidOperationException("Bộ lọc báo cáo không hợp lệ.");
        var rows=await s.Rows("SELECT COALESCE(d.MaDDC,'CHUA_PHAN_LOAI') MaDDC,d.LoaiTaiLieu,COUNT(DISTINCT d.IdDauTaiLieu) SoDau,COUNT(b.IdBanTaiLieu) SoBan,COALESCE(SUM(b.TrangThai='SAN_SANG'),0) SanSang,COALESCE(SUM(b.TrangThai='DANG_MUON'),0) DangMuon,COALESCE(SUM(b.TrangThai='THANH_LY'),0) ThanhLy,COUNT(DISTINCT CASE WHEN m.TrangThai='DA_KIEM_TRA' AND m.PhienBanDauTaiLieu=d.PhienBan THEN d.IdDauTaiLieu END) MARCDaKiem FROM DauTaiLieu d LEFT JOIN BanTaiLieu b ON b.IdDauTaiLieu=d.IdDauTaiLieu LEFT JOIN BieuGhiMARC m ON m.IdDauTaiLieu=d.IdDauTaiLieu WHERE (@ddc IS NULL OR d.MaDDC=@ddc) AND (@type IS NULL OR d.LoaiTaiLieu=@type) AND (@from IS NULL OR b.NgayNhap>=@from) AND (@to IS NULL OR b.NgayNhap<=@to) GROUP BY d.MaDDC,d.LoaiTaiLieu ORDER BY d.MaDDC,d.LoaiTaiLieu",ct,("ddc",string.IsNullOrWhiteSpace(q.MaDDC)?null:q.MaDDC),("type",string.IsNullOrWhiteSpace(q.LoaiTaiLieu)?null:q.LoaiTaiLieu),("from",q.TuNgay?.ToDateTime(TimeOnly.MinValue)),("to",q.DenNgay?.ToDateTime(TimeOnly.MinValue)));
        return WorkRules.Table(rows,"MaDDC","LoaiTaiLieu","SoDau","SoBan","SanSang","DangMuon","ThanhLy","MARCDaKiem");
    }
}
