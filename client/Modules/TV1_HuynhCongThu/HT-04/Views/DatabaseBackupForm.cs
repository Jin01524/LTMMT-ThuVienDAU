using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU.Forms;

public sealed class DatabaseBackupForm : Form
{
    private readonly DatabaseBackupClientService service;
    private readonly SessionContext sessionContext;
    private readonly ListBox backupsList = new();
    private readonly Label summary = new();
    private readonly Button createButton = new();
    private readonly Button restoreButton = new();
    private List<BackupInfo> backups = new();
    private string database = "";

    public DatabaseBackupForm(DatabaseBackupClientService service, SessionContext sessionContext)
    {
        this.service = service;
        this.sessionContext = sessionContext;
        InitializeUi();
    }

    private void InitializeUi()
    {
        Text = "HT-04 · Sao lưu và phục hồi";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(720, 440);
        ClientSize = new Size(900, 560);

        summary.Dock = DockStyle.Top;
        summary.Height = 54;
        summary.Padding = new Padding(12);
        summary.Text = "Bản sao lưu được lưu trên máy server; dữ liệu gồm CSDL và các PDF đính kèm.";

        backupsList.Dock = DockStyle.Fill;
        backupsList.IntegralHeight = false;
        backupsList.Font = new Font("Consolas", 9);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 54, Padding = new Padding(8), WrapContents = false };
        var refreshButton = new Button { Text = "Tải danh sách", AutoSize = true };
        createButton.Text = "Tạo bản sao lưu";
        createButton.AutoSize = true;
        restoreButton.Text = "Phục hồi bản đã chọn…";
        restoreButton.AutoSize = true;
        restoreButton.Enabled = false;
        refreshButton.Click += async (_, _) => await LoadBackupsAsync();
        createButton.Click += async (_, _) => await CreateBackupAsync();
        restoreButton.Click += async (_, _) => await RestoreBackupAsync();
        backupsList.SelectedIndexChanged += (_, _) => restoreButton.Enabled = backupsList.SelectedIndex >= 0;
        actions.Controls.AddRange([refreshButton, createButton, restoreButton]);

        Controls.Add(backupsList);
        Controls.Add(summary);
        Controls.Add(actions);
        Load += async (_, _) => await LoadBackupsAsync();
    }

    private async Task LoadBackupsAsync()
    {
        var session = sessionContext.Current;
        if (session is null) return;
        try
        {
            var result = await service.ListAsync(session.SessionId);
            database = result.Database;
            backups = result.Backups.ToList();
            backupsList.Items.Clear();
            foreach (var item in backups)
                backupsList.Items.Add($"{item.BackupId} | {item.CreatedAtUtc:u} | {item.SizeBytes:N0} bytes | {item.AttachmentCount} PDF");
            summary.Text = $"CSDL: {database}. Bản sao lưu: {backups.Count}. Phục hồi sẽ tạo bản an toàn tự động và yêu cầu đăng nhập lại.";
            restoreButton.Enabled = backupsList.SelectedIndex >= 0;
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Không tải được danh sách sao lưu", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task CreateBackupAsync()
    {
        var session = sessionContext.Current;
        if (session is null) return;
        createButton.Enabled = false;
        try
        {
            var backup = await service.CreateAsync(session.SessionId);
            MessageBox.Show(this, $"Đã tạo bản sao lưu {backup.BackupId} trên server. Bao gồm {backup.AttachmentCount} PDF.",
                "Sao lưu hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadBackupsAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Không tạo được bản sao lưu", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { createButton.Enabled = true; }
    }

    private async Task RestoreBackupAsync()
    {
        var session = sessionContext.Current;
        if (session is null || backupsList.SelectedIndex < 0) return;
        var selected = backups[backupsList.SelectedIndex];
        if (MessageBox.Show(this,
                $"CẢNH BÁO: thao tác sẽ thay thế dữ liệu trong CSDL {database} và các PDF hiện tại bằng bản đã chọn. Server tạo thêm một bản an toàn trước khi phục hồi. Tiếp tục?",
                "Xác nhận phục hồi dữ liệu", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var expected = "PHUC_HOI " + database;
        var confirmation = AskForConfirmation(expected);
        if (confirmation is null) return;
        restoreButton.Enabled = false;
        try
        {
            var result = await service.RestoreAsync(session.SessionId, new RestoreBackupRequest(selected.BackupId, confirmation));
            MessageBox.Show(this,
                $"Đã phục hồi {result.RestoredBackupId}. Bản an toàn trước phục hồi: {result.SafetyBackupId}. Ứng dụng sẽ đóng; hãy đăng nhập lại.",
                "Phục hồi hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Không hoàn tất phục hồi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            restoreButton.Enabled = true;
        }
    }

    private string? AskForConfirmation(string expected)
    {
        using var dialog = new Form
        {
            Text = "Xác nhận thao tác phá hủy dữ liệu",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(460, 150)
        };
        var label = new Label { Left = 12, Top = 12, Width = 430, Height = 44, Text = $"Nhập chính xác chuỗi sau để tiếp tục:\r\n{expected}" };
        var input = new TextBox { Left = 12, Top = 62, Width = 430 };
        var accept = new Button { Text = "Xác nhận", Left = 270, Top = 102, Width = 82, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Hủy", Left = 360, Top = 102, Width = 82, DialogResult = DialogResult.Cancel };
        dialog.Controls.AddRange([label, input, accept, cancel]);
        dialog.AcceptButton = accept;
        dialog.CancelButton = cancel;
        return dialog.ShowDialog(this) == DialogResult.OK ? input.Text : null;
    }
}
