using System.Drawing.Printing;
using System.ComponentModel;
namespace ThuVienDAU.Catalog;
public sealed class LabelForm : WorkForm
{
    private readonly BindingList<CopyItem> copies=new();private readonly DataGridView grid=Grid();
    public LabelForm(string session):base(session,"TL-07 · In barcode và nhãn gáy")
    {
        grid.DataSource=copies;Content.Controls.Add(grid);Button("Thêm bản sách",()=>{var copy=ChooseCopy();if(copy is not null&&!copies.Any(x=>x.IdBanTaiLieu==copy.IdBanTaiLieu)){LabelRenderer.Encode(copy.MaVach);copies.Add(copy);}return Task.CompletedTask;});
        Button("Bỏ dòng",()=>{if(grid.CurrentRow?.DataBoundItem is CopyItem c)copies.Remove(c);return Task.CompletedTask;});
        Button("Xem trước/in",async()=>
        {
            if(copies.Count==0)throw new InvalidOperationException("Thêm bản sách cần in.");
            // Refresh server values before printing; selected data may have changed on another workstation.
            var fresh=new List<CopyItem>();foreach(var c in copies){var value=await Api.CallAsync<CopyLookup>("TV2_COPY_LOOKUP",new BarcodeQuery(c.MaVach),Session);LabelRenderer.Encode(value.BanSach.MaVach);fresh.Add(value.BanSach);}
            using var doc=LabelRenderer.Document(fresh);using var preview=new PrintPreviewDialog{Document=doc,Width=1100,Height=800};preview.ShowDialog(this);
        });
        Content.Controls.Add(new Label{Dock=DockStyle.Bottom,Height=46,Text="Nhãn A4, 2 cột; barcode Code 128, kèm ĐKCB và ký hiệu xếp giá. Không thay mã đã lưu.\nMã quá dài/không in được sẽ báo lỗi; kiểm xem trước rồi hiệu chỉnh máy in thực tế khi nghiệm thu thiết bị."});
    }
}
public static class LabelRenderer
{
    public static bool[] Encode(string barcode)
    {
        if(string.IsNullOrWhiteSpace(barcode)||barcode.Length>40||barcode.Any(c=>c<' '||c>'~'))throw new InvalidOperationException("Nhãn Code 128 hỗ trợ 1–40 ký tự ASCII in được; sửa barcode tại TL-04 nếu cần.");
        return new ZXing.OneD.Code128Writer().encode(barcode);
    }
    public static void Draw(Graphics g,CopyItem copy,RectangleF bounds)
    {
        var bars=Encode(copy.MaVach);using var font=new Font("Segoe UI",9);using var bold=new Font("Segoe UI",10,FontStyle.Bold);var scale=bounds.Width/(bars.Length+20);
        g.FillRectangle(Brushes.White,bounds);g.DrawString("THƯ VIỆN ĐẠI HỌC KIẾN TRÚC ĐÀ NẴNG",font,Brushes.Black,bounds.X,bounds.Y);
        for(int x=0;x<bars.Length;x++)if(bars[x])g.FillRectangle(Brushes.Black,bounds.X+(x+10)*scale,bounds.Y+23,scale,42);
        g.DrawString(copy.MaVach,font,Brushes.Black,bounds.X,bounds.Y+67);g.DrawString("ĐKCB: "+copy.SoDangKyCaBiet,font,Brushes.Black,bounds.X,bounds.Y+84);g.DrawString(copy.KyHieuXepGia??"Chưa có ký hiệu xếp giá",bold,Brushes.Black,new RectangleF(bounds.X,bounds.Y+102,bounds.Width,36));
    }
    public static PrintDocument Document(IReadOnlyList<CopyItem> copies)
    {
        var doc=new PrintDocument{DocumentName="Barcode_va_nhan_gay_DAU"};doc.DefaultPageSettings.PaperSize=new PaperSize("A4",827,1169);doc.DefaultPageSettings.Margins=new Margins(40,40,40,40);int index=0;
        doc.BeginPrint+=(_,_)=>index=0;doc.PrintPage+=(_,e)=>
        {
            var area=e.MarginBounds;var width=(area.Width-25)/2f;int rows=Math.Max(1,area.Height/150);
            for(int row=0;row<rows&&index<copies.Count;row++)for(int col=0;col<2&&index<copies.Count;col++)Draw(e.Graphics!,copies[index++],new(area.X+col*(width+25),area.Y+row*150,width,140));e.HasMorePages=index<copies.Count;
        };return doc;
    }
}
