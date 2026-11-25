using System.Text;
using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU.Forms;

public sealed class SystemAdministrationForm : Form
{
    private readonly SystemAdministrationClientService service;
    private readonly SessionContext sessionContext;
    private readonly TextBox configurationText = new();
    private readonly ListBox pendingList = new();
    private readonly DataGridView auditGrid = new();
    private readonly DateTimePicker fromPicker = new();
    private readonly DateTimePicker toPicker = new();

    public SystemAdministrationForm(SystemAdministrationClientService service, SessionContext sessionContext)
    {
        this.service = service;
        this.sessionContext = sessionContext;
        InitializeUi();
    }

    private void InitializeUi()
    {
        Text = "Cấu hình hệ thống và nhật ký";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 600);
        ClientSize = new Size(1100, 700);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var configurationTab = new TabPage("Cấu hình") { Padding = new Padding(12) };
        configurationText.Dock = DockStyle.Fill;
        configurationText.Multiline = true;
        configurationText.ReadOnly = true;
        configurationText.ScrollBars = ScrollBars.Both;
        configurationText.Font = new Font("Consolas", 10);
        pendingList.Dock = DockStyle.Bottom;
        pendingList.Height = 125;
        configurationTab.Controls.Add(configurationText);
        configurationTab.Controls.Add(pendingList);

        var auditTab = new TabPage("Nhật ký") { Padding = new Padding(12) };
        var filter = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false };
        fromPicker.Format = DateTimePickerFormat.Short;
        toPicker.Format = DateTimePickerFormat.Short;
        fromPicker.Value = DateTime.Today.AddDays(-30);
        toPicker.Value = DateTime.Today;
        var loadButton = new Button { Text = "Tra cứu", Width = 100, Height = 30 };
        loadButton.Click += async (_, _) => await LoadAuditAsync();
        filter.Controls.AddRange([new Label { Text = "Từ ngày", AutoSize = true, Padding = new Padding(0, 8, 0, 0) },
            fromPicker, new Label { Text = "Đến ngày", AutoSize = true, Padding = new Padding(0, 8, 0, 0) },
            toPicker, loadButton]);
        auditGrid.Dock = DockStyle.Fill;
        auditGrid.ReadOnly = true;
        auditGrid.AllowUserToAddRows = false;
        auditGrid.AllowUserToDeleteRows = false;
        auditGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        auditGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        auditTab.Controls.Add(auditGrid);
        auditTab.Controls.Add(filter);

        tabs.TabPages.Add(configurationTab);
        tabs.TabPages.Add(auditTab);
        Controls.Add(tabs);
        Load += async (_, _) => await LoadConfigurationAsync();
    }

    private async Task LoadConfigurationAsync()
    {
        var session = sessionContext.Current;
        if (session is null) return;
        try
        {
            var configuration = await service.GetConfigurationAsync(session.SessionId);
            configurationText.Text = configuration.Json;
            pendingList.Items.Clear();
            foreach (var pending in configuration.PendingDecisions) pendingList.Items.Add(pending);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task LoadAuditAsync()
    {
        var session = sessionContext.Current;
        if (session is null) return;
        try
        {
            var from = fromPicker.Value.Date;
            var to = toPicker.Value.Date.AddDays(1);
            var entries = await service.GetAuditLogAsync(session.SessionId, from, to);
            auditGrid.DataSource = entries.ToList();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Nhật ký", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
