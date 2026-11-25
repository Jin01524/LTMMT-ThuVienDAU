using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MySql.Data.MySqlClient;
using ThuVienDAU.Protocol;
namespace ThuVienDAU.Catalog;
internal sealed class CatalogStore(MySqlConnection connection, MySqlTransaction transaction)
{
    private MySqlCommand Command(string sql,(string Name,object? Value)[] args)
    {
        var command = new MySqlCommand(sql,connection,transaction);
        foreach(var (name,value) in args) command.Parameters.AddWithValue("@"+name,value ?? DBNull.Value);
        return command;
    }
    public async Task<List<Dictionary<string,object?>>> Rows(string sql,CancellationToken ct,params (string,object?)[] args)
    {
        using var cmd=Command(sql,args); await using var reader=await cmd.ExecuteReaderAsync(ct); var rows=new List<Dictionary<string,object?>>();
        while(await reader.ReadAsync(ct)) { var row=new Dictionary<string,object?>(); for(var i=0;i<reader.FieldCount;i++) row[reader.GetName(i)]=reader.IsDBNull(i)?null:reader.GetValue(i); rows.Add(row); } return rows;
    }
    public async Task<int> Execute(string sql,CancellationToken ct,params (string,object?)[] args)
    {
        using var cmd=Command(sql,args); await cmd.ExecuteNonQueryAsync(ct); return checked((int)cmd.LastInsertedId);
    }
    public Task<int> Audit(int actor,string action,string table,string key,object? before,object after,CancellationToken ct)
        => Execute("INSERT INTO NhatKy(IdTaiKhoan,HanhDong,BangDuLieu,KhoaBanGhi,ChiTiet) VALUES(@actor,@action,@table,@key,@json)",ct,("actor",actor),("action",action),("table",table),("key",key),("json",JsonSerializer.Serialize(new { Before=before,After=after },TcpProtocol.Json)));
    public static DdcItem Ddc(Dictionary<string,object?> r)
    {
        var item=new DdcItem((string)r["MaDDC"]!,r["MaCha"] as string,(string)r["TenMuc"]!, (string)r["NguonThamKhao"]!,Convert.ToBoolean(r["DaDoiChieu"]),r["ViTriTraBanIn"] as string,r["CachGhepSo"] as string,r["IdNguoiDoiChieu"] is null?null:Convert.ToInt32(r["IdNguoiDoiChieu"]),r["ThoiDiemDoiChieu"] as DateTime?);
        return item with { Revision=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(item,TcpProtocol.Json))) };
    }
    public static TitleItem Title(Dictionary<string,object?> r) => new(Convert.ToInt32(r["IdDauTaiLieu"]),(string)r["NhanDe"]!,(string)r["LoaiTaiLieu"]!,r["MaDDC"] as string,(string)r["TrangThaiPhanLoai"]!,r["ViTriTraDDC"] as string,Convert.ToBoolean(r["DangHienThi"]),Convert.ToUInt64(r["PhienBan"]));

    public static string Revision<T>(T item) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(item,TcpProtocol.Json)));
    public static StorageLocationItem Location(Dictionary<string,object?> r)
    {
        var item=new StorageLocationItem(Convert.ToInt32(r["IdViTri"]),(string)r["DayKe"]!,(string)r["Ngan"]!);
        return item with { Revision=Revision(item) };
    }
    public static CopyItem Copy(Dictionary<string,object?> r)
    {
        var item=new CopyItem(Convert.ToInt32(r["IdBanTaiLieu"]),Convert.ToInt32(r["IdDauTaiLieu"]),(string)r["SoDangKyCaBiet"]!,(string)r["MaVach"]!,
            r["IdViTri"] is null?null:Convert.ToInt32(r["IdViTri"]),r["KyHieuXepGia"] as string,(string)r["TrangThai"]!,(string)r["TinhTrang"]!,
            r["GiaBia"] is null?null:Convert.ToDecimal(r["GiaBia"]),r["NgayNhap"] is DateTime date?DateOnly.FromDateTime(date):null);
        return item with { Revision=Revision(item),NhanDe=r.GetValueOrDefault("NhanDe") as string??"",DayKe=r.GetValueOrDefault("DayKe") as string,Ngan=r.GetValueOrDefault("Ngan") as string };
    }
}
