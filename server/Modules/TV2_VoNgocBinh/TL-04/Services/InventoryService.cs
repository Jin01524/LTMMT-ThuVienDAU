namespace ThuVienDAU.Catalog;
internal static class InventoryService
{
    internal const string CopySelect="SELECT b.*,d.NhanDe,v.DayKe,v.Ngan FROM BanTaiLieu b JOIN DauTaiLieu d ON d.IdDauTaiLieu=b.IdDauTaiLieu LEFT JOIN ViTriLuuTru v ON v.IdViTri=b.IdViTri";
    internal static async Task<StorageLocationItem> SaveLocation(CatalogStore store,StorageLocationItem item,int actor,CancellationToken ct)
    {
        Need(item.DayKe,30,"Dãy kệ");Need(item.Ngan,30,"Ngăn");
        if(item.IdViTri<0)throw new InvalidOperationException("Vị trí không hợp lệ.");
        var old=await store.Rows("SELECT * FROM ViTriLuuTru WHERE IdViTri=@id FOR UPDATE",ct,("id",item.IdViTri));
        if(item.IdViTri>0 && (old.Count==0 || CatalogStore.Location(old[0]).Revision!=item.Revision))throw new InvalidOperationException("Vị trí đã thay đổi. Tải lại trước khi lưu.");
        var id=await store.Execute(item.IdViTri==0 ? "INSERT INTO ViTriLuuTru(DayKe,Ngan) VALUES(@ke,@ngan)" : "UPDATE ViTriLuuTru SET DayKe=@ke,Ngan=@ngan WHERE IdViTri=@id",ct,("ke",item.DayKe.Trim()),("ngan",item.Ngan.Trim()),("id",item.IdViTri));
        if(item.IdViTri>0)id=item.IdViTri;
        var result=CatalogStore.Location((await store.Rows("SELECT * FROM ViTriLuuTru WHERE IdViTri=@id",ct,("id",id)))[0]);
        await store.Audit(actor,"TV2_LOCATION_SAVE","ViTriLuuTru",id.ToString(),old.Count==0?null:CatalogStore.Location(old[0]),result,ct);
        return result;
    }
    internal static async Task<CopyItem> SaveCopy(CatalogStore store,CopyItem item,int actor,CancellationToken ct)
    {
        Need(item.SoDangKyCaBiet,50,"Số ĐKCB");Need(item.MaVach,100,"Barcode");
        if(item.IdBanTaiLieu<0 || item.IdDauTaiLieu<=0 || item.IdViTri is <=0)throw new InvalidOperationException("Đầu tài liệu/vị trí không hợp lệ.");
        if(item.KyHieuXepGia?.Length>100)throw new InvalidOperationException("Ký hiệu xếp giá tối đa 100 ký tự.");
        if(item.GiaBia is decimal price && (price<0 || price>999999999999m || price!=decimal.Truncate(price)))throw new InvalidOperationException("Giá bìa là số đồng nguyên không âm, tối đa 12 chữ số; có thể để trống.");
        if(!new[]{"DANG_XU_LY","SAN_SANG","HU_HONG"}.Contains(item.TrangThai) || !new[]{"TOT","CU","HU_HONG"}.Contains(item.TinhTrang))throw new InvalidOperationException("TL-04 chỉ xử lý trạng thái đang xử lý, sẵn sàng hoặc hư hỏng; mượn/mất/thanh lý thuộc phân hệ khác.");
        if(item.TrangThai=="SAN_SANG" && (item.IdViTri is null || string.IsNullOrWhiteSpace(item.KyHieuXepGia) || item.TinhTrang=="HU_HONG"))throw new InvalidOperationException("Sách sẵn sàng cần vị trí, ký hiệu xếp giá và không bị hư hỏng.");
        if(item.TrangThai=="HU_HONG" && item.TinhTrang!="HU_HONG")throw new InvalidOperationException("Trạng thái hư hỏng cần tình trạng hư hỏng tương ứng.");
        // Lock copy first: TV3 must take the same lock before checking and writing a loan.
        var old=await store.Rows("SELECT * FROM BanTaiLieu WHERE IdBanTaiLieu=@id FOR UPDATE",ct,("id",item.IdBanTaiLieu));
        if(item.IdBanTaiLieu>0)
        {
            if(old.Count==0 || CatalogStore.Copy(old[0]).Revision!=item.Revision)throw new InvalidOperationException("Bản sách đã thay đổi. Tải lại trước khi lưu.");
            var before=CatalogStore.Copy(old[0]);
            if(before.IdDauTaiLieu!=item.IdDauTaiLieu)throw new InvalidOperationException("Không đổi đầu tài liệu gắn với bản sách đã tạo.");
            if(!new[]{"DANG_XU_LY","SAN_SANG","HU_HONG"}.Contains(before.TrangThai))throw new InvalidOperationException("Bản sách đang mượn, mất/thất lạc hoặc thanh lý không được sửa tại TL-04.");
            if((await store.Rows("SELECT IdChiTietMuon FROM ChiTietPhieuMuon WHERE IdBanTaiLieu=@id AND TrangThai='DANG_MUON'",ct,("id",item.IdBanTaiLieu))).Count>0)throw new InvalidOperationException("Bản sách còn khoản mượn mở; cần hoàn tất mượn–trả trước khi sửa.");
        }
        // Lock referenced rows so the verified title/location cannot disappear before this write.
        if((await store.Rows("SELECT IdDauTaiLieu FROM DauTaiLieu WHERE IdDauTaiLieu=@id FOR UPDATE",ct,("id",item.IdDauTaiLieu))).Count==0)throw new InvalidOperationException("Đầu tài liệu không tồn tại.");
        if(item.IdViTri is int location && (await store.Rows("SELECT IdViTri FROM ViTriLuuTru WHERE IdViTri=@id FOR UPDATE",ct,("id",location))).Count==0)throw new InvalidOperationException("Vị trí không tồn tại.");
        // Explicit parameter list; never construct SQL from client-supplied column names.
        var values=new (string,object?)[]{("id",item.IdBanTaiLieu),("dau",item.IdDauTaiLieu),("dkcb",item.SoDangKyCaBiet.Trim()),("barcode",item.MaVach.Trim()),("vitri",item.IdViTri),("xep",string.IsNullOrWhiteSpace(item.KyHieuXepGia)?null:item.KyHieuXepGia.Trim()),("trangthai",item.TrangThai),("tinhtrang",item.TinhTrang),("gia",item.GiaBia),("ngay",item.NgayNhap?.ToDateTime(TimeOnly.MinValue))};
        var sql=item.IdBanTaiLieu==0 ? "INSERT INTO BanTaiLieu(IdDauTaiLieu,SoDangKyCaBiet,MaVach,IdViTri,KyHieuXepGia,TrangThai,TinhTrang,GiaBia,NgayNhap) VALUES(@dau,@dkcb,@barcode,@vitri,@xep,@trangthai,@tinhtrang,@gia,@ngay)" : "UPDATE BanTaiLieu SET SoDangKyCaBiet=@dkcb,MaVach=@barcode,IdViTri=@vitri,KyHieuXepGia=@xep,TrangThai=@trangthai,TinhTrang=@tinhtrang,GiaBia=@gia,NgayNhap=@ngay WHERE IdBanTaiLieu=@id";
        var id=await store.Execute(sql,ct,values);if(item.IdBanTaiLieu>0)id=item.IdBanTaiLieu;
        var result=CatalogStore.Copy((await store.Rows(CopySelect+" WHERE b.IdBanTaiLieu=@id",ct,("id",id)))[0]);
        await store.Audit(actor,"TV2_COPY_SAVE","BanTaiLieu",id.ToString(),old.Count==0?null:CatalogStore.Copy(old[0]),result,ct);
        return result;
    }
    private static void Need(string? value,int max,string name){if(string.IsNullOrWhiteSpace(value)||value.Length>max)throw new InvalidOperationException(name+" bị thiếu hoặc quá dài.");}
}
