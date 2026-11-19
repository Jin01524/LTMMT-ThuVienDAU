using System.Text.Json;
using MySql.Data.MySqlClient;
using ThuVienDAU.Configuration;
using ThuVienDAU.Data;
using ThuVienDAU.Services;

namespace ThuVienDAU.Forms;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    private async void CheckConnectionMenuItem_Click(object? sender, EventArgs e)
    {
        checkConnectionMenuItem.Enabled = false;
        statusLabel.Text = "Đang kiểm tra kết nối MySQL…";
        try
        {
            var options = DatabaseOptions.Load();
            var service = new DatabaseHealthService(new MySqlConnectionFactory(options));
            var result = await service.CheckAsync();
            if (IsDisposed)
            {
                return;
            }
            MessageBox.Show(this, result, "Kết nối MySQL", MessageBoxButtons.OK, MessageBoxIcon.Information);
            statusLabel.Text = "Đã kết nối MySQL.";
        }
        catch (MySqlException exception)
        {
            MessageBox.Show(this,
                $"Không thể kết nối MySQL (mã {exception.Number}).\n" +
                "Kiểm tra máy chủ, cổng, tài khoản, CSDL và quyền truy cập.",
                "Kết nối MySQL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            statusLabel.Text = "Kết nối chưa thành công.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or JsonException or IOException or ArgumentException)
        {
            var message = exception is JsonException
                ? "File appsettings.local.json không đúng định dạng JSON."
                : exception is IOException
                    ? "Không đọc được file cấu hình MySQL. Kiểm tra đường dẫn và quyền truy cập."
                    : exception.Message;
            MessageBox.Show(this, message, "Cấu hình MySQL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            statusLabel.Text = "Cần kiểm tra cấu hình MySQL.";
        }
        finally
        {
            if (!IsDisposed)
            {
                checkConnectionMenuItem.Enabled = true;
            }
        }
    }
}
