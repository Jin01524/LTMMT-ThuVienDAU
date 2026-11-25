namespace ThuVienDAU.Catalog;
public sealed class LocationForm(string session) : CatalogListForm<StorageLocationItem>(session,"TL-04 · Dãy kệ và ngăn","TV2_LOCATION_LIST","TV2_LOCATION_SAVE")
{
    protected override StorageLocationItem? EditItem(StorageLocationItem? item)
    {
        using var editor=new CatalogEditor(item is null?"Thêm vị trí kho":"Sửa vị trí kho");
        var shelf=editor.TextField("Dãy kệ",item?.DayKe,30);var compartment=editor.TextField("Ngăn",item?.Ngan,30);
        editor.Hint("Một phòng, một tầng: chỉ lưu dãy kệ/ngăn. Đổi tên vị trí giữ nguyên mã và liên kết bản sách; không tạo thêm vị trí trùng.");
        editor.ValidateOnSave(()=>string.IsNullOrWhiteSpace(shelf.Text)||string.IsNullOrWhiteSpace(compartment.Text)?"Nhập đủ dãy kệ và ngăn.":null);
        if(editor.ShowDialog(this)!=DialogResult.OK)return null;
        return new(item?.IdViTri??0,shelf.Text,compartment.Text,item?.Revision??"");
    }
}
