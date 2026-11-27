using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU.Forms;

public partial class MainForm : Form, IMainView
{
    private readonly AuthenticationService? authenticationService;
    private readonly ReaderDirectoryService? readerDirectoryService;
    private readonly SystemAdministrationClientService? systemAdministrationService;
    private readonly ReaderReportClientService? readerReportService;
    private readonly ReaderImportClientService? readerImportService;
    private readonly DatabaseBackupClientService? databaseBackupService;
    private readonly SessionContext? sessionContext;

    public MainForm(AuthenticationService? authenticationService = null, SessionContext? sessionContext = null,
        ReaderDirectoryService? readerDirectoryService = null,
        SystemAdministrationClientService? systemAdministrationService = null,
        ReaderReportClientService? readerReportService = null,
        ReaderImportClientService? readerImportService = null,
        DatabaseBackupClientService? databaseBackupService = null)
    {
        this.authenticationService = authenticationService;
        this.sessionContext = sessionContext;
        this.readerDirectoryService = readerDirectoryService;
        this.systemAdministrationService = systemAdministrationService;
        this.readerReportService = readerReportService;
        this.readerImportService = readerImportService;
        this.databaseBackupService = databaseBackupService;
        InitializeComponent();
        var readerImportMenu = new ToolStripMenuItem("DG-01 · Nhập hồ sơ độc giả");
        readerImportMenu.Click += ReaderImportMenuItem_Click;
        toolsMenuItem.DropDownItems.Insert(1, readerImportMenu);
        var backupMenu = new ToolStripMenuItem("HT-04 · Sao lưu / phục hồi");
        backupMenu.Click += DatabaseBackupMenuItem_Click;
        toolsMenuItem.DropDownItems.Insert(2, backupMenu);
        var catalogMenu = new ToolStripMenuItem("TV2 · Biên mục");
        catalogMenu.Click += (_, _) => { using var workspace = new ThuVienDAU.Catalog.CatalogWorkspaceForm(sessionContext?.Current); workspace.ShowDialog(this); };
        catalogMenu.Enabled=sessionContext?.Current?.Role is "BIEN_MUC" or "QUAN_LY_THU_VIEN";
        menuStrip.Items.Insert(0,catalogMenu);
        var circulationMenu = new ToolStripMenuItem("TV3 · Lưu thông");
        circulationMenu.Enabled = sessionContext?.Current?.Role is "THU_THU" or "QUAN_LY_THU_VIEN" or "DOC_GIA";
        circulationMenu.Click += (_, _) => { if(sessionContext?.Current is null)return; using var workspace = new ThuVienDAU.Circulation.CirculationWorkspaceForm(sessionContext.Current); workspace.ShowDialog(this); };
        menuStrip.Items.Insert(1,circulationMenu);
        var logoutMenu=new ToolStripMenuItem("Đăng xuất");
        logoutMenu.Click+=async(_,_)=> {logoutMenu.Enabled=false;try {if(authenticationService is not null && sessionContext?.Current is not null)await authenticationService.LogoutAsync(sessionContext.Current.SessionId);}catch(Exception ex){MessageBox.Show(this,"Phiên đã được đóng trên client nhưng có thể chưa ghi đủ nhật ký đăng xuất: "+ex.Message,"Đăng xuất",MessageBoxButtons.OK,MessageBoxIcon.Warning);}finally {sessionContext?.Clear();Close();}};
        menuStrip.Items.Add(logoutMenu);
    }

    public event EventHandler? ConnectionCheckRequested;

    private void CheckConnectionMenuItem_Click(object? sender, EventArgs e)
    {
        ConnectionCheckRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ReaderImportMenuItem_Click(object? sender, EventArgs e)
    {
        if (readerImportService is null || sessionContext?.Current is null)
        {
            MessageBox.Show(this, "Chưa đăng nhập.", "Nhập hồ sơ độc giả", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (sessionContext.Current.Role != "QUAN_LY_THU_VIEN")
        {
            MessageBox.Show(this, "Chỉ Quản lý thư viện được chạy nhập hồ sơ.", "Từ chối truy cập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        using var form = new ReaderImportForm(readerImportService, sessionContext);
        form.ShowDialog(this);
    }

    private void DatabaseBackupMenuItem_Click(object? sender, EventArgs e)
    {
        if (databaseBackupService is null || sessionContext?.Current is null) return;
        if (sessionContext.Current.Role is not ("QUAN_TRI" or "QUAN_LY_THU_VIEN"))
        {
            MessageBox.Show(this, "Chỉ Quản trị hoặc Quản lý thư viện được sao lưu/phục hồi.", "Từ chối truy cập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        using var form = new DatabaseBackupForm(databaseBackupService, sessionContext);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            sessionContext.Clear();
            Close();
        }
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
            toolsMenuItem.DropDownItems[1].Visible = session.Role == "QUAN_LY_THU_VIEN";
            toolsMenuItem.DropDownItems[2].Visible = session.Role is "QUAN_TRI" or "QUAN_LY_THU_VIEN";
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
