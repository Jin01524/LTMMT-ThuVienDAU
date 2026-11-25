using System.Globalization;
namespace ThuVienDAU.Catalog;
public sealed class CopyForm : CatalogListForm<CopyItem>
{
    public CopyForm(string session):base(session,"TL-04 · Bản sách và vị trí kho","TV2_COPY_LIST","TV2_COPY_SAVE")
    {
        Grid.DataBindingComplete+=(_,_)=>
        {
            foreach(var id in new[]{"IdBanTaiLieu","IdDauTaiLieu","IdViTri"}) if(Grid.Columns[id] is DataGridViewColumn column)column.Visible=false;
            var index=0;foreach(var name in new[]{"SoDangKyCaBiet","MaVach","NhanDe","DayKe","Ngan","KyHieuXepGia","TrangThai","TinhTrang","GiaBia","NgayNhap"}) if(Grid.Columns[name] is DataGridViewColumn column)column.DisplayIndex=index++;
            if(Grid.Columns["NhanDe"] is DataGridViewColumn title){title.AutoSizeMode=DataGridViewAutoSizeColumnMode.None;title.Width=260;}
            if(Grid.Columns["GiaBia"] is DataGridViewColumn price)price.DefaultCellStyle.Format="N0";
            if(Grid.Columns["NgayNhap"] is DataGridViewColumn date)date.DefaultCellStyle.Format="dd/MM/yyyy";
        };
        Grid.CellFormatting+=(_,e)=>
        {
            if(e.ColumnIndex<0 || Grid.Columns[e.ColumnIndex].DataPropertyName is not ("TrangThai" or "TinhTrang") || e.Value is not string code)return;
            e.Value=code switch {"DANG_XU_LY"=>"Đang xử lý","SAN_SANG"=>"Sẵn sàng","DANG_MUON"=>"Đang mượn","THAT_LAC"=>"Thất lạc","MAT"=>"Mất","THANH_LY"=>"Đã thanh lý","HU_HONG"=>"Hư hỏng","TOT"=>"Tốt","CU"=>"Cũ",_=>code};e.FormattingApplied=true;
        };
    }
    protected override CopyItem? EditItem(CopyItem? item)
    {
        if(item is not null && !new[]{"DANG_XU_LY","SAN_SANG","HU_HONG"}.Contains(item.TrangThai)){MessageBox.Show(this,"Bản sách đang mượn, mất/thất lạc hoặc thanh lý: xem trên danh sách; xử lý tại phân hệ tương ứng.");return null;}
        using var editor=new CatalogEditor(item is null?"Thêm bản sách":"Sửa bản sách · "+item.SoDangKyCaBiet){Height=740,Width=800};
        var titleId=item?.IdDauTaiLieu??0;int? locationId=item?.IdViTri;
        if(item is not null)editor.TextField("Đầu tài liệu",item.NhanDe,500,true);
        else editor.Picker("Đầu tài liệu","",box=>
        {
            using var picker=new CatalogPicker<TitleItem>(Session,"TV2_TITLE_LIST","Chọn đầu tài liệu",("IdDauTaiLieu","Mã"),("NhanDe","Nhan đề"),("LoaiTaiLieu","Loại"),("MaDDC","DDC"));
            if(picker.ShowDialog(editor)==DialogResult.OK && picker.Selection is TitleItem title){titleId=title.IdDauTaiLieu;box.Text=title.NhanDe;}
        });
        var number=editor.TextField("Số đăng ký cá biệt",item?.SoDangKyCaBiet,50);var barcode=editor.TextField("Barcode",item?.MaVach,100);
        editor.Picker("Dãy kệ / ngăn",item?.IdViTri is null?"":$"{item.DayKe} / {item.Ngan}",box=>
        {
            using var picker=new CatalogPicker<StorageLocationItem>(Session,"TV2_LOCATION_LIST","Chọn vị trí",("DayKe","Dãy kệ"),("Ngan","Ngăn"));
            if(picker.ShowDialog(editor)==DialogResult.OK && picker.Selection is StorageLocationItem location){locationId=location.IdViTri;box.Text=location.DayKe+" / "+location.Ngan;}
        },box=>{locationId=null;box.Text="";});
        var callNumber=editor.TextField("Ký hiệu xếp giá",item?.KyHieuXepGia,100);
        var state=editor.LabeledChoice("Trạng thái",item?.TrangThai??"DANG_XU_LY",new(){["DANG_XU_LY"]="Đang xử lý",["SAN_SANG"]="Sẵn sàng",["HU_HONG"]="Hư hỏng"});
        var condition=editor.LabeledChoice("Tình trạng",item?.TinhTrang??"TOT",new(){["TOT"]="Tốt",["CU"]="Cũ",["HU_HONG"]="Hư hỏng"});
        var price=editor.TextField("Giá bìa (đồng, có thể trống)",item?.GiaBia?.ToString(CultureInfo.InvariantCulture),12);
        var date=editor.DateField("Ngày nhập (bỏ chọn nếu trống)",item?.NgayNhap);
        editor.Hint("Sẵn sàng cần vị trí và ký hiệu xếp giá. Giá nhập số đồng nguyên, ví dụ 120000. Không chuyển đầu tài liệu đã gắn; không sửa khi còn khoản mượn mở.");
        decimal? amount=null;
        editor.ValidateOnSave(()=>
        {
            if(titleId<=0)return "Chọn đầu tài liệu trước.";
            if(string.IsNullOrWhiteSpace(number.Text)||string.IsNullOrWhiteSpace(barcode.Text))return "Nhập đủ ĐKCB và barcode.";
            amount=null;
            if(!string.IsNullOrWhiteSpace(price.Text)){if(!decimal.TryParse(price.Text,NumberStyles.None,CultureInfo.InvariantCulture,out var value))return "Giá bìa chỉ gồm chữ số, không có dấu phân cách.";amount=value;}
            if(((CatalogChoice)state.SelectedItem!).Code=="SAN_SANG" && (locationId is null || string.IsNullOrWhiteSpace(callNumber.Text) || ((CatalogChoice)condition.SelectedItem!).Code=="HU_HONG"))return "Sẵn sàng cần vị trí, ký hiệu xếp giá và sách không hư hỏng.";
            if(((CatalogChoice)state.SelectedItem!).Code=="HU_HONG" && ((CatalogChoice)condition.SelectedItem!).Code!="HU_HONG")return "Chọn tình trạng hư hỏng tương ứng.";
            return null;
        });
        if(editor.ShowDialog(this)!=DialogResult.OK)return null;
        return new(item?.IdBanTaiLieu??0,titleId,number.Text,barcode.Text,locationId,callNumber.Text,((CatalogChoice)state.SelectedItem!).Code,((CatalogChoice)condition.SelectedItem!).Code,amount,date.Checked?DateOnly.FromDateTime(date.Value):null,Revision:item?.Revision??"");
    }
}
