#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThuVienDAU.Circulation.LT04
{
    public class LoanHistoryForm : Form
    {
        private readonly LoanHistoryService _service;

        private readonly TextBox _txtKeyword = new TextBox { Width = 220 };
        private readonly ComboBox _cboStatus = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly CheckBox _chkDate = new CheckBox { Text = "Lọc ngày mượn:", AutoSize = true, Padding = new Padding(6, 4, 0, 0) };
        private readonly DateTimePicker _dtFrom = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Enabled = false };
        private readonly DateTimePicker _dtTo = new DateTimePicker { Width = 115, Format = DateTimePickerFormat.Short, Enabled = false };
        private readonly Button _btnSearch = new Button { Text = "Tra cứu", Width = 100 };

        private readonly DataGridView _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        private readonly Label _lblStats = new Label { AutoSize = true, Text = "Đang tải dữ liệu..." };
        private List<LoanHistoryRecord> _records = new List<LoanHistoryRecord>();

        public LoanHistoryForm(ThuVienDAU.Models.SessionInfo session)
        {
            _service = new LoanHistoryService(session);
            Text = "LT-04 · In phiếu mượn, biên lai và lịch sử";
            Size = new Size(1060, 600);
            StartPosition = FormStartPosition.CenterParent;

            _cboStatus.Items.AddRange(new object[] { "TAT_CA", "DANG_MUON", "QUA_HAN", "DA_TRA", "HU_HONG", "MAT" });
            _cboStatus.SelectedIndex = 0;
            _dtFrom.Value = DateTime.Today.AddMonths(-1);
            _dtTo.Value = DateTime.Today;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var grpFilter = new GroupBox { Text = "Bộ lọc tra cứu phiếu mượn & lịch sử lưu thông", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var row = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            row.Controls.Add(new Label { Text = "Từ khóa (Mã thẻ/Tên/Barcode/Mã phiếu):", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            row.Controls.Add(_txtKeyword);
            row.Controls.Add(new Label { Text = "Trạng thái:", AutoSize = true, Padding = new Padding(6, 6, 0, 0) });
            row.Controls.Add(_cboStatus);
            row.Controls.Add(_chkDate);
            row.Controls.Add(_dtFrom);
            row.Controls.Add(new Label { Text = "→", AutoSize = true, Padding = new Padding(2, 6, 2, 0) });
            row.Controls.Add(_dtTo);
            row.Controls.Add(_btnSearch);
            grpFilter.Controls.Add(row);

            _grid.Columns.Add("LoanId", "Mã phiếu");
            _grid.Columns.Add("LoanStatus", "TT Phiếu");
            _grid.Columns.Add("Reader", "Độc giả");
            _grid.Columns.Add("Barcode", "Mã vạch");
            _grid.Columns.Add("TitleName", "Nhan đề tài liệu");
            _grid.Columns.Add("BorrowedAt", "Ngày mượn");
            _grid.Columns.Add("DueDate", "Hạn trả");
            _grid.Columns.Add("ReturnedAt", "Ngày trả");
            _grid.Columns.Add("Renew", "Gia hạn");
            _grid.Columns.Add("Status", "Trạng thái sách");

            root.Controls.Add(grpFilter, 0, 0);
            root.Controls.Add(_grid, 0, 1);
            root.Controls.Add(_lblStats, 0, 2);
            Controls.Add(root);
            var printBar = new FlowLayoutPanel { Dock=DockStyle.Bottom, Height=48 };
            var kind = new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList, Width=200 };
            kind.Items.AddRange(new object[] { "Phiếu mượn", "Biên lai", "Thông báo đơn vị" }); kind.SelectedIndex=0;
            var receiptId = new NumericUpDown { Minimum=1, Maximum=int.MaxValue, Width=110 };
            var preview = new Button { Text="Xem / In lại", AutoSize=true };
            printBar.Controls.AddRange(new Control[] {kind,new Label {Text="Mã phiếu:",AutoSize=true},receiptId,preview});
            preview.Click += async (_,_) => await _service.PreviewAsync(this,kind.SelectedIndex==0?"LOAN":kind.SelectedIndex==1?"RECEIPT":"NOTICE",(int)receiptId.Value);
            Controls.Add(printBar);

            _chkDate.CheckedChanged += (s, e) =>
            {
                _dtFrom.Enabled = _chkDate.Checked;
                _dtTo.Enabled = _chkDate.Checked;
            };
            _btnSearch.Click += async (s, e) => await LoadDataAsync();
            _txtKeyword.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await LoadDataAsync(); } };
            _cboStatus.SelectedIndexChanged += async (s, e) => await LoadDataAsync();
            _grid.CellDoubleClick += (s, e) => ShowDetailDialog(e.RowIndex);

            Load += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _btnSearch.Enabled = false;
                var filter = new LoanHistoryFilter
                {
                    Keyword = _txtKeyword.Text,
                    StatusFilter = _cboStatus.SelectedItem?.ToString() ?? "TAT_CA",
                    UseDateRange = _chkDate.Checked,
                    FromDate = _dtFrom.Value,
                    ToDate = _dtTo.Value
                };

                _records = await _service.SearchHistoryAsync(filter);
                RefreshGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Lỗi tải lịch sử", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnSearch.Enabled = true;
            }
        }

        private void RefreshGrid()
        {
            _grid.Rows.Clear();
            foreach (var r in _records)
            {
                string statusText = r.IsOverdue
                    ? $"QUÁ HẠN ({r.OverdueDays} ngày)"
                    : r.DetailStatus;

                int idx = _grid.Rows.Add(
                    $"#{r.LoanId}",
                    r.LoanStatus,
                    $"{r.ReaderName} ({r.ReaderCode})",
                    r.Barcode,
                    r.TitleName,
                    r.BorrowedAt.ToString("dd/MM/yyyy HH:mm"),
                    r.DueDate.ToString("dd/MM/yyyy HH:mm"),
                    r.ReturnedAt.HasValue ? r.ReturnedAt.Value.ToString("dd/MM/yyyy HH:mm") : "—",
                    $"{r.RenewCount} lần",
                    statusText);

                if (r.IsOverdue || r.DetailStatus == "HU_HONG" || r.DetailStatus == "MAT")
                    _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.Firebrick;
                else if (r.DetailStatus == "DANG_MUON")
                    _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.DarkBlue;
                else
                    _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.DarkGreen;
            }

            int activeCount = _records.Count(x => x.DetailStatus == "DANG_MUON");
            int overdueCount = _records.Count(x => x.IsOverdue);
            int returnedCount = _records.Count(x => x.DetailStatus == "DA_TRA");

            _lblStats.Text = $"Tổng hiển thị: {_records.Count} lượt | Đang mượn (phiếu mở): {activeCount} | Đang quá hạn: {overdueCount} | Đã trả: {returnedCount}  (Nhấp đúp vào dòng để xem chi tiết)";
        }

        private void ShowDetailDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _records.Count) return;
            var r = _records[rowIndex];

            string msg =
                $"Mã phiếu mượn: #{r.LoanId} ({r.LoanStatus})\n" +
                $"Độc giả: {r.ReaderName} - Mã thẻ: {r.ReaderCode} ({r.ReaderType})\n" +
                $"Tài liệu: [{r.Barcode} | ĐKCB: {r.RegistrationNumber}] {r.TitleName}\n" +
                $"Thời điểm mượn: {r.BorrowedAt:dd/MM/yyyy HH:mm}\n" +
                $"Hạn trả: {r.DueDate:dd/MM/yyyy HH:mm} (Đã gia hạn {r.RenewCount} lần)\n" +
                $"Thời điểm trả: {(r.ReturnedAt.HasValue ? r.ReturnedAt.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa trả")}\n" +
                $"Trạng thái: {(r.IsOverdue ? $"QUÁ HẠN {r.OverdueDays} NGÀY" : r.DetailStatus)}\n" +
                $"Tình trạng khi trả: {(string.IsNullOrEmpty(r.ReturnCondition) ? "—" : r.ReturnCondition)}\n" +
                $"Ghi chú: {(string.IsNullOrEmpty(r.Note) ? "Không có" : r.Note)}";

            MessageBox.Show(this, msg, $"Chi tiết khoản mượn #{r.DetailId}", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}