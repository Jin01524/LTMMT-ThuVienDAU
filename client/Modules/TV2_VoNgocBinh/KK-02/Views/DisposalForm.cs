namespace ThuVienDAU.Catalog;
public sealed class DisposalForm : WorkForm
{
    private readonly DataGridView grid=Grid();private readonly TextBox search=new(){Width=190,PlaceholderText="Barcode / lý do",MaxLength=100};private int offset;
    public DisposalForm(string session):base(session,"KK-02 · Đề nghị và quyết định thanh lý")
    {
        Content.Controls.Add(grid);Bar.Controls.Add(search);Button("Tải danh sách",async()=>{offset=0;await LoadRows();});Button("Trang trước",async()=>{offset=Math.Max(0,offset-100);await LoadRows();});Button("Trang sau",async()=>{offset+=100;await LoadRows();});
        Button("Biên mục · Đề nghị",async()=>
        {
            var copy=ChooseCopy();if(copy is null)return;using var f=new CatalogEditor("Đề nghị thanh lý · "+copy.MaVach);var reason=f.TextField("Lý do đề nghị",null,500);var stock=f.TextField("Mã đợt đã hoàn tất (tùy chọn)",null,10);
            if(f.ShowDialog(this)!=DialogResult.OK)return;int? id=null;if(stock.Text.Length>0){if(!int.TryParse(stock.Text,out var n)||n<=0)throw new InvalidOperationException("Mã đợt không hợp lệ.");id=n;}
            await Api.CallAsync<object>("TV2_DISPOSAL_CREATE",new DisposalCreate(copy.IdBanTaiLieu,reason.Text,id),Session);await LoadRows();
        });
        Button("Quản lý · Duyệt/chốt",()=>Decide(true));Button("Quản lý · Từ chối",()=>Decide(false));
        Content.Controls.Add(new Label{Dock=DockStyle.Bottom,Height=46,Text="Biên mục lập đề nghị; Quản lý thư viện duyệt/chốt. Căn cứ quyết định lưu trong nhật ký.\nSách đang mượn không được thanh lý. Không xóa sách hoặc chứng từ; kiểm kê thiếu không tự thanh lý."});Shown+=async(_,_)=>await Run(LoadRows);
    }
    private async Task LoadRows(){Bind(grid,await Api.CallAsync<TableResult>("TV2_DISPOSAL_LIST",new CatalogQuery(search.Text,offset),Session));Status.Text=$"Từ dòng {offset+1}, tối đa 100 dòng/trang";}
    private async Task Decide(bool accept)
    {
        var id=SelectedId(grid,"IdThanhLy");using var f=new CatalogEditor((accept?"Duyệt/chốt":"Từ chối")+" đề nghị #"+id);var basis=f.TextField("Căn cứ quyết định",null,500);if(f.ShowDialog(this)!=DialogResult.OK)return;
        await Api.CallAsync<object>("TV2_DISPOSAL_DECIDE",new DisposalDecision(id,accept,basis.Text),Session);await LoadRows();
    }
}
