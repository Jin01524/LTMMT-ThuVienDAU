using System.Text;
using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU.Forms;

public sealed class ReaderImportForm : Form
{
    private readonly ReaderImportClientService service;
    private readonly SessionContext sessionContext;
    private readonly DataGridView grid = new();
    private readonly Label summary = new();
    private IReadOnlyList<ReaderImportRow> rows = Array.Empty<ReaderImportRow>();
    private ReaderImportCommand? previewCommand;

    public ReaderImportForm(ReaderImportClientService service, SessionContext sessionContext)
    {
        this.service = service;
        this.sessionContext = sessionContext;
        InitializeUi();
    }

    private void InitializeUi()
    {
        Text = "DG-01 · Nhập hồ sơ độc giả";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 520);
        ClientSize = new Size(1100, 650);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8), WrapContents = false };
        var selectButton = new Button { Text = "Chọn Excel/CSV…", AutoSize = true };
        var previewButton = new Button { Text = "Kiểm tra / xem trước", AutoSize = true, Enabled = false };
        var importButton = new Button { Text = "Xác nhận nhập dòng hợp lệ", AutoSize = true, Enabled = false };
        var exportButton = new Button { Text = "Xuất dòng lỗi…", AutoSize = true, Enabled = false };
        selectButton.Click += (_, _) => SelectFile(previewButton, importButton, exportButton);
        previewButton.Click += async (_, _) => await PreviewAsync(importButton, exportButton);
        importButton.Click += async (_, _) => await CommitAsync(importButton, exportButton);
        exportButton.Click += (_, _) => ExportErrors();
        toolbar.Controls.AddRange([selectButton, previewButton, importButton, exportButton]);

        summary.Dock = DockStyle.Top;
        summary.Height = 36;
        summary.Padding = new Padding(12, 8, 0, 0);
        summary.Text = "Chọn file .xlsx hoặc .csv (tối đa 500 dòng, 10 MiB).";

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        grid.DataSource = new List<ReaderImportRowResult>();

        Controls.Add(grid);
        Controls.Add(summary);
        Controls.Add(toolbar);
    }

    private void SelectFile(Button previewButton, Button importButton, Button exportButton)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn danh sách độc giả",
            Filter = "Excel hoặc CSV (*.xlsx;*.csv)|*.xlsx;*.csv|Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            rows = service.ReadFile(dialog.FileName);
            previewCommand = new ReaderImportCommand(Guid.NewGuid().ToString("N"), rows.ToArray());
            grid.DataSource = rows.Select(row => new ReaderImportRowResult(row.RowNumber,
                string.IsNullOrEmpty(row.MaSinhVien) ? row.MaGiangVien ?? "" : row.MaSinhVien,
                "CHỜ KIỂM TRA", null)).ToList();
            summary.Text = $"Đã đọc {rows.Count} dòng. Chưa ghi dữ liệu vào CSDL.";
            previewButton.Enabled = true;
            importButton.Enabled = false;
            exportButton.Enabled = false;
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Không đọc được file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task PreviewAsync(Button importButton, Button exportButton)
    {
        var session = sessionContext.Current;
        if (session is null || previewCommand is null) return;
        try
        {
            var result = await service.PreviewAsync(session.SessionId, previewCommand);
            ShowResult(result);
            importButton.Enabled = result.ValidRows > 0;
            exportButton.Enabled = result.ErrorRows > 0;
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Không kiểm tra được file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task CommitAsync(Button importButton, Button exportButton)
    {
        var session = sessionContext.Current;
        if (session is null || previewCommand is null) return;
        if (MessageBox.Show(this, "Chỉ các dòng hợp lệ sẽ được thêm/cập nhật. Ô trống khi cập nhật được giữ nguyên. Tiếp tục?",
                "Xác nhận nhập độc giả", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        importButton.Enabled = false;
        try
        {
            var result = await service.CommitAsync(session.SessionId, previewCommand);
            ShowResult(result);
            exportButton.Enabled = result.ErrorRows > 0;
            MessageBox.Show(this,
                $"Đã nhập {result.ValidRows} dòng (thêm mới {result.CreatedRows}, cập nhật {result.UpdatedRows}); {result.ErrorRows} dòng lỗi không được ghi.",
                "Kết quả nhập", MessageBoxButtons.OK, MessageBoxIcon.Information);
            previewCommand = null;
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Không hoàn tất nhập", MessageBoxButtons.OK, MessageBoxIcon.Error);
            importButton.Enabled = true;
        }
    }

    private void ShowResult(ReaderImportResult result)
    {
        grid.DataSource = result.Results.ToList();
        summary.Text = $"Tổng {result.TotalRows} · hợp lệ {result.ValidRows} · lỗi {result.ErrorRows} · thêm {result.CreatedRows} · cập nhật {result.UpdatedRows}";
    }

    private void ExportErrors()
    {
        if (grid.DataSource is not List<ReaderImportRowResult> rowsWithErrors) return;
        var errors = rowsWithErrors.Where(row => !string.IsNullOrWhiteSpace(row.Error)).ToArray();
        if (errors.Length == 0) return;
        using var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "DG01_dong_loi.csv", AddExtension = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var builder = new StringBuilder("Dong,MaTruong,TrangThai,MoTaLoi\r\n");
            foreach (var error in errors)
                builder.Append(Csv(error.RowNumber.ToString())).Append(',')
                    .Append(Csv(error.SchoolCode)).Append(',')
                    .Append(Csv(error.Action)).Append(',')
                    .Append(Csv(error.Error ?? "")).Append("\r\n");
            File.WriteAllText(dialog.FileName, builder.ToString(), new UTF8Encoding(true));
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Không xuất được file lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string Csv(string value)
    {
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r') value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
