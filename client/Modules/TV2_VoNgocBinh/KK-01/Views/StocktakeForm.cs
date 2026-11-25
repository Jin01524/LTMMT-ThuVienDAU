namespace ThuVienDAU.Catalog;
public sealed class StocktakeForm : WorkForm
{
    private readonly DataGridView rounds=Grid(),details=Grid();private int offset,roundOffset;
    private readonly TextBox name=new(){Width=210,MaxLength=200,PlaceholderText="Tên đợt kiểm kê"};
    private readonly TextBox barcode=new(){Width=180,MaxLength=100,PlaceholderText="Quét/nhập barcode"};
    private readonly CheckBox damaged=new(){Text="Hư hỏng",AutoSize=true};
    private StorageLocationItem? location;private int? currentId;
    public StocktakeForm(string session):base(session,"KK-01 · Kiểm kê kho")
    {
        var split=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,Size=new Size(1100,550),SplitterDistance=190};split.Panel1.Controls.Add(rounds);split.Panel2.Controls.Add(details);Content.Controls.Add(split);
        Bar.Controls.Add(name);Button("Chọn vị trí",()=>{location=ChooseLocation();Status.Text=location is null?"Phạm vi toàn kho":$"Vị trí {location.DayKe}/{location.Ngan}";return Task.CompletedTask;});Button("Toàn kho",()=>{location=null;Status.Text="Phạm vi toàn kho";return Task.CompletedTask;});
        Button("Lập danh sách",async()=>{await Api.CallAsync<object>("TV2_STOCK_CREATE",new StocktakeCreate(name.Text,location?.IdViTri),Session);await LoadRounds();});
        Button("Tải đợt",async()=>{roundOffset=0;await LoadRounds();});Button("Đợt trước",async()=>{roundOffset=Math.Max(0,roundOffset-100);await LoadRounds();});Button("Đợt sau",async()=>{roundOffset+=100;await LoadRounds();});Button("Xem đợt",async()=>{currentId=SelectedId(rounds,"IdKiemKe");offset=0;await LoadDetail();});
        Button("Bắt đầu kiểm",()=>Action("DANG_KIEM"));Button("Chốt kiểm kê",()=>Action("HOAN_TAT"));Button("Hủy đợt",()=>Action("HUY"));
        Bar.Controls.Add(barcode);Bar.Controls.Add(damaged);Button("Ghi nhận quét",Scan);barcode.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Run(Scan);}};
        Button("Chi tiết trước",async()=>{offset=Math.Max(0,offset-100);await LoadDetail();});Button("Chi tiết sau",async()=>{offset+=100;await LoadDetail();});
        Button("Xuất toàn bộ đợt",async()=>{Require();var all=new List<string?[]>();TableResult page;int pageOffset=0;do{page=await Api.CallAsync<TableResult>("TV2_STOCK_DETAIL",new StocktakeQuery(currentId!.Value,pageOffset),Session);all.AddRange(page.Dong);pageOffset+=100;}while(page.Dong.Length==100);Export(new(page.Cot,all.ToArray()),"Kiem_ke_"+currentId,$"Đợt {currentId}, xuất {DateTime.Now:dd/MM/yyyy HH:mm}");});
        Content.Controls.Add(new Label{Dock=DockStyle.Bottom,Height=45,Text="Lập danh sách chụp vị trí dự kiến; sách đang mượn/thanh lý được loại. Chọn vị trí thực tế trước khi quét.\nChốt đánh dấu dòng chưa kiểm là thiếu, không tự đổi trạng thái sách. Đợt đã chốt/hủy không thể quét lại."});
        Shown+=async(_,_)=>await Run(LoadRounds);
    }
    private void Require(){if(currentId is null)throw new InvalidOperationException("Chọn đợt rồi bấm Xem đợt trước.");}
    private async Task LoadRounds(){Bind(rounds,await Api.CallAsync<TableResult>("TV2_STOCK_LIST",new CatalogQuery("",roundOffset),Session));Status.Text=$"Đợt từ dòng {roundOffset+1}. Chọn dòng và bấm Xem đợt.";}
    private async Task LoadDetail(){Require();var table=await Api.CallAsync<TableResult>("TV2_STOCK_DETAIL",new StocktakeQuery(currentId!.Value,offset),Session);Bind(details,table);Status.Text=$"Đợt {currentId}, từ dòng {offset+1}, {table.Dong.Length} dòng";}
    private async Task Action(string state){Require();if(MessageBox.Show(this,$"Đổi đợt {currentId} sang {state}?","Kiểm kê",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;await Api.CallAsync<object>("TV2_STOCK_ACTION",new StocktakeAction(currentId!.Value,state),Session);await LoadRounds();await LoadDetail();}
    private async Task Scan(){Require();if(location is null)throw new InvalidOperationException("Chọn dãy kệ/ngăn thực tế trước khi quét.");await Api.CallAsync<object>("TV2_STOCK_SCAN",new StocktakeScan(currentId!.Value,barcode.Text,location.IdViTri,damaged.Checked),Session);barcode.Clear();await LoadDetail();barcode.Focus();}
}
