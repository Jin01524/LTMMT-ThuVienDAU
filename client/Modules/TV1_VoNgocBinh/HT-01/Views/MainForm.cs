using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU.Forms;

public partial class MainForm : Form, IMainView
{
    private readonly AuthenticationService? authenticationService;
    private readonly SessionContext? sessionContext;

    public MainForm(AuthenticationService? authenticationService = null, SessionContext? sessionContext = null)
    {
        this.authenticationService = authenticationService;
        this.sessionContext = sessionContext;
        InitializeComponent();
    }

    public event EventHandler? ConnectionCheckRequested;

    private void CheckConnectionMenuItem_Click(object? sender, EventArgs e)
    {
        ConnectionCheckRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ReaderAccountsMenuItem_Click(object? sender, EventArgs e)
    {
        if (authenticationService is null || sessionContext?.Current is null)
        {
            MessageBox.Show(this, "Chưa đăng nhập.", "Tài khoản độc giả", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (sessionContext.Current.Role != "BIEN_MUC")
        {
            MessageBox.Show(this, "Chức năng này chỉ dành cho vai trò BIEN_MUC.", "Từ chối truy cập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        using var form = new ReaderAccountForm(authenticationService, sessionContext);
        form.ShowDialog(this);
    }

    public void ShowLogin(SessionInfo session)
    {
        if (!IsDisposed)
        {
            statusLabel.Text = $"Đã đăng nhập: {session.DisplayName} ({session.Role})";
        }
    }

    void IMainView.ShowConnectionCheckStarted()
    {
        checkConnectionMenuItem.Enabled = false;
        statusLabel.Text = "Đang kiểm tra kết nối MySQL…";
    }

    void IMainView.ShowConnectionCheckSucceeded(DatabaseConnectionInfo connectionInfo)
    {
        ShowConnectionResult(
            $"Kết nối thành công.\nMySQL: {connectionInfo.ServerVersion}\nCSDL: {connectionInfo.DatabaseName}",
            "Kết nối MySQL", MessageBoxIcon.Information, "Đã kết nối MySQL.");
    }

    void IMainView.ShowConnectionCheckFailed(string message)
    {
        ShowConnectionResult(
            message,
            "Kết nối MySQL", MessageBoxIcon.Warning, "Kết nối chưa thành công.");
    }

    void IMainView.ShowConnectionConfigurationError(string message)
    {
        ShowConnectionResult(
            message, "Cấu hình hệ thống", MessageBoxIcon.Warning, "Cần kiểm tra cấu hình hệ thống.");
    }

    void IMainView.EnableConnectionCheck()
    {
        if (!IsDisposed)
        {
            checkConnectionMenuItem.Enabled = true;
        }
    }

    private void ShowConnectionResult(string message, string title, MessageBoxIcon icon, string status)
    {
        if (IsDisposed)
        {
            return;
        }

        MessageBox.Show(this, message, title, MessageBoxButtons.OK, icon);
        statusLabel.Text = status;
    }
}
