using ThuVienDAU.Models;

namespace ThuVienDAU.Circulation.LT03;

public sealed class RenewForm : Form
{
    private readonly RenewService service;
    private readonly TextBox keyword = new() { Width = 200, PlaceholderText = "Mã trường / barcode" };
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AllowUserToAddRows = false };
    private readonly DateTimePicker newDue = new() { Format = DateTimePickerFormat.Short, Width = 115 };
    private readonly TextBox reason = new() { Width = 250, PlaceholderText = "Căn cứ gia hạn" };
    private readonly CheckBox confirmed = new() { Text = "Đã xác nhận không có bạn đọc khác đăng ký mượn", AutoSize = true };
    private readonly Button save = new() { Text = "Xác nhận gia hạn", AutoSize = true };
    public RenewForm(SessionInfo session)
    {
        service = new(session); Text = "LT-03 · Gia hạn tại quầy"; Size = new(1150, 600);
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45 }; var search = new Button { Text = "Tải khoản mượn", AutoSize = true }; top.Controls.AddRange(new Control[] { keyword, search });
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 85, AutoScroll = true };
        bottom.Controls.AddRange(new Control[] { new Label { Text = "Hạn trả mới:", AutoSize = true }, newDue, reason, confirmed, save });
        Controls.Add(grid); Controls.Add(top); Controls.Add(bottom);
        grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells;
        grid.DataBindingComplete += (_,_) => {
            var names = new Dictionary<string,string> { ["DetailId"]="Mã khoản mượn",["ReaderCode"]="Mã trường",["ReaderName"]="Bạn đọc",["ReaderType"]="Đối tượng",["Barcode"]="Mã vạch",["TitleName"]="Nhan đề",["DueDate"]="Hạn trả",["RenewCount"]="Số lần gia hạn",["CanRenew"]="Đủ điều kiện",["BlockReason"]="Điều kiện/lý do",["ViolationType"]="Loại vi phạm",["BookPrice"]="Giá bìa",["HasBookPrice"]="Có giá bìa",["ReturnedAt"]="Ngày trả/báo mất",["ChargeableDays"]="Ngày tính phạt",["SuggestedAmount"]="Số tiền dự kiến",["CalculationNote"]="Căn cứ tính",["DetailStatus"]="Trạng thái",["RawOverdueDays"]="Ngày trễ thô",["HolidayDaysExcluded"]="Ngày lễ/Tết loại trừ",["IsCalendarFinalized"]="Đã chốt lịch miễn phạt" };
            foreach(DataGridViewColumn column in grid.Columns) { if(names.TryGetValue(column.DataPropertyName,out var text))column.HeaderText=text; if(column.DataPropertyName is "LoanId" or "ReaderId" or "TitleId")column.Visible=false; }
        };
        search.Click += async (_, _) => await LoadRows();
        grid.SelectionChanged += (_, _) => { if (grid.CurrentRow?.DataBoundItem is RenewableLoanItem item) { newDue.Value = item.DueDate.Date.AddDays(1); confirmed.Checked = false; } };
        save.Click += async (_, _) =>
        {
            if (grid.CurrentRow?.DataBoundItem is not RenewableLoanItem item) return;
            save.Enabled = false;
            try { var result = await service.RenewAsync(new(item.DetailId, newDue.Value.Date, confirmed.Checked, reason.Text)); MessageBox.Show(this, $"Đã gia hạn đến {result.NewDueDate:dd/MM/yyyy}."); await LoadRows(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Gia hạn", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { save.Enabled = true; }
        };
    }
    private async Task LoadRows()
    {
        try { grid.DataSource = await service.GetActiveLoansAsync(keyword.Text); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Tải khoản mượn"); }
    }
}
