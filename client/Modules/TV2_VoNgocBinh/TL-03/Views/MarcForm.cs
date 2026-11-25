using System.ComponentModel;
namespace ThuVienDAU.Catalog;
public sealed class MarcForm : WorkForm
{
    private readonly DataGridView grid=Grid(true);
    private readonly TextBox leader=new(){Width=240,MaxLength=24,Text="00000nam a2200000   4500"};
    private MarcItem? item;private TitleItem? title;
    private BindingList<MarcLine> rows=new();
    public MarcForm(string session):base(session,"TL-03 · Biên mục MARC21")
    {
        Content.Controls.Add(grid);Bar.Controls.Add(new Label{Text="Leader",AutoSize=true,Padding=new Padding(0,9,0,0)});Bar.Controls.Add(leader);
        Button("Chọn tài liệu",async()=>{var t=ChooseTitle();if(t is not null){title=t;await LoadRecord();}});
        Button("Tải lại",LoadRecord);Button("Mẫu cơ bản",()=>{Template();return Task.CompletedTask;});
        Button("Thêm dòng",()=>{rows.Add(new(){Nhom=rows.Count==0?1:rows.Max(x=>x.Nhom)+1});return Task.CompletedTask;});
        Button("Dòng lên",()=>{MoveLine(-1);return Task.CompletedTask;});Button("Dòng xuống",()=>{MoveLine(1);return Task.CompletedTask;});
        Button("Lưu nháp",Save);Button("Kiểm tra/chốt",async()=>{if(item is null)throw new InvalidOperationException("Chọn và lưu biểu ghi trước.");grid.EndEdit();if(Dirty())throw new InvalidOperationException("Lưu nháp thay đổi trước khi kiểm tra.");item=await Api.CallAsync<MarcItem>("TV2_MARC_CHECK",new MarcCheck(item.IdDauTaiLieu,item.PhienBan,item.PhienBanHoSo),Session);await LoadRecord();});
        var hint=new Label{Dock=DockStyle.Bottom,Height=54,Text="Mỗi nhóm = một lần xuất hiện trường. Trường con cùng nhóm ở các dòng liên tiếp; giữ nguyên thứ tự.\n082: chỉ thị 1 = 1 (rút gọn), $a khớp DDC đã xác nhận, $2 = 23. Delete để bỏ dòng; trường điều khiển không có trường con."};Content.Controls.Add(hint);
    }
    private bool Dirty()=>item is null||leader.Text!=item.DauBieuMARC||!System.Text.Json.JsonElement.DeepEquals(MarcEditorCodec.Encode(rows),item.Truong);
    private async Task LoadRecord()
    {
        if(title is null)throw new InvalidOperationException("Chọn đầu tài liệu trước.");item=await Api.CallAsync<MarcItem>("TV2_MARC_GET",new TitleQuery(title.IdDauTaiLieu),Session);leader.Text=item.DauBieuMARC;rows=new(MarcEditorCodec.Decode(item.Truong));grid.DataSource=rows;if(grid.Columns[nameof(MarcLine.NoiDung)] is DataGridViewColumn column)column.AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill;
        Status.Text=$"#{title.IdDauTaiLieu} · {title.NhanDe} · {item.TrangThai} · phiên {item.PhienBan}. Thay đổi hồ sơ đầu tài liệu cần kiểm MARC lại.";
    }
    private void Template()
    {
        if(title is null)throw new InvalidOperationException("Chọn đầu tài liệu trước.");if(rows.Count>0&&MessageBox.Show(this,"Thay nội dung đang nhập bằng mẫu mới?","Mẫu MARC",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        rows=new(new List<MarcLine>{new(){Nhom=1,NhanTruong="001",ChiThi1=" ",ChiThi2=" ",MaTruongCon="",NoiDung="DAU-"+title.IdDauTaiLieu},new(){Nhom=2,NhanTruong="005",ChiThi1=" ",ChiThi2=" ",MaTruongCon="",NoiDung=DateTime.UtcNow.ToString("yyyyMMddHHmmss.0")},new(){Nhom=3,NhanTruong="008",ChiThi1=" ",ChiThi2=" ",MaTruongCon="",NoiDung=new string(' ',40)},new(){Nhom=4,NhanTruong="245",NoiDung=title.NhanDe}});
        if(title.MaDDC is not null){rows.Add(new(){Nhom=5,NhanTruong="082",ChiThi1="1",ChiThi2="4",MaTruongCon="a",NoiDung=title.MaDDC});rows.Add(new(){Nhom=5,NhanTruong="082",ChiThi1="1",ChiThi2="4",MaTruongCon="2",NoiDung="23"});}
        grid.DataSource=rows;Status.Text="Mẫu là nháp, cần điền 008 và thông tin từ tài liệu thực tế trước khi kiểm.";
    }
    private void MoveLine(int step){grid.EndEdit();var i=grid.CurrentRow?.Index??-1;var target=i+step;if(i<0||target<0||target>=rows.Count)return;var row=rows[i];rows.RemoveAt(i);rows.Insert(target,row);grid.CurrentCell=grid.Rows[target].Cells[0];}
    private async Task Save(){if(item is null)throw new InvalidOperationException("Chọn đầu tài liệu trước.");grid.EndEdit();item=await Api.CallAsync<MarcItem>("TV2_MARC_SAVE",item with{DauBieuMARC=leader.Text,Truong=MarcEditorCodec.Encode(rows)},Session);await LoadRecord();}
}
