using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU.Forms;

public sealed class ReaderDirectoryForm : Form
{
    private readonly ReaderDirectoryService readerService;
    private readonly SessionContext sessionContext;
    private readonly TextBox searchBox = new();
    private readonly DataGridView grid = new();
    private readonly TextBox reasonBox = new();
    private readonly Button blockButton = new();
    private readonly Button unblockButton = new();

    public ReaderDirectoryForm(ReaderDirectoryService readerService, SessionContext sessionContext)
    {
        this.readerService = readerService;
        this.sessionContext = sessionContext;
        InitializeUi();
    }

    private void InitializeUi()
    {
        Text = "Tra cứu độc giả và quyền mượn";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(920, 540);
        ClientSize = new Size(1100, 650);

        var searchPanel = new Panel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(12) };
        searchBox.SetBounds(12, 14, 470, 30);
        searchBox.PlaceholderText = "Mã thẻ trường hoặc họ tên (ít nhất 2 ký tự)";
        var searchButton = new Button { Text = "Tra cứu", Left = 494, Top = 12, Width = 100, Height = 34 };
        searchButton.Click += async (_, _) => await SearchAsync();
        searchBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SearchAsync();
            }
        };
        searchPanel.Controls.AddRange([searchBox, searchButton]);

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

        var permissionPanel = new Panel { Dock = DockStyle.Bottom, Height = 68, Padding = new Padding(12) };
        reasonBox.SetBounds(12, 16, 570, 32);
        reasonBox.PlaceholderText = "Lý do khóa/mở quyền (bắt buộc)";
        blockButton.Text = "Khóa quyền mượn";
        blockButton.SetBounds(596, 14, 150, 36);
        unblockButton.Text = "Mở quyền mượn";
        unblockButton.SetBounds(756, 14, 150, 36);
        blockButton.Click += async (_, _) => await ChangePermissionAsync(false);
        unblockButton.Click += async (_, _) => await ChangePermissionAsync(true);
        var isManager = sessionContext.Current?.Role == "QUAN_LY_THU_VIEN";
        blockButton.Visible = unblockButton.Visible = isManager;
        reasonBox.Visible = isManager;
        permissionPanel.Controls.AddRange([reasonBox, blockButton, unblockButton]);

        Controls.Add(grid);
        Controls.Add(permissionPanel);
        Controls.Add(searchPanel);
    }

    private async Task SearchAsync()
    {
        var session = sessionContext.Current;
        if (session is null) return;

        try
        {
            var readers = await readerService.SearchAsync(session.SessionId, searchBox.Text);
            grid.DataSource = readers.ToList();
            if (readers.Count == 0)
            {
                MessageBox.Show(this, "Không tìm thấy độc giả phù hợp.", "Tra cứu độc giả",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Tra cứu độc giả",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task ChangePermissionAsync(bool canBorrow)
    {
        var session = sessionContext.Current;
        if (session is null || grid.CurrentRow?.DataBoundItem is not ReaderProfile reader)
        {
            MessageBox.Show(this, "Chọn hồ sơ độc giả trước.", "Quyền mượn",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(reasonBox.Text))
        {
            MessageBox.Show(this, "Phải nhập lý do thao tác.", "Quyền mượn",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var action = canBorrow ? "mở" : "khóa";
        if (MessageBox.Show(this, $"Xác nhận {action} quyền mượn của {reader.Name} ({reader.SchoolId})?",
                "Xác nhận thao tác", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await readerService.SetBorrowPermissionAsync(reader.ReaderId,
                new BorrowPermissionRequest(session.SessionId, canBorrow, reasonBox.Text.Trim()));
            reasonBox.Clear();
            await SearchAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Không cập nhật được quyền mượn",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
