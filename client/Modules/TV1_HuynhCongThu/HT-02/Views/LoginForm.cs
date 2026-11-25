using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU.Forms;

public sealed class LoginForm : Form
{
    private readonly AuthenticationService authenticationService;
    private readonly SessionContext sessionContext;

    public LoginForm(AuthenticationService authenticationService, SessionContext sessionContext)
    {
        this.authenticationService = authenticationService;
        this.sessionContext = sessionContext;
        InitializeUi();
    }

    private TextBox usernameBox = null!;
    private TextBox passwordBox = null!;
    private Label statusLabel = null!;
    private Button loginButton = null!;

    private void InitializeUi()
    {
        Text = "Đăng nhập - Thư viện DAU";
        ClientSize = new Size(520, 340);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = Color.FromArgb(246, 248, 251);

        var card = new Panel
        {
            Left = 60, Top = 38, Width = 400, Height = 260,
            BackColor = Color.White, BorderStyle = BorderStyle.None
        };

        var title = new Label
        {
            Text = "ĐĂNG NHẬP HỆ THỐNG",
            Left = 30, Top = 24, Width = 340, Height = 36,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 55, 83)
        };
        var usernameLabel = new Label { Text = "Tên đăng nhập", Left = 30, Top = 78, Width = 340 };
        usernameBox = new TextBox { Left = 30, Top = 98, Width = 340, Height = 30 };
        var passwordLabel = new Label { Text = "Mật khẩu", Left = 30, Top = 132, Width = 340 };
        passwordBox = new TextBox { Left = 30, Top = 152, Width = 340, Height = 30, UseSystemPasswordChar = true };
        statusLabel = new Label
        {
            Left = 30, Top = 188, Width = 340, Height = 24,
            ForeColor = Color.FromArgb(180, 50, 50), TextAlign = ContentAlignment.MiddleCenter
        };
        loginButton = new Button
        {
            Text = "Đăng nhập", Left = 30, Top = 218, Width = 340, Height = 38,
            BackColor = Color.FromArgb(34, 103, 154), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        loginButton.FlatAppearance.BorderSize = 0;
        loginButton.Click += async (_, _) => await LoginAsync();
        passwordBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await LoginAsync(); }
        };

        card.Controls.AddRange([title, usernameLabel, usernameBox, passwordLabel, passwordBox, statusLabel, loginButton]);
        Controls.Add(card);
        AcceptButton = loginButton;
    }

    private async Task LoginAsync()
    {
        loginButton.Enabled=false;
        statusLabel.Text = "Đang xác thực…";
        try
        {
            var session = await authenticationService.LoginAsync(usernameBox.Text, passwordBox.Text);
            sessionContext.Set(session);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (System.Net.Sockets.SocketException)
        {
            statusLabel.Text = "Không kết nối được server. Kiểm tra server TCP và cấu hình Host/Port.";
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or InvalidOperationException or IOException or System.Text.Json.JsonException or OperationCanceledException)
        {
            statusLabel.Text = exception is OperationCanceledException ? "Hết thời gian chờ server." : exception.Message;
        }
        finally {if(!IsDisposed)loginButton.Enabled=true;}
    }
}
