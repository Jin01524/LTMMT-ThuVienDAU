using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using ThuVienDAU.Configuration;
using ThuVienDAU.Data;
using ThuVienDAU.Protocol;
namespace ThuVienDAU.Catalog;
public sealed class CatalogService(ICatalogSessionResolver? sessions)
{
    public async Task<TcpResponse> HandleAsync(TcpRequest request, CancellationToken ct)
    {
        try
        {
            if (request.Version != 1 || string.IsNullOrWhiteSpace(request.RequestId) || request.RequestId.Length > 64)
                throw new InvalidOperationException("Phiên bản hoặc mã yêu cầu không hợp lệ.");
            var actor = sessions?.Resolve(request.SessionToken) ?? throw new UnauthorizedAccessException("Chưa có phiên Biên mục. Chờ HT-02 hoặc dùng chế độ thử loopback được cấp trên server.");
            await using var db = await new MySqlConnectionFactory(DatabaseOptions.Load()).OpenAsync(ct);
            await using var tx = await db.BeginTransactionAsync(ct);
            var store = new CatalogStore(db, tx);
            // Lock actor until transaction completes; client cannot supply/override this identity.
            var account = await store.Rows("SELECT VaiTro FROM TaiKhoan WHERE IdTaiKhoan=@id AND DangHoatDong=1 FOR UPDATE", ct, ("id",actor));
            if (account.Count != 1 || !(new[]{"BIEN_MUC","QUAN_LY_THU_VIEN"}.Contains(account[0]["VaiTro"]?.ToString()) || request.Command=="TV2_COPY_LOOKUP" && account[0]["VaiTro"]?.ToString()=="THU_THU"))
                throw new UnauthorizedAccessException("Tài khoản đã khóa hoặc không có quyền Biên mục/Quản lý.");
            object result;
            if (request.Command.EndsWith("_LIST",StringComparison.Ordinal))
            {
                var q = Payload<CatalogQuery>(request); if (q.Text is null || q.Text.Length > 100 || q.Offset < 0 || q.Offset > 1000000) throw new InvalidOperationException("Bộ lọc không hợp lệ.");
                var term = "%" + q.Text.Replace("!","!!").Replace("%","!%").Replace("_","!_") + "%";
                if (request.Command == "TV2_DDC_LIST") result = (await store.Rows("SELECT * FROM MucPhanLoaiDDC WHERE MaDDC LIKE @q ESCAPE '!' OR TenMuc LIKE @q ESCAPE '!' ORDER BY MaDDC LIMIT 100 OFFSET @off",ct,("q",term),("off",q.Offset))).Select(CatalogStore.Ddc).ToArray();
                else if (request.Command == "TV2_TITLE_LIST") result = (await store.Rows("SELECT * FROM DauTaiLieu WHERE NhanDe LIKE @q ESCAPE '!' OR CAST(IdDauTaiLieu AS CHAR)=@exact ORDER BY IdDauTaiLieu DESC LIMIT 100 OFFSET @off",ct,("q",term),("exact",q.Text),("off",q.Offset))).Select(CatalogStore.Title).ToArray();
                else if(request.Command=="TV2_LOCATION_LIST") result=(await store.Rows("SELECT * FROM ViTriLuuTru WHERE DayKe LIKE @q ESCAPE '!' OR Ngan LIKE @q ESCAPE '!' ORDER BY DayKe,Ngan,IdViTri LIMIT 100 OFFSET @off",ct,("q",term),("off",q.Offset))).Select(CatalogStore.Location).ToArray();
                else if(request.Command=="TV2_COPY_LIST") result=(await store.Rows(InventoryService.CopySelect+" WHERE b.SoDangKyCaBiet LIKE @q ESCAPE '!' OR b.MaVach LIKE @q ESCAPE '!' OR d.NhanDe LIKE @q ESCAPE '!' ORDER BY b.IdBanTaiLieu DESC LIMIT 100 OFFSET @off",ct,("q",term),("off",q.Offset))).Select(CatalogStore.Copy).ToArray();
                else throw new InvalidOperationException("Lệnh danh mục không hợp lệ.");
            }
            else if (request.Command == "TV2_DDC_SAVE")
            {
                var item = Payload<DdcItem>(request);
                Need(item.MaDDC,64,"Mã DDC"); Need(item.TenMuc,200,"Tên mục"); Need(item.NguonThamKhao,600,"Nguồn tham khảo");
                if (!Regex.IsMatch(item.MaDDC,@"^[0-9]{3}(\.[0-9]*[1-9])?$")) throw new InvalidOperationException("Mã DDC phải giữ 3 chữ số đầu; phần lẻ không kết thúc bằng 0.");
                Optional(item.MaCha,64); Optional(item.ViTriTraBanIn,150); Optional(item.CachGhepSo,600);
                if(item.DaDoiChieu) Need(item.ViTriTraBanIn,150,"Tập/trang tra bản in tiếng Việt rút gọn");
                var old = await store.Rows("SELECT * FROM MucPhanLoaiDDC WHERE MaDDC=@id FOR UPDATE",ct,("id",item.MaDDC));
                if(old.Count == 0 && !string.IsNullOrEmpty(item.Revision) || old.Count == 1 && CatalogStore.Ddc(old[0]).Revision != item.Revision) throw new InvalidOperationException("Mục DDC đã được thay đổi. Tải lại trước khi lưu.");
                var values = new (string,object?)[] { ("ma",item.MaDDC),("cha",Blank(item.MaCha)),("ten",item.TenMuc.Trim()),("nguon",item.NguonThamKhao.Trim()),("duyet",item.DaDoiChieu),("vitri",Blank(item.ViTriTraBanIn)),("ghep",Blank(item.CachGhepSo)),("actor",item.DaDoiChieu ? actor : null) };
                var sql = old.Count == 0 ? "INSERT INTO MucPhanLoaiDDC(MaDDC,MaCha,TenMuc,NguonThamKhao,DaDoiChieu,ViTriTraBanIn,CachGhepSo,IdNguoiDoiChieu,ThoiDiemDoiChieu) VALUES(@ma,@cha,@ten,@nguon,@duyet,@vitri,@ghep,@actor,IF(@duyet,NOW(),NULL))" : "UPDATE MucPhanLoaiDDC SET MaCha=@cha,TenMuc=@ten,NguonThamKhao=@nguon,DaDoiChieu=@duyet,ViTriTraBanIn=@vitri,CachGhepSo=@ghep,IdNguoiDoiChieu=@actor,ThoiDiemDoiChieu=IF(@duyet,NOW(),NULL) WHERE MaDDC=@ma";
                await store.Execute(sql,ct,values);
                result = CatalogStore.Ddc((await store.Rows("SELECT * FROM MucPhanLoaiDDC WHERE MaDDC=@ma",ct,("ma",item.MaDDC)))[0]);
                await store.Audit(actor,"TV2_DDC_SAVE","MucPhanLoaiDDC",item.MaDDC,old.Count == 0 ? null : CatalogStore.Ddc(old[0]),result,ct);
            }
            else if(request.Command == "TV2_TITLE_SAVE")
            {
                var item = Payload<TitleItem>(request); Need(item.NhanDe,500,"Nhan đề");
                if(item.IdDauTaiLieu < 0 || !new[]{"SACH","DO_AN","LUAN_VAN","KHAC"}.Contains(item.LoaiTaiLieu) || !new[]{"CHUA_PHAN_LOAI","DU_KIEN","DA_XAC_NHAN"}.Contains(item.TrangThaiPhanLoai)) throw new InvalidOperationException("Loại tài liệu/trạng thái không hợp lệ.");
                Optional(item.MaDDC,64); Optional(item.ViTriTraDDC,150);
                var classified = item.TrangThaiPhanLoai != "CHUA_PHAN_LOAI";
                if(classified) Need(item.MaDDC,64,"Mã DDC");
                else if(!string.IsNullOrWhiteSpace(item.MaDDC) || !string.IsNullOrWhiteSpace(item.ViTriTraDDC)) throw new InvalidOperationException("Chưa phân loại phải bỏ trống mã DDC/vị trí tra.");
                if(item.TrangThaiPhanLoai == "DA_XAC_NHAN") Need(item.ViTriTraDDC,150,"Vị trí tra DDC cho tài liệu");
                var old = await store.Rows("SELECT * FROM DauTaiLieu WHERE IdDauTaiLieu=@id FOR UPDATE",ct,("id",item.IdDauTaiLieu));
                if(item.IdDauTaiLieu > 0 && (old.Count == 0 || CatalogStore.Title(old[0]).PhienBan != item.PhienBan)) throw new InvalidOperationException("Đầu tài liệu đã thay đổi. Tải lại trước khi lưu.");
                if(old.Count == 1) { var previous = CatalogStore.Title(old[0]); if(previous.TrangThaiPhanLoai == "DA_XAC_NHAN" && item.TrangThaiPhanLoai == "DA_XAC_NHAN" && (previous.MaDDC != item.MaDDC || previous.ViTriTraDDC != item.ViTriTraDDC)) throw new InvalidOperationException("Đổi phân loại đã xác nhận: lưu về DỰ KIẾN trước, rồi xác nhận lại sau khi tra bản in."); }
                var values = new (string,object?)[] { ("id",item.IdDauTaiLieu),("ten",item.NhanDe.Trim()),("loai",item.LoaiTaiLieu),("ma",Blank(item.MaDDC)),("trangthai",item.TrangThaiPhanLoai),("vitri",Blank(item.ViTriTraDDC)),("hien",item.DangHienThi),("actor",classified ? actor : null) };
                var sql = item.IdDauTaiLieu == 0 ? "INSERT INTO DauTaiLieu(NhanDe,LoaiTaiLieu,MaDDC,TrangThaiPhanLoai,ViTriTraDDC,DangHienThi,IdNguoiPhanLoai,ThoiDiemPhanLoai) VALUES(@ten,@loai,@ma,@trangthai,@vitri,@hien,@actor,IF(@actor IS NULL,NULL,NOW()))" : "UPDATE DauTaiLieu SET NhanDe=@ten,LoaiTaiLieu=@loai,MaDDC=@ma,TrangThaiPhanLoai=@trangthai,ViTriTraDDC=@vitri,DangHienThi=@hien,IdNguoiPhanLoai=@actor,ThoiDiemPhanLoai=IF(@actor IS NULL,NULL,NOW()) WHERE IdDauTaiLieu=@id";
                var id = await store.Execute(sql,ct,values); if(item.IdDauTaiLieu > 0) id = item.IdDauTaiLieu;
                result = CatalogStore.Title((await store.Rows("SELECT * FROM DauTaiLieu WHERE IdDauTaiLieu=@id",ct,("id",id)))[0]);
                await store.Audit(actor,"TV2_TITLE_SAVE","DauTaiLieu",id.ToString(),old.Count == 0 ? null : CatalogStore.Title(old[0]),result,ct);
            }
            else if(request.Command=="TV2_LOCATION_SAVE") result=await InventoryService.SaveLocation(store,Payload<StorageLocationItem>(request),actor,ct);
            else if(request.Command=="TV2_COPY_SAVE") result=await InventoryService.SaveCopy(store,Payload<CopyItem>(request),actor,ct);
            else if(request.Command=="TV2_COPY_LOOKUP")
            {
                var query=Payload<BarcodeQuery>(request);Need(query.MaVach,100,"Barcode");
                var rows=await store.Rows("SELECT * FROM BanTaiLieu WHERE MaVach=@barcode FOR UPDATE",ct,("barcode",query.MaVach.Trim()));
                if(rows.Count==0)throw new InvalidOperationException("Không tìm thấy bản sách theo barcode.");
                var id=Convert.ToInt32(rows[0]["IdBanTaiLieu"]);
                var copy=CatalogStore.Copy((await store.Rows(InventoryService.CopySelect+" WHERE b.IdBanTaiLieu=@id",ct,("id",id)))[0]);
                var active=(await store.Rows("SELECT IdChiTietMuon FROM ChiTietPhieuMuon WHERE IdBanTaiLieu=@id AND TrangThai='DANG_MUON'",ct,("id",id))).Count>0;
                result=new CopyLookup(copy,active);
            }
            else throw new InvalidOperationException("Lệnh TV2 chưa được triển khai.");
            await tx.CommitAsync(ct);
            return new(1,request.RequestId,true,JsonSerializer.SerializeToElement(result,TcpProtocol.Json));
        }
        catch(UnauthorizedAccessException ex) { return new(1,request.RequestId,false,Error:ex.Message); }
        catch(Exception ex) when(ex is InvalidOperationException or ArgumentException or JsonException) { return new(1,request.RequestId,false,Error:ex.Message); }
        catch(MySqlException ex)
        {
            var message = ex.Number switch { 1062 => "Mã đã tồn tại; tải lại danh sách.", 1452 => "Mã cha DDC hoặc dữ liệu tham chiếu không tồn tại.", 1644 => "CSDL từ chối: " + ex.Message, 3819 => "Dữ liệu vi phạm ràng buộc CSDL.", 1213 or 1205 => "Dữ liệu đang được người khác xử lý; thử tải lại.", _ => "Không thao tác được CSDL. Kiểm tra cấu hình server." };
            return new(1,request.RequestId,false,Error:message,ErrorNumber:ex.Number);
        }
    }
    private static T Payload<T>(TcpRequest r) where T : class => r.Payload?.Deserialize<T>(TcpProtocol.Json) ?? throw new InvalidOperationException("Thiếu dữ liệu yêu cầu.");
    private static void Need(string? value,int max,string label) { if(string.IsNullOrWhiteSpace(value) || value.Length > max) throw new InvalidOperationException(label + " bị thiếu hoặc quá dài."); }
    private static void Optional(string? value,int max) { if(value?.Length > max) throw new InvalidOperationException("Dữ liệu quá dài."); }
    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
