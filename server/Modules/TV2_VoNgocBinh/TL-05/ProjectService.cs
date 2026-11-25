using System.Text.Json;
using ThuVienDAU.Protocol;
namespace ThuVienDAU.Catalog;
internal static class ProjectService
{
    internal static async Task<ProjectItem> Get(CatalogStore s,int id,CancellationToken ct)
    {
        var t=await WorkRules.Title(s,id,ct);RequireProject(t);
        var members=await s.Rows("SELECT v.IdDocGia,v.VaiTro,d.HoTen,d.MaDinhDanhTruong FROM ThanhVienDeTai v JOIN DocGia d USING(IdDocGia) WHERE v.IdDauTaiLieu=@id ORDER BY v.IdDocGia,v.VaiTro",ct,("id",id));
        return new(id,t["MaNganh"] as string,t["KhoaHoc"] as string,members.Select(x=>new ProjectMember(Convert.ToInt32(x["IdDocGia"]),(string)x["VaiTro"]!,(string)x["HoTen"]!,(string)x["MaDinhDanhTruong"]!)).ToArray(),JsonSerializer.Deserialize<AttachmentItem[]>((string)t["DanhSachTep"]!,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!,Convert.ToUInt64(t["PhienBan"]));
    }
    internal static void RequireProject(Dictionary<string,object?> t){if(t["LoaiTaiLieu"] as string is not ("DO_AN" or "LUAN_VAN"))throw new InvalidOperationException("Chỉ áp dụng đồ án/luận văn.");}
    internal static async Task<ProjectItem> Save(CatalogStore s,ProjectItem item,int actor,CancellationToken ct)
    {
        var t=await WorkRules.Title(s,item.IdDauTaiLieu,ct);RequireProject(t);WorkRules.Version(t,item.PhienBan);
        if(item.KhoaHoc?.Length>30||item.MaNganh?.Length!=7||item.ThanhVien is null||item.ThanhVien.Length>30||item.ThanhVien.Any(x=>x is null)||item.ThanhVien.DistinctBy(x=>(x.IdDocGia,x.VaiTro)).Count()!=item.ThanhVien.Length)throw new InvalidOperationException("Cần mã ngành 7 chữ số, khóa tối đa 30 ký tự, tối đa 30 thành viên không trùng.");
        foreach(var m in item.ThanhVien)
        {
            var rows=await s.Rows("SELECT LoaiDocGia FROM DocGia WHERE IdDocGia=@id",ct,("id",m.IdDocGia));
            if(rows.Count==0||m.VaiTro is not ("THUC_HIEN" or "HUONG_DAN")||m.VaiTro=="THUC_HIEN"&&rows[0]["LoaiDocGia"] as string!="SINH_VIEN"||m.VaiTro=="HUONG_DAN"&&rows[0]["LoaiDocGia"] as string is not ("GIANG_VIEN" or "TRO_GIANG" or "CAN_BO"))throw new InvalidOperationException("Thành viên không phù hợp vai trò sinh viên / cán bộ hướng dẫn.");
        }
        var old=await Get(s,item.IdDauTaiLieu,ct);
        await s.Execute("UPDATE DauTaiLieu SET MaNganh=@major,KhoaHoc=@course WHERE IdDauTaiLieu=@id",ct,("major",item.MaNganh),("course",item.KhoaHoc?.Trim()),("id",item.IdDauTaiLieu));
        await s.Execute("DELETE FROM ThanhVienDeTai WHERE IdDauTaiLieu=@id",ct,("id",item.IdDauTaiLieu));
        foreach(var m in item.ThanhVien)await s.Execute("INSERT INTO ThanhVienDeTai VALUES(@id,@reader,@role)",ct,("id",item.IdDauTaiLieu),("reader",m.IdDocGia),("role",m.VaiTro));
        // Files can only be changed by the managed upload service; never accept arbitrary paths.
        var result=await Get(s,item.IdDauTaiLieu,ct);await s.Audit(actor,"TV2_PROJECT_SAVE","DauTaiLieu",item.IdDauTaiLieu.ToString(),old,result,ct);return result;
    }
}
