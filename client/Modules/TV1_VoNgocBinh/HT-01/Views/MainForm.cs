using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU.Forms;

public partial class MainForm : Form, IMainView
{
    private readonly AuthenticationService? authenticationService;
    private readonly ReaderDirectoryService? readerDirectoryService;
    private readonly SystemAdministrationClientService? systemAdministrationService;
    private readonly ReaderReportClientService? readerReportService;
    private readonly SessionContext? sessionContext;

    public MainForm(AuthenticationService? authenticationService = null, SessionContext? sessionContext = null,
        ReaderDirectoryService? readerDirectoryService = null,
        SystemAdministrationClientService? systemAdministrationService = null,
        ReaderReportClientService? readerReportService = null)
    {
        this.authenticationService = authenticationService;
        this.sessionContext = sessionContext;
        this.readerDirectoryService = readerDirectoryService;
        this.systemAdministrationService = systemAdministrationService;
        this.readerReportService = readerReportService;
        InitializeComponent();
    }

    public event EventHandler? ConnectionCheckRequested;

    private void CheckConnectionMenuItem_Click(object? sender, EventArgs e)
    {
        ConnectionCheckRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ReaderDirectoryMenuItem_Click(object? sender, EventArgs e)
    {
        if (readerDirectoryService is null || sessionContext?.Current is null)
        {
            MessageBox.Show(this, "Chưa đăng nhập.", "Tra cứu độc giả", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var form = new ReaderDirectoryForm(readerDirectoryService, sessionContext);
        form.ShowDialog(this);
    }

    private void SystemAdministrationMenuItem_Click(object? sender, EventArgs e)
    {
        if (systemAdministrationService is null || sessionContext?.Current is null)
        {
            MessageBox.Show(this, "Chưa đăng nhập.", "Cấu hình hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var form = new SystemAdministrationForm(systemAdministrationService, sessionContext);
        form.ShowDialog(this);
    }

    private void ReaderReportMenuItem_Click(object? sender, EventArgs e)
    {
        if (readerReportService is null || sessionContext?.Current is null)
        {
            MessageBox.Show(this, "Chưa đăng nhập.", "Báo cáo độc giả", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var form = new ReaderReportForm(readerReportService, sessionContext);
        form.ShowDialog(this);
    }

    private void AccountProvisionMenuItem_Click(object? sender, EventArgs e)
    {
        if (authenticationService is null || sessionContext?.Current is null)
        {
            MessageBox.Show(this, "Chưa đăng nhập.", "Tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (sessionContext.Current.Role != "QUAN_TRI")
        {
            MessageBox.Show(this, "Chức năng này chỉ dành cho vai trò QUAN_TRI.", "Từ chối truy cập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        using var form = new AccountProvisionForm(authenticationService, sessionContext);
        form.ShowDialog(this);
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
            readerDirectoryMenuItem.Visible = session.Role is "QUAN_TRI" or "QUAN_LY_THU_VIEN" or "BIEN_MUC" or "THU_THU";
            systemAdministrationMenuItem.Visible = session.Role is "QUAN_TRI" or "QUAN_LY_THU_VIEN";
            readerReportMenuItem.Visible = session.Role is "QUAN_TRI" or "QUAN_LY_THU_VIEN";
            accountProvisionMenuItem.Visible = session.Role == "QUAN_TRI";
            readerAccountsMenuItem.Visible = session.Role == "BIEN_MUC";
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
