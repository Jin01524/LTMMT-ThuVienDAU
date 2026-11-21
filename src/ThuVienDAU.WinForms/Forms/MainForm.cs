using ThuVienDAU.Models;

namespace ThuVienDAU.Forms;

public partial class MainForm : Form, IMainView
{
    public MainForm()
    {
        InitializeComponent();
    }

    public event EventHandler? ConnectionCheckRequested;

    private void CheckConnectionMenuItem_Click(object? sender, EventArgs e)
    {
        ConnectionCheckRequested?.Invoke(this, EventArgs.Empty);
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

    void IMainView.ShowConnectionCheckFailed(int errorNumber)
    {
        ShowConnectionResult(
            $"Không thể kết nối MySQL (mã {errorNumber}).\n" +
            "Kiểm tra máy chủ, cổng, tài khoản, CSDL và quyền truy cập.",
            "Kết nối MySQL", MessageBoxIcon.Warning, "Kết nối chưa thành công.");
    }

    void IMainView.ShowConnectionConfigurationError(string message)
    {
        ShowConnectionResult(
            message, "Cấu hình MySQL", MessageBoxIcon.Warning, "Cần kiểm tra cấu hình MySQL.");
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
