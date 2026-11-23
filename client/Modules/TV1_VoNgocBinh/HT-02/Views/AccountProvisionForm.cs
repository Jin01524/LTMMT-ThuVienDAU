using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU.Forms;

public sealed class AccountProvisionForm : Form
{
    private readonly AuthenticationService authenticationService;
    private readonly SessionContext sessionContext;
    private readonly DataGridView grid = new();
    private readonly TextBox usernameBox = new();
    private readonly TextBox passwordBox = new();
    private readonly TextBox confirmPasswordBox = new();
    private readonly ComboBox roleBox = new();
    private readonly NumericUpDown readerIdBox = new();

    public AccountProvisionForm(AuthenticationService authenticationService, SessionContext sessionContext)
    {
        this.authenticationService = authenticationService;
        this.sessionContext = sessionContext;
        InitializeUi();
    }

    private void InitializeUi()
    {
        Text = "Cấp tài khoản hệ thống";
        ClientSize = new Size(980, 620);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(244, 247, 250);

        var title = new Label
        {
            Dock = DockStyle.Top, Height = 58,
            Text = "QUẢN LÝ TÀI KHOẢN",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 55, 83)
        };

        var panel = new Panel { Dock = DockStyle.Right, Width = 330, Padding = new Padding(24), BackColor = Color.White };
        var usernameLabel = new Label { Text = "Tên đăng nhập", Left = 24, Top = 28, Width = 280 };
        usernameBox.Left = 24; usernameBox.Top = 50; usernameBox.Width = 280;
        var passwordLabel = new Label { Text = "Mật khẩu (ít nhất 8 ký tự)", Left = 24, Top = 88, Width = 280 };
        passwordBox.Left = 24; passwordBox.Top = 110; passwordBox.Width = 280; passwordBox.UseSystemPasswordChar = true;
        var confirmLabel = new Label { Text = "Xác nhận mật khẩu", Left = 24, Top = 148, Width = 280 };
        confirmPasswordBox.Left = 24; confirmPasswordBox.Top = 170; confirmPasswordBox.Width = 280; confirmPasswordBox.UseSystemPasswordChar = true;
        var roleLabel = new Label { Text = "Vai trò", Left = 24, Top = 208, Width = 280 };
        roleBox.Left = 24; roleBox.Top = 230; roleBox.Width = 280; roleBox.DropDownStyle = ComboBoxStyle.DropDownList;
        roleBox.Items.AddRange(["QUAN_TRI", "QUAN_LY_THU_VIEN", "BIEN_MUC", "THU_THU", "DOC_GIA"]);
        roleBox.SelectedIndex = 0;
        var readerLabel = new Label { Text = "Id hồ sơ độc giả (bắt buộc với DOC_GIA)", Left = 24, Top = 268, Width = 280 };
        readerIdBox.Left = 24; readerIdBox.Top = 290; readerIdBox.Width = 280;
        readerIdBox.Minimum = 0; readerIdBox.Maximum = int.MaxValue;

        var createButton = new Button
        {
            Text = "Cấp tài khoản", Left = 24, Top = 340, Width = 280, Height = 42,
            BackColor = Color.FromArgb(34, 103, 154), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        createButton.FlatAppearance.BorderSize = 0;
        createButton.Click += async (_, _) => await ProvisionAsync();

        panel.Controls.AddRange([usernameLabel, usernameBox, passwordLabel, passwordBox, confirmLabel,
            confirmPasswordBox, roleLabel, roleBox, readerLabel, readerIdBox, createButton]);

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        Controls.Add(grid);
        Controls.Add(panel);
        Controls.Add(title);
        Load += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (sessionContext.Current is null) return;
        try
        {
            grid.DataSource = (await authenticationService.GetAccountsAsync(sessionContext.Current.SessionId)).ToList();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task ProvisionAsync()
    {
        if (sessionContext.Current is null) return;
        if (passwordBox.Text != confirmPasswordBox.Text)
        {
            MessageBox.Show(this, "Mật khẩu xác nhận không khớp.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var role = roleBox.SelectedItem?.ToString();
        if (role is null) return;
        var readerId = role == "DOC_GIA" && readerIdBox.Value > 0 ? (int?)readerIdBox.Value : null;

        try
        {
            await authenticationService.ProvisionAccountAsync(new ProvisionAccountRequest(
                sessionContext.Current.SessionId, usernameBox.Text, passwordBox.Text, role, readerId));
            usernameBox.Clear(); passwordBox.Clear(); confirmPasswordBox.Clear(); readerIdBox.Value = 0;
            await LoadAsync();
            MessageBox.Show(this, "Cấp tài khoản thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Không cấp được tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
