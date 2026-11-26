using ThuVienDAU.Models;

namespace ThuVienDAU.Circulation.VP01;

public sealed class ViolationCalcForm : Form
{
    private readonly ViolationService service;
    private readonly TextBox keyword = new() { Width = 200, PlaceholderText = "Mã trường / barcode" };
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AllowUserToAddRows = false };
    private readonly ComboBox method = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly NumericUpDown price = new() { Maximum = 499999999999m, Width = 150, ThousandsSeparator = true };
    private readonly NumericUpDown amount = new() { Maximum = 999999999999m, Width = 150, ThousandsSeparator = true };
    private readonly TextBox basis = new() { Width = 350, PlaceholderText = "Căn cứ/chi tiết quyết định xử lý" };
    private readonly Button save = new() { Text = "Lập vi phạm / thông báo", AutoSize = true };
    public ViolationCalcForm(SessionInfo session)
    {
        service = new(session); Text = "VP-01 · Quá hạn và bồi thường"; Size = new(1250, 650);
        var search = new Button { Text = "Tải căn cứ vi phạm", AutoSize = true }; var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45 }; top.Controls.AddRange(new Control[] { keyword, search });
        method.Items.AddRange(new object[] { "DEN_TIEN", "DEN_SACH" }); method.SelectedIndex = 0;
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 120, AutoScroll = true };
        bottom.Controls.AddRange(new Control[] { new Label { Text = "Mất sách:", AutoSize = true }, method, new Label { Text = "Giá tương đương (QL):", AutoSize = true }, price, new Label { Text = "Chi phí hỏng trang (QL):", AutoSize = true }, amount, basis, save });
        Controls.Add(grid); Controls.Add(top); Controls.Add(bottom);
        grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells;
        grid.DataBindingComplete += (_,_) => {
            var names = new Dictionary<string,string> { ["DetailId"]="Mã khoản mượn",["ReaderCode"]="Mã trường",["ReaderName"]="Bạn đọc",["ReaderType"]="Đối tượng",["Barcode"]="Mã vạch",["TitleName"]="Nhan đề",["DueDate"]="Hạn trả",["RenewCount"]="Số lần gia hạn",["CanRenew"]="Đủ điều kiện",["BlockReason"]="Điều kiện/lý do",["ViolationType"]="Loại vi phạm",["BookPrice"]="Giá bìa",["HasBookPrice"]="Có giá bìa",["ReturnedAt"]="Ngày trả/báo mất",["ChargeableDays"]="Ngày tính phạt",["SuggestedAmount"]="Số tiền dự kiến",["CalculationNote"]="Căn cứ tính",["DetailStatus"]="Trạng thái",["RawOverdueDays"]="Ngày trễ thô",["HolidayDaysExcluded"]="Ngày lễ/Tết loại trừ",["IsCalendarFinalized"]="Đã chốt lịch miễn phạt" };
            foreach(DataGridViewColumn column in grid.Columns) { if(names.TryGetValue(column.DataPropertyName,out var text))column.HeaderText=text; if(column.DataPropertyName is "LoanId" or "ReaderId" or "TitleId")column.Visible=false; }
        };
        price.Enabled = amount.Enabled = session.Role == "QUAN_LY_THU_VIEN";
        grid.SelectionChanged += (_, _) => { if (grid.CurrentRow?.DataBoundItem is ViolationCandidate c) { basis.Text = c.CalculationNote; method.SelectedIndex = 0; price.Value = 0; amount.Value = 0; } };
        search.Click += async (_, _) => await LoadRows(); Shown += async (_, _) => await LoadRows();
        save.Click += async (_, _) =>
        {
            if (grid.CurrentRow?.DataBoundItem is not ViolationCandidate item) return;
            if (item.ViolationType == "MAT_SACH" && method.SelectedItem?.ToString() == "DEN_SACH" && MessageBox.Show(this, "Xác nhận thư viện đã nhận sách thay thế và thu thêm 20.000đ phí kỹ thuật?", "Đền sách", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            save.Enabled = false;
            try
            {
                var result = await service.CreateViolationAsync(new() { DetailId = item.DetailId, ViolationType = item.ViolationType, Description = basis.Text, SettlementMethod = method.SelectedItem!.ToString(), AssessedEquivalentPrice = price.Value > 0 ? price.Value : null, Amount = amount.Value });
                MessageBox.Show(this, $"Đã lập #{result.ViolationId}: {result.Amount:N0}đ. Quyền mượn giữ nguyên; khoản nợ chưa trả vẫn chặn mượn tiếp.");
                if (result.ViolationType == "THONG_BAO_DON_VI") await service.PreviewAsync(this, "NOTICE", result.ViolationId);
                await LoadRows();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Lập vi phạm", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { save.Enabled = true; }
        };
    }
    private async Task LoadRows()
    {
        try { grid.DataSource = await service.GetPendingCandidatesAsync(keyword.Text); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Căn cứ vi phạm"); }
    }
}
