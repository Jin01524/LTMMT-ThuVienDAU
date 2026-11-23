#nullable enable

namespace ThuVienDAU.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;
    private MenuStrip menuStrip = null!;
    private ToolStripMenuItem toolsMenuItem = null!;
    private ToolStripMenuItem checkConnectionMenuItem = null!;
    private ToolStripMenuItem readerAccountsMenuItem = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private TableLayoutPanel contentPanel = null!;
    private Label titleLabel = null!;
    private Label schoolLabel = null!;
    private Label welcomeLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        menuStrip = new MenuStrip();
        toolsMenuItem = new ToolStripMenuItem();
        checkConnectionMenuItem = new ToolStripMenuItem();
        readerAccountsMenuItem = new ToolStripMenuItem();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        contentPanel = new TableLayoutPanel();
        titleLabel = new Label();
        schoolLabel = new Label();
        welcomeLabel = new Label();
        menuStrip.SuspendLayout();
        statusStrip.SuspendLayout();
        contentPanel.SuspendLayout();
        SuspendLayout();

        menuStrip.Name = "menuStrip";
        menuStrip.Items.AddRange(new ToolStripItem[] { toolsMenuItem });
        toolsMenuItem.Name = "toolsMenuItem";
        toolsMenuItem.Text = "Công cụ";
        toolsMenuItem.DropDownItems.AddRange(new ToolStripItem[] { checkConnectionMenuItem, readerAccountsMenuItem });
        readerAccountsMenuItem.Name = "readerAccountsMenuItem";
        readerAccountsMenuItem.Text = "Tài khoản độc giả";
        readerAccountsMenuItem.Click += ReaderAccountsMenuItem_Click;
        checkConnectionMenuItem.Name = "checkConnectionMenuItem";
        checkConnectionMenuItem.Text = "Kiểm tra kết nối MySQL";
        checkConnectionMenuItem.Click += CheckConnectionMenuItem_Click;
        statusStrip.Name = "statusStrip";
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
        statusLabel.Name = "statusLabel";
        statusLabel.Text = "Sẵn sàng. Chưa kiểm tra kết nối MySQL.";

        contentPanel.Name = "contentPanel";
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Padding = new Padding(36);
        contentPanel.ColumnCount = 1;
        contentPanel.RowCount = 4;
        contentPanel.BackColor = Color.White;
        contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        contentPanel.Controls.Add(titleLabel, 0, 0);
        contentPanel.Controls.Add(schoolLabel, 0, 1);
        contentPanel.Controls.Add(welcomeLabel, 0, 2);

        titleLabel.Name = "titleLabel";
        titleLabel.Text = "QUẢN LÝ THƯ VIỆN DAU";
        titleLabel.Font = new Font("Segoe UI", 24, FontStyle.Bold);
        titleLabel.ForeColor = Color.FromArgb(28, 55, 83);
        titleLabel.Dock = DockStyle.Fill;
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        schoolLabel.Name = "schoolLabel";
        schoolLabel.Text = "Trường Đại học Kiến Trúc Đà Nẵng";
        schoolLabel.Font = new Font("Segoe UI", 13);
        schoolLabel.Dock = DockStyle.Fill;
        schoolLabel.TextAlign = ContentAlignment.MiddleLeft;
        welcomeLabel.Name = "welcomeLabel";
        welcomeLabel.Text = "Chào mừng đến với hệ thống quản lý thư viện.\n\nCác phân hệ độc giả, tài liệu, mượn–trả, kiểm kê và báo cáo\nsẽ được bổ sung theo kế hoạch thực hiện của nhóm.";
        welcomeLabel.Font = new Font("Segoe UI", 12);
        welcomeLabel.Dock = DockStyle.Fill;
        welcomeLabel.TextAlign = ContentAlignment.MiddleLeft;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 620);
        MinimumSize = new Size(850, 540);
        Controls.Add(contentPanel);
        Controls.Add(statusStrip);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Quản lý thư viện DAU";
        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        contentPanel.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
