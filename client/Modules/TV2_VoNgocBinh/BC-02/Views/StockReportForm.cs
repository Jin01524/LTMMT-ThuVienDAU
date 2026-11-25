namespace ThuVienDAU.Catalog;
public sealed class StockReportForm : WorkForm
{
    private readonly DataGridView grid=Grid();private TableResult? loaded;private string context="";
    private readonly DateTimePicker from=new(){ShowCheckBox=true,Checked=false,Format=DateTimePickerFormat.Short,Width=130},to=new(){ShowCheckBox=true,Checked=false,Format=DateTimePickerFormat.Short,Width=130};
    private readonly TextBox ddc=new(){Width=100,MaxLength=64,PlaceholderText="DDC chính xác"};private readonly ComboBox type=new(){Width=130,DropDownStyle=ComboBoxStyle.DropDownList};
    public StockReportForm(string session):base(session,"BC-02 · Thống kê tài liệu và kho")
    {
        type.Items.AddRange(new[]{"","SACH","DO_AN","LUAN_VAN","KHAC"});type.SelectedIndex=0;Bar.Controls.Add(new Label{Text="Ngày nhập từ / đến",AutoSize=true,Padding=new Padding(0,8,0,0)});Bar.Controls.AddRange(new Control[]{from,to,ddc,type});Content.Controls.Add(grid);
        Button("Lập báo cáo",LoadReport);Button("Xuất CSV",()=>{if(loaded is null)throw new InvalidOperationException("Lập báo cáo trước.");Export(loaded,"Thong_ke_kho",context);return Task.CompletedTask;});
        Button("Báo cáo kiểm kê",()=>{using var f=new StocktakeForm(Session);f.ShowDialog(this);return Task.CompletedTask;});
        Content.Controls.Add(new Label{Dock=DockStyle.Bottom,Height=55,Text="Không chọn ngày: tồn kho hiện tại, kể cả đầu tài liệu chưa có bản. Chọn ngày: lọc theo ngày nhập bản sách.\nSố đầu đếm DISTINCT, không cộng thêm do nhiều bản/MARC. MARC đã kiểm chỉ tính khi còn khớp phiên đầu tài liệu.\nCác bản sách chưa biết ngày nhập không được tính khi lọc khoảng ngày."});Shown+=async(_,_)=>await Run(LoadReport);
    }
    private async Task LoadReport()
    {
        var q=new ReportFilter(from.Checked?DateOnly.FromDateTime(from.Value):null,to.Checked?DateOnly.FromDateTime(to.Value):null,string.IsNullOrWhiteSpace(ddc.Text)?null:ddc.Text,type.Text);
        loaded=await Api.CallAsync<TableResult>("TV2_REPORT",q,Session);context=$"Ngày nhập {q.TuNgay?.ToString()??"không giới hạn"} → {q.DenNgay?.ToString()??"không giới hạn"}; DDC={q.MaDDC??"tất cả"}; loại={q.LoaiTaiLieu}; thời điểm {DateTime.Now:dd/MM/yyyy HH:mm}";Bind(grid,loaded);Status.Text=context;
    }
}
