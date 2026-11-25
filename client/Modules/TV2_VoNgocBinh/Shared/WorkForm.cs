using System.Data;
using System.Text;
using System.Net.Sockets;
using System.Text.Json;
namespace ThuVienDAU.Catalog;
public class WorkForm : Form
{
    protected readonly CatalogClientController Api=new();
    protected readonly string Session;
    protected readonly FlowLayoutPanel Bar=new(){Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(8),WrapContents=true};
    protected readonly Label Status=new(){Dock=DockStyle.Bottom,Height=32,Text="Sẵn sàng",Padding=new Padding(8)};
    protected readonly Panel Content=new(){Dock=DockStyle.Fill};
    private bool busy;
    protected WorkForm(string session,string title)
    {
        Session=session;Text=title;Size=new Size(1180,760);MinimumSize=new Size(950,620);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
        Controls.Add(Content);Controls.Add(Bar);Controls.Add(Status);
    }
    protected Button Button(string label,Func<Task> action)
    {
        var b=new Button{Text=label,AutoSize=true,Height=36};b.Click+=async(_,_)=>await Run(action);Bar.Controls.Add(b);return b;
    }
    protected async Task Run(Func<Task> action)
    {
        if(busy)return;busy=true;Bar.Enabled=false;Content.Enabled=false;UseWaitCursor=true;
        try{await action();}
        catch(Exception ex) when(ex is InvalidOperationException or IOException or SocketException or JsonException or OperationCanceledException or ArgumentException or UnauthorizedAccessException){if(!IsDisposed){Status.Text="Thao tác chưa hoàn tất";MessageBox.Show(this,ex is OperationCanceledException?"Hết thời gian chờ. Tải lại để kiểm tra kết quả trước khi thử lại.":ex.Message,"Thư viện",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
        finally{busy=false;if(!IsDisposed){Bar.Enabled=true;Content.Enabled=true;UseWaitCursor=false;}}
    }
    protected static DataGridView Grid(bool editable=false)=>new(){Dock=DockStyle.Fill,ReadOnly=!editable,AllowUserToAddRows=false,AllowUserToDeleteRows=editable,AutoGenerateColumns=true,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false};
    protected static void Bind(DataGridView grid,TableResult result)
    {
        var table=new DataTable();foreach(var c in result.Cot)table.Columns.Add(c);
        foreach(var row in result.Dong)table.Rows.Add(row.Select(x=>(object?)x??DBNull.Value).ToArray());grid.DataSource=table;
    }
    protected static int SelectedId(DataGridView grid,string column)=>int.TryParse(grid.CurrentRow?.Cells[column].Value?.ToString(),out var id)?id:throw new InvalidOperationException("Chọn một dòng trước.");
    protected TitleItem? ChooseTitle()
    {
        using var p=new CatalogPicker<TitleItem>(Session,"TV2_TITLE_LIST","Chọn đầu tài liệu",("IdDauTaiLieu","Mã"),("NhanDe","Nhan đề"),("LoaiTaiLieu","Loại"));return p.ShowDialog(this)==DialogResult.OK?p.Selection:null;
    }
    protected StorageLocationItem? ChooseLocation()
    {
        using var p=new CatalogPicker<StorageLocationItem>(Session,"TV2_LOCATION_LIST","Chọn dãy kệ/ngăn",("DayKe","Dãy kệ"),("Ngan","Ngăn"));return p.ShowDialog(this)==DialogResult.OK?p.Selection:null;
    }
    protected CopyItem? ChooseCopy()
    {
        using var p=new CatalogPicker<CopyItem>(Session,"TV2_COPY_LIST","Chọn bản sách",("MaVach","Barcode"),("NhanDe","Nhan đề"),("TrangThai","Trạng thái"));return p.ShowDialog(this)==DialogResult.OK?p.Selection:null;
    }
    protected void Export(TableResult table,string name,string context)
    {
        using var save=new SaveFileDialog{Filter="CSV UTF-8|*.csv",FileName=name+".csv"};if(save.ShowDialog(this)!=DialogResult.OK)return;
        static string Cell(string? s){s??="";if(s.Length>0&&"=+-@\t\r".Contains(s[0]))s="'"+s;return "\""+s.Replace("\"","\"\"")+"\"";}
        File.WriteAllLines(save.FileName,new[]{Cell(context),string.Join(",",table.Cot.Select(Cell))}.Concat(table.Dong.Select(row=>string.Join(",",row.Select(Cell)))),new UTF8Encoding(true));Status.Text="Đã xuất: "+save.FileName;
    }
}
