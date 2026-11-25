using System.Text.Json;
using System.Text.RegularExpressions;
namespace ThuVienDAU.Catalog;
public sealed class MarcLine
{
    public int Nhom {get;set;}
    public string NhanTruong {get;set;}="245";
    public string ChiThi1 {get;set;}="0";
    public string ChiThi2 {get;set;}="0";
    public string MaTruongCon {get;set;}="a";
    public string NoiDung {get;set;}="";
}
public static class MarcEditorCodec
{
    public static JsonElement Encode(IEnumerable<MarcLine> source)
    {
        var rows=source.ToArray();var output=new List<object>();var used=new HashSet<int>();
        for(var i=0;i<rows.Length;)
        {
            var first=rows[i];if(!used.Add(first.Nhom))throw new InvalidOperationException("Các dòng của cùng nhóm trường phải liên tiếp.");
            var group=new List<MarcLine>();while(i<rows.Length&&rows[i].Nhom==first.Nhom)group.Add(rows[i++]);
            if(!Regex.IsMatch(first.NhanTruong,@"\A(00[1-9]|0[1-9][0-9]|[1-9][0-9]{2})\z")||group.Any(x=>x.NhanTruong!=first.NhanTruong||x.ChiThi1!=first.ChiThi1||x.ChiThi2!=first.ChiThi2))throw new InvalidOperationException("Mã trường 001–999; trong cùng nhóm phải cùng mã trường/chỉ thị.");
            if(first.NhanTruong.StartsWith("00"))
            {
                if(group.Count!=1)throw new InvalidOperationException("Trường điều khiển mỗi nhóm có một dòng.");output.Add(new{NhanTruong=first.NhanTruong,NoiDungDieuKhien=first.NoiDung});
            }
            else output.Add(new{NhanTruong=first.NhanTruong,ChiThi1=first.ChiThi1,ChiThi2=first.ChiThi2,TruongCon=group.Select(x=>new{MaTruongCon=x.MaTruongCon,NoiDung=x.NoiDung}).ToArray()});
        }
        return JsonSerializer.SerializeToElement(output);
    }
    public static List<MarcLine> Decode(JsonElement fields)
    {
        var rows=new List<MarcLine>();int group=0;
        foreach(var f in fields.EnumerateArray())
        {
            group++;var tag=f.GetProperty("NhanTruong").GetString()!;
            if(tag.StartsWith("00"))rows.Add(new(){Nhom=group,NhanTruong=tag,ChiThi1=" ",ChiThi2=" ",MaTruongCon="",NoiDung=f.GetProperty("NoiDungDieuKhien").GetString()!});
            else foreach(var sub in f.GetProperty("TruongCon").EnumerateArray())rows.Add(new(){Nhom=group,NhanTruong=tag,ChiThi1=f.GetProperty("ChiThi1").GetString()!,ChiThi2=f.GetProperty("ChiThi2").GetString()!,MaTruongCon=sub.GetProperty("MaTruongCon").GetString()!,NoiDung=sub.GetProperty("NoiDung").GetString()!});
        }
        return rows;
    }
}
