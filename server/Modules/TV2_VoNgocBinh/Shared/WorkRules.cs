using System.Globalization;
using System.Text.Json;
using ThuVienDAU.Protocol;
namespace ThuVienDAU.Catalog;
internal static class WorkRules
{
    internal static T Read<T>(TcpRequest r) where T:class => r.Payload?.Deserialize<T>(TcpProtocol.Json) ?? throw new InvalidOperationException("Thiếu dữ liệu yêu cầu.");
    internal static void Need(string? s,int max,string label){if(string.IsNullOrWhiteSpace(s)||s.Length>max)throw new InvalidOperationException(label+" bị thiếu hoặc quá dài.");}
    internal static async Task<Dictionary<string,object?>> Title(CatalogStore s,int id,CancellationToken ct)
    {
        var rows=await s.Rows("SELECT * FROM DauTaiLieu WHERE IdDauTaiLieu=@id FOR UPDATE",ct,("id",id));
        return rows.Count==1?rows[0]:throw new InvalidOperationException("Không tìm thấy đầu tài liệu.");
    }
    internal static void Version(Dictionary<string,object?> row,ulong version){if(Convert.ToUInt64(row["PhienBan"])!=version)throw new InvalidOperationException("Dữ liệu đã thay đổi. Tải lại trước khi lưu.");}
    internal static TableResult Table(List<Dictionary<string,object?>> rows,params string[] columns) => new(columns,rows.Select(r=>columns.Select(c=>r[c] is DateTime d?d.ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture):Convert.ToString(r[c],CultureInfo.InvariantCulture)).ToArray()).ToArray());
    internal static string Like(CatalogQuery q)
    {
        if(q.Text is null||q.Text.Length>100||q.Offset<0||q.Offset>1000000)throw new InvalidOperationException("Bộ lọc không hợp lệ.");
        return "%"+q.Text.Replace("!","!!").Replace("%","!%").Replace("_","!_")+"%";
    }
}
