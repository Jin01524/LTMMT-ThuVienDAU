using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TranMyNhung_LTMMT_TV03.BC03
{
    public class CirculationStatsForm : Form
    {
        private readonly CirculationStatsService _service = new CirculationStatsService();

        private readonly DateTimePicker _dtFrom = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short };
        private readonly DateTimePicker _dtTo = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short };
        private readonly NumericUpDown _numViewerId = new NumericUpDown { Width = 65, Minimum = 1, Maximum = 9999, Value = 1 };
        private readonly Button _btnLoad = new Button { Text = "Tổng hợp số liệu", Width = 145 };
        private readonly Label _lblViewerRole = new Label { AutoSize = true, Padding = new Padding(8, 6, 0, 0), ForeColor = Color.DarkBlue };

        private readonly Label _lblCardCirculation = new Label { AutoSize = true, Font = new Font(SystemFonts.DefaultFont.FontFamily, 9.5f, FontStyle.Regular) };
        private readonly Label _lblCardOverdueRoom = new Label { AutoSize = true, Font = new Font(SystemFonts.DefaultFont.FontFamily, 9.5f, FontStyle.Regular) };
        private readonly Label _lblCardFinance = new Label { AutoSize = true, Font = new Font(SystemFonts.DefaultFont.FontFamily, 9.5f, FontStyle.Regular) };

        private readonly DataGridView _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        public CirculationStatsForm()
        {
            Text = "BC-03 · Thống kê lưu thông, quá hạn và thu tiền (TV03 - Trần Mỹ Nhung)";
            Size = new Size(1060, 640);
            StartPosition = FormStartPosition.CenterParent;

            _dtFrom.Value = DateTime.Today.AddDays(-14);
            _dtTo.Value = DateTime.Today;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // 1. Thanh lọc thời gian & phân quyền
            var grpFilter = new GroupBox { Text = "1. Kỳ báo cáo & Kiểm tra quyền truy cập Dashboard", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var fRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            fRow.Controls.Add(new Label { Text = "Từ ngày:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            fRow.Controls.Add(_dtFrom);
            fRow.Controls.Add(new Label { Text = "Đến ngày:", AutoSize = true, Padding = new Padding(6, 6, 0, 0) });
            fRow.Controls.Add(_dtTo);
            fRow.Controls.Add(new Label { Text = "ID Tài khoản xem:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            fRow.Controls.Add(_numViewerId);
            fRow.Controls.Add(_btnLoad);
            fRow.Controls.Add(_lblViewerRole);
            grpFilter.Controls.Add(fRow);

            // 2. 3 thẻ KPI tổng hợp & đối chiếu tài chính
            var kpiGrid = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true, Padding = new Padding(0, 6, 0, 6) };
            kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

            var grp1 = new GroupBox { Text = "Lưu thông Mượn / Trả / Gia hạn", Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(10) };
            grp1.Controls.Add(_lblCardCirculation);

            var grp2 = new GroupBox { Text = "Quá hạn & Lượt phòng đọc", Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(10) };
            grp2.Controls.Add(_lblCardOverdueRoom);

            var grp3 = new GroupBox { Text = "Đối chiếu Phạt phát sinh vs Thực thu", Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(10) };
            grp3.Controls.Add(_lblCardFinance);

            kpiGrid.Controls.Add(grp1, 0, 0);
            kpiGrid.Controls.Add(grp2, 1, 0);
            kpiGrid.Controls.Add(grp3, 2, 0);

            // 3. Bảng chi tiết theo ngày
            var grpTable = new GroupBox { Text = "2. Chi tiết biến động theo ngày (truy vấn độc lập từng nghiệp vụ, không đếm trùng)", Dock = DockStyle.Fill, Padding = new Padding(10) };
            _grid.Columns.Add("Date", "Ngày");
            _grid.Columns.Add("Borrowed", "Lượt cuốn mượn");
            _grid.Columns.Add("Returned", "Lượt cuốn trả");
            _grid.Columns.Add("Room", "Lượt phòng đọc");
            _grid.Columns.Add("Assessed", "Tiền phạt phát sinh");
            _grid.Columns.Add("Collected", "Thực thu biên lai");
            grpTable.Controls.Add(_grid);

            root.Controls.Add(grpFilter, 0, 0);
            root.Controls.Add(kpiGrid, 0, 1);
            root.Controls.Add(grpTable, 0, 2);
            Controls.Add(root);

            _btnLoad.Click += async (s, e) => await LoadReportAsync();
            Load += async (s, e) => await LoadReportAsync();
        }

        private async Task LoadReportAsync()
        {
            try
            {
                _btnLoad.Enabled = false;
                var rep = await _service.GetDashboardReportAsync(_dtFrom.Value, _dtTo.Value, (int)_numViewerId.Value);

                _lblViewerRole.Text = $"Người xem: {rep.ViewerName} | Quyền: {rep.ViewerRole}";

                _lblCardCirculation.Text =
                    $"• Số phiếu mượn lập mới: {rep.TotalLoanTickets} phiếu\n" +
                    $"• Tổng lượt cuốn mượn: {rep.TotalBorrowedCopies} cuốn\n" +
                    $"• Tổng lượt cuốn đã trả: {rep.TotalReturnedCopies} cuốn\n" +
                    $"• Tổng lượt gia hạn: {rep.TotalRenewals} lượt";

                _lblCardOverdueRoom.Text =
                    $"• Đang quá hạn (hiện tại): {rep.CurrentOverdueCopies} cuốn\n" +
                    $"• Trả trễ hạn trong kỳ: {rep.LateReturnedInPeriod} cuốn\n" +
                    $"• Lượt vào phòng đọc (kỳ): {rep.ReadingRoomVisitsInPeriod} lượt\n" +
                    $"• Đang ngồi trong phòng đọc: {rep.CurrentlyInReadingRoom} người";

                if (rep.ViewerRole == "QUAN_LY_THU_VIEN")
                {
                    _lblCardFinance.Text =
                        $"• Phạt phát sinh trong kỳ: {rep.TotalAssessedFinesInPeriod:N0} đ\n" +
                        $"• Thực thu trên PhieuThu ({rep.ReceiptCountInPeriod} phiếu): {rep.TotalCollectedReceiptsInPeriod:N0} đ\n" +
                        $"  (Tiền mặt: {rep.CashCollectedInPeriod:N0} đ | CK: {rep.TransferCollectedInPeriod:N0} đ)\n" +
                        $"• Tổng nợ phạt chưa thu toàn hệ thống: {rep.TotalOutstandingUnpaidFines:N0} đ";
                }
                else
                {
                    _lblCardFinance.Text =
                        $"• Số biên lai thu trong kỳ: {rep.ReceiptCountInPeriod} phiếu\n" +
                        $"• Tổng thực thu trong kỳ: {rep.TotalCollectedReceiptsInPeriod:N0} đ\n" +
                        $"• Tổng nợ phạt chưa thu: {rep.TotalOutstandingUnpaidFines:N0} đ\n" +
                        $"(Chi tiết phân tách kênh thu hiển thị cho Quản lý thư viện)";
                }

                _grid.Rows.Clear();
                foreach (var row in rep.DailyRows)
                {
                    _grid.Rows.Add(
                        row.ReportDate.ToString("dd/MM/yyyy"),
                        $"{row.BorrowedCount} cuốn",
                        $"{row.ReturnedCount} cuốn",
                        $"{row.ReadingRoomVisits} lượt",
                        $"{row.FineAssessed:N0} đ",
                        $"{row.FineCollected:N0} đ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không thể tải báo cáo BC-03", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnLoad.Enabled = true;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // CirculationStatsForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "CirculationStatsForm";
            this.ResumeLayout(false);

        }
    }
}