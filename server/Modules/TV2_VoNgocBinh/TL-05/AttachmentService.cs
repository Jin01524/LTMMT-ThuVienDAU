using System.Text.Json;
using System.Text.RegularExpressions;
namespace ThuVienDAU.Catalog;
// Files live only on the server. The JSON column contains opaque identifiers, never arbitrary paths.
internal static class AttachmentService
{
    private const int Maximum=20*1024*1024,ChunkSize=192*1024;
    private sealed record Upload(int Actor,FileBegin Request,DateTime Expires);
    private static string Root=>Path.Combine(AppContext.BaseDirectory,"data","tep-do-an");
    private static string Name(string id,string suffix)
    {
        if(!Regex.IsMatch(id??"",@"\A[a-f0-9]{32}\z"))throw new InvalidOperationException("Mã tệp không hợp lệ.");
        return Path.Combine(Root,id+suffix);
    }
    private static Upload Metadata(string id,int actor)
    {
        var path=Name(id,".json");if(!File.Exists(path))throw new InvalidOperationException("Phiên tải tệp không tồn tại.");
        var m=JsonSerializer.Deserialize<Upload>(File.ReadAllText(path))!;
        if(m.Actor!=actor||m.Expires<DateTime.UtcNow)throw new UnauthorizedAccessException("Phiên tải tệp không thuộc tài khoản hoặc đã hết hạn.");return m;
    }
    internal static async Task<FileTicket> Begin(CatalogStore s,FileBegin q,int actor,CancellationToken ct)
    {
        var t=await WorkRules.Title(s,q.IdDauTaiLieu,ct);ProjectService.RequireProject(t);WorkRules.Version(t,q.PhienBan);
        WorkRules.Need(q.TenTep,255,"Tên PDF");
        if(q.TenTep.IndexOfAny(['/', '\\', ':'])>=0||Path.GetExtension(q.TenTep).ToLowerInvariant()!=".pdf"||q.KichThuoc is <5 or >Maximum)throw new InvalidOperationException("Chỉ nhận PDF từ 5 byte đến 20 MiB, tên không chứa đường dẫn.");
        Directory.CreateDirectory(Root);
        // Expired uploads are inaccessible and can be removed safely; do not remove finalized PDFs.
        foreach(var meta in Directory.EnumerateFiles(Root,"*.json"))if(File.GetLastWriteTimeUtc(meta)<DateTime.UtcNow.AddHours(-2)){File.Delete(Path.ChangeExtension(meta,".part"));File.Delete(meta);}
        if(Directory.EnumerateFiles(Root,"*.json").Count()>=100)throw new InvalidOperationException("Quá nhiều tệp đang tải; thử lại sau.");
        var id=Guid.NewGuid().ToString("N");await File.WriteAllTextAsync(Name(id,".json"),JsonSerializer.Serialize(new Upload(actor,q,DateTime.UtcNow.AddHours(1))),ct);
        using(File.Create(Name(id,".part"))){}return new(id);
    }
    internal static async Task<FileTicket> Chunk(FileChunk q,int actor,CancellationToken ct)
    {
        var m=Metadata(q.MaTep,actor);byte[] bytes;
        try{bytes=Convert.FromBase64String(q.DuLieu);}catch(FormatException){throw new InvalidOperationException("Khối tệp không hợp lệ.");}
        var path=Name(q.MaTep,".part");var length=new FileInfo(path).Length;
        if(bytes.Length is <1 or >ChunkSize||q.ViTri!=length||length+bytes.Length>m.Request.KichThuoc)throw new InvalidOperationException("Sai vị trí/kích thước khối tệp.");
        await using var file=new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.None);await file.WriteAsync(bytes,ct);return new(q.MaTep,checked((int)file.Length));
    }
    internal static async Task<ProjectItem> Finish(CatalogStore s,FileTicket q,int actor,CancellationToken ct)
    {
        var m=Metadata(q.MaTep,actor);var t=await WorkRules.Title(s,m.Request.IdDauTaiLieu,ct);ProjectService.RequireProject(t);WorkRules.Version(t,m.Request.PhienBan);
        var path=Name(q.MaTep,".part");var bytes=await File.ReadAllBytesAsync(path,ct);
        if(bytes.Length!=m.Request.KichThuoc||!bytes.AsSpan(0,Math.Min(5,bytes.Length)).SequenceEqual("%PDF-"u8))throw new InvalidOperationException("PDF thiếu dữ liệu hoặc sai chữ ký PDF.");
        var old=await ProjectService.Get(s,m.Request.IdDauTaiLieu,ct);
        if(old.DanhSachTep.Length>=20)throw new InvalidOperationException("Mỗi đề tài tối đa 20 tệp.");
        var attachment=new AttachmentItem(m.Request.TenTep,q.MaTep+".pdf");var files=old.DanhSachTep.Append(attachment).ToArray();
        // DB rollback may leave an unreferenced PDF, but downloads always require a committed DB link.
        File.Move(path,Name(q.MaTep,".pdf"));
        await s.Execute("UPDATE DauTaiLieu SET DanhSachTep=@files WHERE IdDauTaiLieu=@id",ct,("files",JsonSerializer.Serialize(files)),("id",old.IdDauTaiLieu));
        await s.Audit(actor,"TV2_FILE_ATTACH","DauTaiLieu",old.IdDauTaiLieu.ToString(),old.DanhSachTep,files,ct);
        File.Delete(Name(q.MaTep,".json"));return await ProjectService.Get(s,old.IdDauTaiLieu,ct);
    }
    internal static async Task<FileData> Read(CatalogStore s,FileRead q,CancellationToken ct)
    {
        var project=await ProjectService.Get(s,q.IdDauTaiLieu,ct);var path=Name(q.MaTep,".pdf");
        if(!project.DanhSachTep.Any(x=>x.DuongDan==q.MaTep+".pdf"))throw new UnauthorizedAccessException("Tệp không thuộc đề tài.");
        if(q.ViTri<0||!File.Exists(path))throw new InvalidOperationException("Không tìm thấy tệp trên server hoặc sai vị trí đọc.");
        await using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(q.ViTri>=file.Length)throw new InvalidOperationException("Vị trí đọc vượt kích thước.");file.Position=q.ViTri;
        var b=new byte[Math.Min(ChunkSize,checked((int)file.Length-q.ViTri))];await file.ReadExactlyAsync(b,ct);return new(Convert.ToBase64String(b),checked((int)file.Length));
    }
}
