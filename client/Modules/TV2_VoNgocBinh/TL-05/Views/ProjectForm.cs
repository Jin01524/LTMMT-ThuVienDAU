using System.ComponentModel;
using System.Diagnostics;
namespace ThuVienDAU.Catalog;
public sealed class ProjectForm : WorkForm
{
    private readonly DataGridView members=Grid();
    private readonly ListBox files=new(){Dock=DockStyle.Fill,DisplayMember=nameof(AttachmentItem.TenTep)};
    private readonly Label major=new(){AutoSize=true,Text="Chưa chọn ngành"};
    private readonly TextBox course=new(){Width=100,MaxLength=30,PlaceholderText="Khóa học"};
    private ProjectItem? item;private string? majorCode;
    private BindingList<ProjectMember> people=new();
    public ProjectForm(string session):base(session,"TL-05 · Đồ án, luận văn và PDF")
    {
        var split=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,SplitterDistance=280,Size=new Size(1100,560)};split.Panel1.Controls.Add(members);split.Panel2.Controls.Add(files);Content.Controls.Add(split);
        Button("Chọn đồ án/luận văn",async()=>{var title=ChooseTitle();if(title is not null){item=await Api.CallAsync<ProjectItem>("TV2_PROJECT_GET",new TitleQuery(title.IdDauTaiLieu),Session);Display();}});
        Button("Chọn ngành",()=>{using var p=new CatalogPicker<MajorItem>(Session,"TV2_MAJOR_LIST","Chọn ngành",("MaNganh","Mã"),("TenNganh","Ngành"));if(p.ShowDialog(this)==DialogResult.OK){majorCode=p.Selection!.MaNganh;major.Text=p.Selection.TenNganh;}return Task.CompletedTask;});Bar.Controls.Add(major);Bar.Controls.Add(course);
        Button("Thêm sinh viên",()=>AddMember("THUC_HIEN"));Button("Thêm hướng dẫn",()=>AddMember("HUONG_DAN"));Button("Bỏ thành viên",()=>{if(members.CurrentRow?.DataBoundItem is ProjectMember m)people.Remove(m);return Task.CompletedTask;});
        Button("Lưu hồ sơ",async()=>{Require();item=await Api.CallAsync<ProjectItem>("TV2_PROJECT_SAVE",item! with{MaNganh=majorCode,KhoaHoc=course.Text,ThanhVien=people.ToArray()},Session);Display();});
        Button("Tải lại",Reload);Button("Thêm PDF",Upload);Button("Tải/mở PDF",Download);
        Content.Controls.Add(new Label{Dock=DockStyle.Bottom,Height=44,Text="Chọn hồ sơ SV/GV đã nhập từ trường. Tệp PDF tối đa 20 MiB/tệp, lưu ở server, truyền theo khối TCP.\nLưu hồ sơ trước khi thêm PDF; quyền tệp hiện dành cho Biên mục/Quản lý, chưa cung cấp cho độc giả."});
    }
    private void Require(){if(item is null)throw new InvalidOperationException("Chọn đồ án/luận văn trước.");}
    private void Display(){majorCode=item!.MaNganh;major.Text=majorCode??"Chưa chọn ngành";course.Text=item.KhoaHoc??"";people=new(item.ThanhVien.ToList());members.DataSource=people;files.DataSource=item.DanhSachTep;Status.Text=$"Đề tài #{item.IdDauTaiLieu}, phiên {item.PhienBan}, {item.DanhSachTep.Length} tệp. Lưu hồ sơ trước khi thêm PDF.";}
    private async Task Reload(){Require();item=await Api.CallAsync<ProjectItem>("TV2_PROJECT_GET",new TitleQuery(item!.IdDauTaiLieu),Session);Display();}
    private Task AddMember(string role)
    {
        Require();using var p=new CatalogPicker<ReaderReference>(Session,"TV2_READER_LIST","Chọn hồ sơ trường",("MaDinhDanhTruong","Mã trường"),("HoTen","Họ tên"),("LoaiDocGia","Đối tượng"));
        if(p.ShowDialog(this)==DialogResult.OK){var reader=p.Selection!;var m=new ProjectMember(reader.IdDocGia,role,reader.HoTen,reader.MaDinhDanhTruong);if(!people.Any(x=>x.IdDocGia==m.IdDocGia&&x.VaiTro==m.VaiTro))people.Add(m);}return Task.CompletedTask;
    }
    private async Task Upload()
    {
        Require();using var open=new OpenFileDialog{Filter="PDF|*.pdf"};if(open.ShowDialog(this)!=DialogResult.OK)return;
        var size=new FileInfo(open.FileName).Length;if(size is <5 or >20*1024*1024)throw new InvalidOperationException("PDF từ 5 byte đến 20 MiB.");
        var ticket=await Api.CallAsync<FileTicket>("TV2_FILE_BEGIN",new FileBegin(item!.IdDauTaiLieu,item.PhienBan,Path.GetFileName(open.FileName),(int)size),Session);
        await using var input=File.OpenRead(open.FileName);var buffer=new byte[192*1024];int count;
        while((count=await input.ReadAsync(buffer))>0){ticket=await Api.CallAsync<FileTicket>("TV2_FILE_CHUNK",new FileChunk(ticket.MaTep,ticket.ViTri,Convert.ToBase64String(buffer,0,count)),Session);Status.Text=$"Đang gửi {ticket.ViTri}/{size} byte";}
        item=await Api.CallAsync<ProjectItem>("TV2_FILE_FINISH",ticket,Session);Display();
    }
    private async Task Download()
    {
        Require();if(files.SelectedItem is not AttachmentItem attachment)throw new InvalidOperationException("Chọn PDF trong danh sách.");
        using var save=new SaveFileDialog{Filter="PDF|*.pdf",FileName=Path.GetFileName(attachment.TenTep)};if(save.ShowDialog(this)!=DialogResult.OK)return;
        var temp=save.FileName+"."+Guid.NewGuid().ToString("N")+".part";
        try
        {
            await using(var output=new FileStream(temp,FileMode.CreateNew,FileAccess.Write))
            {
                int offset=0,total;do{var chunk=await Api.CallAsync<FileData>("TV2_FILE_READ",new FileRead(item!.IdDauTaiLieu,Path.GetFileNameWithoutExtension(attachment.DuongDan),offset),Session);var bytes=Convert.FromBase64String(chunk.DuLieu);if(bytes.Length==0)throw new InvalidDataException("Tệp thiếu dữ liệu.");await output.WriteAsync(bytes);offset+=bytes.Length;total=chunk.KichThuoc;}while(offset<total);
            }
            File.Move(temp,save.FileName,true);Status.Text="Đã tải PDF: "+save.FileName;
            if(MessageBox.Show(this,"Mở PDF vừa tải?","PDF",MessageBoxButtons.YesNo)==DialogResult.Yes)Process.Start(new ProcessStartInfo(save.FileName){UseShellExecute=true});
        }
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
