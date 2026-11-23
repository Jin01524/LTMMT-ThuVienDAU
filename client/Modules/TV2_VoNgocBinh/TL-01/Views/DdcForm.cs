namespace ThuVienDAU.Catalog;
public sealed class DdcForm : CatalogListForm<DdcItem>
{
    private readonly TreeView tree=new() { Dock=DockStyle.Left,Width=270 };
    public DdcForm(string session):base(session,"TL-01 · DDC23 bản in tiếng Việt rút gọn","TV2_DDC_LIST","TV2_DDC_SAVE") { var split=new SplitContainer { Dock=DockStyle.Fill,Size=new Size(1000,500),SplitterDistance=270 };Grid.Parent=split.Panel2;tree.Dock=DockStyle.Fill;split.Panel1.Controls.Add(tree);Body.Controls.Add(split); }
    protected override void Loaded(DdcItem[] rows)
    {
        tree.Nodes.Clear();var nodes=rows.ToDictionary(x=>x.MaDDC,x=>new TreeNode(x.MaDDC+" · "+x.TenMuc));
        foreach(var row in rows) { if(row.MaCha is not null && nodes.TryGetValue(row.MaCha,out var parent)) parent.Nodes.Add(nodes[row.MaDDC]);else tree.Nodes.Add(nodes[row.MaDDC]); }
        tree.ExpandAll();
    }
    protected override DdcItem? EditItem(DdcItem? item)
    {
        using var form=new CatalogEditor(item is null?"Thêm mục DDC":"Sửa mục DDC · "+item.MaDDC);
        var code=form.TextField("Mã DDC",item?.MaDDC,64,item is not null);var parent=form.TextField("Mã cha",item?.MaCha,64);var name=form.TextField("Tên mục",item?.TenMuc,200);var source=form.TextField("Nguồn tham khảo",item?.NguonThamKhao,600);var position=form.TextField("Tập/trang bản in",item?.ViTriTraBanIn,150);var composition=form.TextField("Căn cứ ghép số",item?.CachGhepSo,600);var confirmed=form.Check("Đã đối chiếu bản in",item?.DaDoiChieu??false);
        form.Hint("Chỉ chọn đã đối chiếu khi đã tra bộ DDC23 tiếng Việt rút gọn thực tế. Không tự duyệt từ tên ngành. Cây bên trái chỉ thể hiện các mục của trang hiện tại.");
        if(form.ShowDialog(this)!=DialogResult.OK)return null;
        return new(code.Text,parent.Text,name.Text,source.Text,confirmed.Checked,position.Text,composition.Text,Revision:item?.Revision??"");
    }
}
