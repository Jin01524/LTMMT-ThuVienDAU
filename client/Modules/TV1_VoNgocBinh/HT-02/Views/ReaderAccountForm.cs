using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU.Forms;

public sealed class ReaderAccountForm : Form
{
    private readonly AuthenticationService authenticationService;
    private readonly SessionContext sessionContext;
    private readonly DataGridView grid = new();

    public ReaderAccountForm(AuthenticationService authenticationService, SessionContext sessionContext)
    {
        this.authenticationService = authenticationService;
        this.sessionContext = sessionContext;
        InitializeUi();
    }

    private void InitializeUi()
    {
        Text = "Quản lý tài khoản độc giả";
        ClientSize = new Size(900, 520);
        StartPosition = FormStartPosition.CenterParent;

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(245, 247, 250) };
        var lockButton = new Button { Text = "Khóa tài khoản", Left = 16, Top = 13, Width = 150, Height = 38 };
        var unlockButton = new Button { Text = "Mở tài khoản", Left = 174, Top = 13, Width = 150, Height = 38 };
        var reasonBox = new TextBox { Left = 340, Top = 16, Width = 520, Height = 32, PlaceholderText = "Nhập lý do bắt buộc" };
        lockButton.Click += async (_, _) => await ChangeStatusAsync(reasonBox.Text, false);
        unlockButton.Click += async (_, _) => await ChangeStatusAsync(reasonBox.Text, true);
        toolbar.Controls.AddRange([lockButton, unlockButton, reasonBox]);

        Controls.Add(grid);
        Controls.Add(toolbar);
        Load += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (sessionContext.Current is null) return;
        try
        {
            var accounts = await authenticationService.GetReaderAccountsAsync(sessionContext.Current.SessionId);
            grid.DataSource = accounts.ToList();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Tài khoản độc giả", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task ChangeStatusAsync(string reason, bool isActive)
    {
        if (sessionContext.Current is null || grid.CurrentRow?.DataBoundItem is not ReaderAccountItem account)
        {
            MessageBox.Show(this, "Chọn tài khoản độc giả cần thay đổi.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            MessageBox.Show(this, "Bắt buộc nhập lý do.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            await authenticationService.SetReaderAccountStatusAsync(
                new ReaderAccountStatusRequest(sessionContext.Current.SessionId, account.AccountId, isActive, reason));
            await LoadAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Không thay đổi được trạng thái", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
