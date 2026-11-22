namespace ThuVienDAU.Catalog;
public sealed class TitleForm(string session) : CatalogListForm<TitleItem>(session,"TL-02 · Đầu tài liệu","TV2_TITLE_LIST","TV2_TITLE_SAVE")
{
    protected override TitleItem? EditItem(TitleItem? item)
    {
        using var form=new CatalogEditor(item is null?"Thêm đầu tài liệu":"Sửa đầu tài liệu #"+item.IdDauTaiLieu);
        var title=form.TextField("Nhan đề",item?.NhanDe,500);var type=form.Choice("Loại tài liệu",item?.LoaiTaiLieu??"SACH","SACH","DO_AN","LUAN_VAN","KHAC");var code=form.TextField("Mã DDC",item?.MaDDC,64);var status=form.Choice("Trạng thái phân loại",item?.TrangThaiPhanLoai??"CHUA_PHAN_LOAI","CHUA_PHAN_LOAI","DU_KIEN","DA_XAC_NHAN");var position=form.TextField("Vị trí tra DDC",item?.ViTriTraDDC,150);var visible=form.Check("Hiển thị hồ sơ nội bộ",item?.DangHienThi??true);
        form.Hint("Chưa phân loại: để trống mã/vị trí tra. Xác nhận: mục DDC phải được đối chiếu trước và cần vị trí tra cho tài liệu. Tác giả/NXB/ISBN thuộc TL-03; không nhập vào nhan đề.");
        if(form.ShowDialog(this)!=DialogResult.OK)return null;
        return new(item?.IdDauTaiLieu??0,title.Text,type.SelectedItem!.ToString()!,code.Text,status.SelectedItem!.ToString()!,position.Text,visible.Checked,item?.PhienBan??0);
    }
}
