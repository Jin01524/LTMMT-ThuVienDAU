using System.Text.Json;
using ThuVienDAU.Protocol;
namespace ThuVienDAU.Catalog;
internal static class MarcService
{
    internal static async Task<MarcItem> Get(CatalogStore s,int id,CancellationToken ct)
    {
        var title=await WorkRules.Title(s,id,ct);var version=Convert.ToUInt64(title["PhienBan"]);
        var rows=await s.Rows("SELECT * FROM BieuGhiMARC WHERE IdDauTaiLieu=@id FOR UPDATE",ct,("id",id));
        if(rows.Count==0)return new(id,"00000nam a2200000   4500",JsonSerializer.SerializeToElement(Array.Empty<object>()),PhienBanHoSo:version);
        var r=rows[0];return new(id,(string)r["DauBieuMARC"]!,JsonDocument.Parse((string)r["Truong"]!).RootElement.Clone(),Convert.ToUInt64(r["PhienBan"]),(string)r["TrangThai"]!,r["PhienBanDauTaiLieu"] is null?null:Convert.ToUInt64(r["PhienBanDauTaiLieu"]),version);
    }
    internal static async Task<MarcItem> Save(CatalogStore s,MarcItem item,int actor,CancellationToken ct)
    {
        var title=await WorkRules.Title(s,item.IdDauTaiLieu,ct);WorkRules.Version(title,item.PhienBanHoSo);WorkRules.Need(item.DauBieuMARC,24,"Đầu biểu 24 ký tự");
        if(item.Truong.ValueKind!=JsonValueKind.Array||item.Truong.GetArrayLength()>300||item.Truong.GetRawText().Length>200000)throw new InvalidOperationException("Tối đa 300 trường MARC / 200.000 ký tự.");
        var old=await Get(s,item.IdDauTaiLieu,ct);if(old.PhienBan!=item.PhienBan)throw new InvalidOperationException("Biểu ghi đã thay đổi. Tải lại.");
        // Editing always returns to draft, including a no-op save of a checked record.
        await s.Execute(item.PhienBan==0?"INSERT INTO BieuGhiMARC(IdDauTaiLieu,DauBieuMARC,Truong,IdNguoiTao) VALUES(@id,@leader,@fields,@actor)":"UPDATE BieuGhiMARC SET DauBieuMARC=@leader,Truong=@fields,TrangThai='NHAP',IdNguoiKiemTra=NULL,ThoiDiemKiemTra=NULL,PhienBanDauTaiLieu=NULL WHERE IdDauTaiLieu=@id",ct,("id",item.IdDauTaiLieu),("leader",item.DauBieuMARC),("fields",item.Truong.GetRawText()),("actor",actor));
        var result=await Get(s,item.IdDauTaiLieu,ct);await s.Audit(actor,"TV2_MARC_SAVE","BieuGhiMARC",item.IdDauTaiLieu.ToString(),old,result,ct);return result;
    }
    internal static async Task<MarcItem> Check(CatalogStore s,MarcCheck item,int actor,CancellationToken ct)
    {
        var title=await WorkRules.Title(s,item.IdDauTaiLieu,ct);WorkRules.Version(title,item.PhienBanHoSo);var old=await Get(s,item.IdDauTaiLieu,ct);
        if(old.PhienBan==0||old.PhienBan!=item.PhienBan)throw new InvalidOperationException("Biểu ghi chưa lưu hoặc đã thay đổi.");
        // Synchronize the explicit catalog title and 245$a in the same transaction.
        var fields=old.Truong.EnumerateArray().Where(x=>x.GetProperty("NhanTruong").GetString()=="245").ToArray();
        var names=fields.SelectMany(x=>x.GetProperty("TruongCon").EnumerateArray()).Where(x=>x.GetProperty("MaTruongCon").GetString()=="a").Select(x=>x.GetProperty("NoiDung").GetString()).ToArray();
        if(names.Length!=1)throw new InvalidOperationException("Cần đúng một 245$a.");WorkRules.Need(names[0],500,"Nhan đề 245$a");
        await s.Execute("UPDATE DauTaiLieu SET NhanDe=@name WHERE IdDauTaiLieu=@id",ct,("name",names[0]!.Trim()),("id",item.IdDauTaiLieu));
        await s.Execute("UPDATE BieuGhiMARC SET TrangThai='DA_KIEM_TRA',IdNguoiKiemTra=@actor,ThoiDiemKiemTra=NOW() WHERE IdDauTaiLieu=@id",ct,("actor",actor),("id",item.IdDauTaiLieu));
        var result=await Get(s,item.IdDauTaiLieu,ct);await s.Audit(actor,"TV2_MARC_CHECK","BieuGhiMARC",item.IdDauTaiLieu.ToString(),new{Marc=old,NhanDe=title["NhanDe"]},result,ct);return result;
    }
    internal static async Task<Bibliography> Summary(CatalogStore s,int id,CancellationToken ct)
    {
        var t=await WorkRules.Title(s,id,ct);var m=await Get(s,id,ct);
        string Text(string tag,string code)=>string.Join("; ",m.Truong.EnumerateArray().Where(f=>f.GetProperty("NhanTruong").GetString()==tag).SelectMany(f=>f.GetProperty("TruongCon").EnumerateArray()).Where(f=>f.GetProperty("MaTruongCon").GetString()==code).Select(f=>f.GetProperty("NoiDung").GetString()));
        return new(id,(string)t["NhanDe"]!,Text("100","a"),Text("020","a"),Text("264","b") is {Length:>0} publisher?publisher:Text("260","b"),Text("264","c") is {Length:>0} year?year:Text("260","c"),m.TrangThai,m.TrangThai=="DA_KIEM_TRA"&&m.PhienBanDauTaiLieu!=Convert.ToUInt64(t["PhienBan"]));
    }
}
