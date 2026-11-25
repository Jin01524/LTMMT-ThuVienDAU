using System.Text;
using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU.Forms;

public sealed class ReaderReportForm : Form
{
    private readonly ReaderReportClientService service;
    private readonly SessionContext sessionContext;
    private readonly ComboBox typeBox = new();
    private readonly TextBox facultyBox = new();
    private readonly TextBox majorBox = new();
    private readonly DateTimePicker fromPicker = new();
    private readonly DateTimePicker toPicker = new();
    private readonly Label summaryLabel = new();
    private readonly DataGridView groupsGrid = CreateGrid();
    private readonly DataGridView readersGrid = CreateGrid();
    private readonly DataGridView activityGrid = CreateGrid();
    private ReaderReportResult? currentReport;

    public ReaderReportForm(ReaderReportClientService service, SessionContext sessionContext)
    {
        this.service = service;
        this.sessionContext = sessionContext;
        InitializeUi();
    }

    private void InitializeUi()
    {
        Text = "BC-01 Thống kê độc giả và nhật ký";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(920, 600);
        ClientSize = new Size(1180, 760);

        var filters = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 110, Padding = new Padding(12), WrapContents = true };
        typeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        typeBox.Items.AddRange(["Tất cả loại", "SINH_VIEN", "CAN_BO", "GIANG_VIEN", "TRO_GIANG", "CNV"]);
        typeBox.SelectedIndex = 0;
        facultyBox.Width = 180;
        facultyBox.PlaceholderText = "Tên khoa chính xác";
        majorBox.Width = 180;
        majorBox.PlaceholderText = "Tên ngành chính xác";
        fromPicker.Format = DateTimePickerFormat.Short;
        toPicker.Format = DateTimePickerFormat.Short;
        fromPicker.Value = DateTime.Today.AddDays(-30);
        toPicker.Value = DateTime.Today;
        var loadButton = new Button { Text = "Tạo báo cáo", Width = 110, Height = 30 };
        loadButton.Click += async (_, _) => await LoadReportAsync();
        var exportButton = new Button { Text = "Xuất CSV", Width = 95, Height = 30 };
        exportButton.Click += (_, _) => ExportCsv();
        filters.Controls.AddRange([new Label { Text = "Đối tượng", AutoSize = true, Padding = new Padding(0, 8, 0, 0) },
            typeBox, facultyBox, majorBox,
            new Label { Text = "Nhật ký từ", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, fromPicker,
            new Label { Text = "đến", AutoSize = true, Padding = new Padding(0, 8, 0, 0) }, toPicker,
            loadButton, exportButton]);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var summaryTab = new TabPage("Thống kê");
        summaryLabel.Dock = DockStyle.Top;
        summaryLabel.Height = 54;
        summaryLabel.Padding = new Padding(12);
        groupsGrid.Dock = DockStyle.Fill;
        summaryTab.Controls.Add(groupsGrid);
        summaryTab.Controls.Add(summaryLabel);
        var readersTab = new TabPage("Danh sách đã lọc");
        readersGrid.Dock = DockStyle.Fill;
        readersTab.Controls.Add(readersGrid);
        var activityTab = new TabPage("Nhật ký độc giả");
        activityGrid.Dock = DockStyle.Fill;
        activityTab.Controls.Add(activityGrid);
        tabs.TabPages.AddRange([summaryTab, readersTab, activityTab]);
        Controls.Add(tabs);
        Controls.Add(filters);
    }

    private async Task LoadReportAsync()
    {
        if (sessionContext.Current is null) return;
        try
        {
            var from = fromPicker.Value.Date;
            var to = toPicker.Value.Date.AddDays(1);
            currentReport = await service.GetAsync(sessionContext.Current.SessionId,
                typeBox.SelectedIndex == 0 ? null : typeBox.SelectedItem?.ToString(),
                facultyBox.Text, majorBox.Text, from, to);
            summaryLabel.Text = $"Tổng {currentReport.TotalReaders}; hồ sơ hoạt động {currentReport.ActiveProfiles}; " +
                $"đang có quyền mượn {currentReport.BorrowEnabled}. Kỳ ngày lọc áp dụng cho nhật ký hồ sơ.";
            var groups = currentReport.ByType.Concat(currentReport.ByFaculty).Concat(currentReport.ByMajor).ToList();
            groupsGrid.DataSource = groups;
            readersGrid.DataSource = currentReport.Readers.ToList();
            activityGrid.DataSource = currentReport.Activity.ToList();
            if (currentReport.ReaderListTruncated)
            {
                MessageBox.Show(this, "Danh sách vượt quá 5.000 hồ sơ; hãy thu hẹp bộ lọc trước khi xuất.",
                    "Báo cáo bị giới hạn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "BC-01", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ExportCsv()
    {
        if (currentReport is null)
        {
            MessageBox.Show(this, "Hãy tạo báo cáo trước khi xuất.", "Xuất báo cáo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (currentReport.ReaderListTruncated)
        {
            MessageBox.Show(this, "Danh sách hiện bị giới hạn 5.000 dòng; hãy thu hẹp bộ lọc rồi xuất lại.",
                "Không thể xuất đầy đủ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new SaveFileDialog { Filter = "CSV UTF-8 (*.csv)|*.csv", FileName = "BaoCaoDocGia.csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var builder = new StringBuilder();
        builder.AppendLine("MaTheTruong,HoTen,LoaiDocGia,Khoa,Nganh,TrangThaiHoSo,DuocMuon,SoBanDangMuon,ViPhamChuaXuLy");
        foreach (var reader in currentReport.Readers)
        {
            builder.AppendLine(string.Join(',', new[]
            {
                Csv(reader.SchoolId), Csv(reader.Name), Csv(reader.ReaderType), Csv(reader.Faculty), Csv(reader.Major),
                Csv(reader.ProfileStatus), Csv(reader.CanBorrow ? "Co" : "Khong"),
                Csv(reader.ActiveLoans.ToString()), Csv(reader.UnresolvedFines.ToString())
            }));
        }
        File.WriteAllText(dialog.FileName, builder.ToString(), new UTF8Encoding(true));
        MessageBox.Show(this, $"Đã xuất {currentReport.Readers.Count} hồ sơ theo đúng bộ lọc.", "Xuất báo cáo",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string Csv(string? value)
    {
        value ??= "";
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@') value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static DataGridView CreateGrid() => new()
    {
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells
    };
}
