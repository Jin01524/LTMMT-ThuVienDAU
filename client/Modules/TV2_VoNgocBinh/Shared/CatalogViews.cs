using System.Net.Sockets;
using System.Text.Json;
namespace ThuVienDAU.Catalog;
public sealed class CatalogWorkspaceForm : Form
{
    public CatalogWorkspaceForm(ThuVienDAU.Models.SessionInfo? session=null)
    {
        Text="TV2 · Biên mục thư viện"; Size=new Size(710,430); StartPosition=FormStartPosition.CenterParent;
        var layout=new FlowLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(24),FlowDirection=FlowDirection.TopDown,WrapContents=false };
        var token=new TextBox { Width=620,UseSystemPasswordChar=true,PlaceholderText="Phiên Biên mục do server cấp (token demo chỉ dùng trên loopback)" };
        layout.Controls.Add(new Label { Text="Tài liệu · DDC23 tiếng Việt rút gọn",AutoSize=true,Font=new Font("Segoe UI",16,FontStyle.Bold) });
        layout.Controls.Add(new Label { Text="HT-02 chưa có. Phiên thử: Biên mục hoặc Quản lý đang hoạt động (chỉ loopback).",AutoSize=true });
        layout.Controls.Add(token);
        if(session is not null){token.Text=session.SessionId;token.Visible=false;}
        var sessionLabel=layout.Controls.OfType<Label>().Last();
        if(session is not null)sessionLabel.Text=$"Đã đăng nhập: {session.DisplayName} ({session.Role})";
        foreach(var name in new[]{"TL-01 · Danh mục DDC23","TL-02 · Hồ sơ đầu tài liệu","TL-04 · Bản sách","TL-04 · Dãy kệ/ngăn"})
        {
            var button=new Button { Text=name,Width=620,Height=44 };
            button.Click+=(_,_)=> { if(string.IsNullOrWhiteSpace(token.Text)) { MessageBox.Show(this,"Nhập phiên Biên mục trước.");return; } using Form form=name.StartsWith("TL-01") ? new DdcForm(token.Text.Trim()) : name.StartsWith("TL-02") ? new TitleForm(token.Text.Trim()) : name.EndsWith("Bản sách") ? new CopyForm(token.Text.Trim()) : new LocationForm(token.Text.Trim()); form.ShowDialog(this); };
            layout.Controls.Add(button);
        }
        Controls.Add(layout);
    }
}
public abstract class CatalogListForm<T> : Form where T : class
{
    protected readonly CatalogClientController Controller=new();
    protected readonly string Session;
    protected readonly Panel Body=new() { Dock=DockStyle.Fill };
    private readonly string listCommand,saveCommand;
    protected readonly DataGridView Grid=new() { Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false };
    private readonly TextBox query=new() { Width=260,PlaceholderText="Tìm mã hoặc tên (nội bộ Biên mục)",MaxLength=100 };
    private readonly Label status=new() { AutoSize=true,Padding=new Padding(8) };
    private readonly FlowLayoutPanel toolbar=new() { Dock=DockStyle.Top,Height=52,Padding=new Padding(8) };
    private readonly Button next=new() { Text="Trang sau",AutoSize=true },previous=new() { Text="Trang trước",AutoSize=true };
    private int offset;
    private bool busy;
    protected CatalogListForm(string session,string title,string list,string save)
    {
        Session=session;listCommand=list;saveCommand=save;Text=title;Size=new Size(1180,700);MinimumSize=new Size(900,540);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
        var search=new Button { Text="Tìm / tải lại",AutoSize=true };var add=new Button { Text="Thêm mới",AutoSize=true };var edit=new Button { Text="Xem / sửa",AutoSize=true };
        toolbar.Controls.AddRange(new Control[]{query,search,add,edit,previous,next});
        search.Click+=async(_,_)=>{offset=0;await LoadRows();}; previous.Click+=async(_,_)=>{offset=Math.Max(0,offset-100);await LoadRows();}; next.Click+=async(_,_)=>{offset+=100;await LoadRows();};
        add.Click+=async(_,_)=>await EditAndSave(null); edit.Click+=async(_,_)=>{if(Grid.CurrentRow?.DataBoundItem is T row) await EditAndSave(row);else MessageBox.Show(this,"Chọn một dòng để xem/sửa.");};
        Grid.CellDoubleClick+=async(_,e)=>{ if(e.RowIndex>=0) await EditAndSave(Grid.Rows[e.RowIndex].DataBoundItem as T); };
        query.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;offset=0;await LoadRows();}};
        Body.Controls.Add(Grid);Controls.Add(Body);Controls.Add(toolbar);Controls.Add(new Panel { Dock=DockStyle.Bottom,Height=36,Controls={status} });
        Grid.DataBindingComplete+=(_,_)=>
        {
            var labels=new Dictionary<string,string> { ["MaDDC"]="Mã DDC",["MaCha"]="Mã cha",["TenMuc"]="Tên mục",["NguonThamKhao"]="Nguồn tham khảo",["DaDoiChieu"]="Đã đối chiếu",["ViTriTraBanIn"]="Tập/trang bản in",["CachGhepSo"]="Căn cứ ghép số",["IdNguoiDoiChieu"]="Mã người đối chiếu",["ThoiDiemDoiChieu"]="Thời điểm đối chiếu",["IdDauTaiLieu"]="Mã đầu tài liệu",["NhanDe"]="Nhan đề",["LoaiTaiLieu"]="Loại tài liệu",["TrangThaiPhanLoai"]="Phân loại",["ViTriTraDDC"]="Vị trí tra DDC",["DangHienThi"]="Hiển thị nội bộ",["IdBanTaiLieu"]="Mã bản sách",["SoDangKyCaBiet"]="Số ĐKCB",["MaVach"]="Barcode",["IdViTri"]="Mã vị trí",["DayKe"]="Dãy kệ",["Ngan"]="Ngăn",["KyHieuXepGia"]="Ký hiệu xếp giá",["TrangThai"]="Trạng thái",["TinhTrang"]="Tình trạng",["GiaBia"]="Giá bìa (đồng)",["NgayNhap"]="Ngày nhập" };
            foreach(DataGridViewColumn col in Grid.Columns) { if(labels.TryGetValue(col.DataPropertyName,out var label))col.HeaderText=label;if(col.DataPropertyName is "Revision" or "PhienBan")col.Visible=false; }
        };
        Shown+=async(_,_)=>await LoadRows();
    }
    protected abstract T? EditItem(T? item);
    protected virtual void Loaded(T[] rows) { }
    private async Task LoadRows()
    {
        if(busy)return; SetBusy(true);
        try { var rows=await Controller.CallAsync<T[]>(listCommand,new CatalogQuery(query.Text,offset),Session);if(IsDisposed)return;Grid.DataSource=rows;Loaded(rows);previous.Enabled=offset>0;next.Enabled=rows.Length==100;status.Text=$"{rows.Length} dòng · từ {offset+1} · tối đa 100 dòng/trang"; }
        catch(Exception ex) when(ex is InvalidOperationException or IOException or SocketException or JsonException or OperationCanceledException) { ShowError(ex); }
        finally { SetBusy(false); }
    }
    private async Task EditAndSave(T? selected)
    {
        if(busy)return;
        var item=EditItem(selected);if(item is null)return;SetBusy(true);
        try { await Controller.CallAsync<T>(saveCommand,item,Session); if(!IsDisposed) status.Text="Đã lưu và ghi nhật ký."; }
        catch(Exception ex) when(ex is InvalidOperationException or IOException or SocketException or JsonException or OperationCanceledException) { ShowError(ex); }
        finally { SetBusy(false); }
        if(!IsDisposed) await LoadRows();
    }
    private void ShowError(Exception ex) { if(!IsDisposed) MessageBox.Show(this,ex is OperationCanceledException ? "Hết thời gian chờ server. Tải lại để kiểm tra trước khi lưu lại." : ex is SocketException ? "Không kết nối được server TCP. Kiểm tra địa chỉ/cổng." : ex.Message,"Biên mục",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
    private void SetBusy(bool value) { busy=value;if(IsDisposed)return;toolbar.Enabled=!value;Grid.Enabled=!value;UseWaitCursor=value; }
}
public sealed class CatalogEditor : Form
{
    private readonly TableLayoutPanel fields=new() { Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(16),ColumnCount=2 };
    public CatalogEditor(string title)
    {
        Text=title;Size=new Size(700,600);MinimumSize=new Size(620,480);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,210));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        var buttons=new FlowLayoutPanel { Dock=DockStyle.Bottom,Height=55,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(8) };
        var save=new Button { Text="Lưu",DialogResult=DialogResult.OK,Width=100,Height=34 };var cancel=new Button { Text="Hủy",DialogResult=DialogResult.Cancel,Width=100,Height=34 };buttons.Controls.AddRange(new Control[]{cancel,save});AcceptButton=save;CancelButton=cancel;Controls.Add(fields);Controls.Add(buttons);
    }
    public void ValidateOnSave(Func<string?> validate)
    {
        FormClosing+=(_,e)=> { if(DialogResult!=DialogResult.OK)return;var error=validate();if(error is not null){e.Cancel=true;DialogResult=DialogResult.None;MessageBox.Show(this,error,"Kiểm tra dữ liệu",MessageBoxButtons.OK,MessageBoxIcon.Warning);} };
    }
    public TextBox Picker(string label,string text,Action<TextBox> choose,Action<TextBox>? clear=null)
    {
        var box=new TextBox { Text=text,ReadOnly=true,Width=250 };var button=new Button { Text="Chọn",Width=65,Height=29 };
        var panel=new FlowLayoutPanel { Dock=DockStyle.Fill,AutoSize=true,WrapContents=true };panel.Controls.AddRange(new Control[]{box,button});button.Click+=(_,_)=>choose(box);
        if(clear is not null){var reset=new Button { Text="Bỏ",Width=50,Height=29 };reset.Click+=(_,_)=>clear(box);panel.Controls.Add(reset);}
        Add(label,panel);return box;
    }
    public ComboBox LabeledChoice(string label,string value,Dictionary<string,string> values)
    {
        var choices=values.Select(x=>new CatalogChoice(x.Key,x.Value)).ToArray();var input=new ComboBox { Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList };input.Items.AddRange(choices);input.SelectedItem=choices.First(x=>x.Code==value);Add(label,input);return input;
    }
    public DateTimePicker DateField(string label,DateOnly? value)
    {
        var input=new DateTimePicker { Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",ShowCheckBox=true,Value=value?.ToDateTime(TimeOnly.MinValue)??DateTime.Today,Checked=value.HasValue,Dock=DockStyle.Fill };Add(label,input);return input;
    }
    public TextBox TextField(string label,string? value,int max,bool readOnly=false)
    { var input=new TextBox { Text=value??"",MaxLength=max,ReadOnly=readOnly,Dock=DockStyle.Fill };Add(label,input);return input; }
    public ComboBox Choice(string label,string value,params string[] values)
    { var c=new ComboBox { Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList };c.Items.AddRange(values);c.SelectedItem=value;Add(label,c);return c; }
    public CheckBox Check(string label,bool value) { var c=new CheckBox { Checked=value,Dock=DockStyle.Fill };Add(label,c);return c; }
    public void Hint(string text) { var label=new Label { Text=text,AutoSize=true,MaximumSize=new Size(420,0) };Add("Lưu ý",label); }
    private void Add(string label,Control input)
    { var row=fields.RowCount++;fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));fields.Controls.Add(new Label { Text=label,AutoSize=true,Padding=new Padding(0,5,0,10) },0,row);input.Margin=new Padding(3,5,3,10);fields.Controls.Add(input,1,row); }
}

public sealed record CatalogChoice(string Code,string Label) { public override string ToString()=>Label; }
