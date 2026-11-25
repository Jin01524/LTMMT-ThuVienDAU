using System.Net.Sockets;
using System.Text.Json;
namespace ThuVienDAU.Catalog;
internal sealed class CatalogPicker<T> : Form where T : class
{
    private readonly DataGridView grid=new() { Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AutoGenerateColumns=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill };
    private readonly TextBox search=new() { Width=290,MaxLength=100,PlaceholderText="Nhập mã hoặc tên để tìm" };
    private readonly Button next=new(){Text="Trang sau",AutoSize=true},previous=new(){Text="Trang trước",AutoSize=true};
    private readonly Button choose=new(){Text="Chọn",AutoSize=true};
    private readonly FlowLayoutPanel bar=new(){Dock=DockStyle.Top,Height=50,Padding=new Padding(8)};
    private readonly string session,command;private int offset;private bool busy;
    public T? Selection { get;private set; }
    public CatalogPicker(string session,string command,string title,params (string Property,string Label)[] columns)
    {
        this.session=session;this.command=command;Text=title;Size=new Size(880,540);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
        foreach(var (property,label) in columns)grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=property,HeaderText=label});
        var find=new Button{Text="Tìm",AutoSize=true};bar.Controls.AddRange(new Control[]{search,find,previous,next,choose});Controls.Add(grid);Controls.Add(bar);
        choose.Click+=(_,_)=>Accept();grid.CellDoubleClick+=(_,e)=>{if(e.RowIndex>=0)Accept();};
        find.Click+=async(_,_)=>{offset=0;await LoadRows();};previous.Click+=async(_,_)=>{offset=Math.Max(0,offset-100);await LoadRows();};next.Click+=async(_,_)=>{offset+=100;await LoadRows();};
        search.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;offset=0;await LoadRows();}};
        Shown+=async(_,_)=>await LoadRows();
    }
    private void Accept(){if(busy)return;if(grid.CurrentRow?.DataBoundItem is T item){Selection=item;DialogResult=DialogResult.OK;Close();}}
    private async Task LoadRows()
    {
        if(busy)return;busy=true;bar.Enabled=false;grid.Enabled=false;
        try{var rows=await new CatalogClientController().CallAsync<T[]>(command,new CatalogQuery(search.Text,offset),session);if(IsDisposed)return;grid.DataSource=rows;choose.Enabled=rows.Length>0;previous.Enabled=offset>0;next.Enabled=rows.Length==100;}
        catch(Exception ex) when(ex is InvalidOperationException or IOException or SocketException or JsonException or OperationCanceledException){if(!IsDisposed){grid.DataSource=null;choose.Enabled=false;MessageBox.Show(this,"Chưa tải được danh sách: "+ex.Message,"Chọn dữ liệu");}}
        finally{busy=false;if(!IsDisposed){bar.Enabled=true;grid.Enabled=true;}}
    }
}
