#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThuVienDAU.Circulation.LT02
{
    public class ReturnForm : Form
    {
        private readonly ReturnService _service;

        private readonly TextBox _txtBarcode = new TextBox { Width = 250 };
        private readonly Button _btnScan = new Button { Text = "Quét sách trả", Width = 120 };
        private readonly ComboBox _cboOutcome = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox _txtNote = new TextBox { Width = 220 };
        private readonly Button _btnRemove = new Button { Text = "Xóa dòng chọn", Width = 120, Enabled = false };

        private readonly DataGridView _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        private readonly Label _lblSummary = new Label { AutoSize = true, Text = "Quét mã vạch sách đang mượn để tiếp nhận trả (hỗ trợ trả từng cuốn / trả một phần)." };
        private readonly Button _btnSubmit = new Button { Text = "Xác nhận trả sách", Width = 170, Height = 36, Enabled = false };
        private readonly Button _btnReset = new Button { Text = "Làm mới", Width = 100, Height = 36 };

        private readonly List<(ActiveLoanCopyInfo Info, string Outcome, string Note)> _returnList = new List<(ActiveLoanCopyInfo, string, string)>();

        public ReturnForm(ThuVienDAU.Models.SessionInfo session)
        {
            _service = new ReturnService(session);
            Text = "LT-02 · Trả sách và tình trạng bản sách (TV03 - Trần Mỹ Nhung)";
            Size = new Size(960, 560);
            StartPosition = FormStartPosition.CenterParent;

            _cboOutcome.Items.AddRange(new object[] { "BINH_THUONG", "HU_HONG", "MAT" });
            _cboOutcome.SelectedIndex = 0;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var grpScan = new GroupBox { Text = "Quét bản sách đang mượn & Ghi nhận tình trạng", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var scanRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            scanRow.Controls.Add(new Label { Text = "Barcode / ĐKCB:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            scanRow.Controls.Add(_txtBarcode);
            scanRow.Controls.Add(new Label { Text = "Tình trạng trả:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            scanRow.Controls.Add(_cboOutcome);
            scanRow.Controls.Add(new Label { Text = "Ghi chú:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            scanRow.Controls.Add(_txtNote);
            scanRow.Controls.Add(_btnScan);
            scanRow.Controls.Add(_btnRemove);
            grpScan.Controls.Add(scanRow);

            _grid.Columns.Add("LoanId", "Mã phiếu");
            _grid.Columns.Add("Reader", "Độc giả");
            _grid.Columns.Add("Barcode", "Mã vạch");
            _grid.Columns.Add("TitleName", "Nhan đề tài liệu");
            _grid.Columns.Add("DueDate", "Hạn trả");
            _grid.Columns.Add("Overdue", "Quá hạn");
            _grid.Columns.Add("Outcome", "Tình trạng trả");
            _grid.Columns.Add("Note", "Ghi chú");

            var bottomBar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
            bottomBar.Controls.Add(_btnSubmit);
            bottomBar.Controls.Add(_btnReset);

            root.Controls.Add(grpScan, 0, 0);
            root.Controls.Add(_grid, 0, 1);
            root.Controls.Add(_lblSummary, 0, 2);
            root.Controls.Add(bottomBar, 0, 3);
            Controls.Add(root);

            _btnScan.Click += async (s, e) => await ScanCopyAsync();
            _txtBarcode.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await ScanCopyAsync(); } };
            _btnRemove.Click += (s, e) =>
            {
                if (_grid.CurrentRow != null && _grid.CurrentRow.Index >= 0 && _grid.CurrentRow.Index < _returnList.Count)
                {
                    _returnList.RemoveAt(_grid.CurrentRow.Index);
                    RefreshGrid();
                }
            };
            _btnReset.Click += (s, e) => ResetForm();
            _btnSubmit.Click += async (s, e) => await SubmitReturnAsync();
        }

        private async Task ScanCopyAsync()
        {
            try
            {
                _btnScan.Enabled = false;
                var info = await _service.LookupActiveBorrowByBarcodeAsync(_txtBarcode.Text);

                if (_returnList.Any(x => x.Info.DetailId == info.DetailId))
                    throw new InvalidOperationException("Bản sách này đã có trong danh sách chờ trả.");

                string outcome = _cboOutcome.SelectedItem?.ToString() ?? "BINH_THUONG";
                string note = _txtNote.Text.Trim();

                _returnList.Add((info, outcome, note));
                _txtBarcode.Clear();
                _txtNote.Clear();
                _cboOutcome.SelectedIndex = 0;
                _txtBarcode.Focus();
                RefreshGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không thể thêm bản trả", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtBarcode.SelectAll();
                _txtBarcode.Focus();
            }
            finally
            {
                _btnScan.Enabled = true;
            }
        }

        private async Task SubmitReturnAsync()
        {
            if (_returnList.Count == 0) return;

            try
            {
                _btnSubmit.Enabled = false;
                var inputs = _returnList.Select(x => new ReturnItemInput
                {
                    DetailId = x.Info.DetailId,
                    ReturnOutcome = x.Outcome,
                    BookCondition = x.Outcome == "BINH_THUONG" ? x.Info.CurrentCondition : x.Outcome,
                    Note = x.Note
                }).ToList();

                var results = await _service.ProcessReturnsAsync(inputs);
                var violations = results.Where(r => r.HasViolationBasis).ToList();

                string msg = $"Đã ghi nhận trả thành công {results.Count} cuốn sách!";
                if (violations.Count > 0)
                {
                    var lines = violations.Select(v => $"- [{v.Barcode}] {v.TitleName} ({v.ReaderName}): {v.ViolationSummary}");
                    msg += "\n\nCẢNH BÁO CĂN CỨ VI PHẠM (cần chuyển sang VP-01):\n" + string.Join("\n", lines);
                }

                MessageBox.Show(this, msg, "Hoàn tất LT-02", MessageBoxButtons.OK,
                    violations.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Lỗi xử lý trả sách", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _btnSubmit.Enabled = _returnList.Count > 0;
            }
        }

        private void RefreshGrid()
        {
            _grid.Rows.Clear();
            foreach (var item in _returnList)
            {
                _grid.Rows.Add(
                    $"#{item.Info.LoanId}",
                    $"{item.Info.ReaderName} ({item.Info.ReaderCode})",
                    item.Info.Barcode,
                    item.Info.TitleName,
                    item.Info.DueDate.ToString("dd/MM/yyyy HH:mm"),
                    item.Info.OverdueDays > 0 ? $"Trễ {item.Info.OverdueDays} ngày" : "Đúng hạn",
                    item.Outcome,
                    item.Note);
            }

            int violCount = _returnList.Count(x => x.Info.OverdueDays > 0 || x.Outcome != "BINH_THUONG");
            _lblSummary.ForeColor = violCount > 0 ? Color.Firebrick : Color.DarkGreen;
            _lblSummary.Text = _returnList.Count == 0
                ? "Quét mã vạch sách đang mượn để tiếp nhận trả (hỗ trợ trả từng cuốn / trả một phần)."
                : $"Tổng số chờ trả: {_returnList.Count} cuốn | Phát hiện căn cứ vi phạm (quá hạn / hư hỏng / mất): {violCount} cuốn.";

            _btnRemove.Enabled = _returnList.Count > 0;
            _btnSubmit.Enabled = _returnList.Count > 0;
        }

        private void ResetForm()
        {
            _returnList.Clear();
            _txtBarcode.Clear();
            _txtNote.Clear();
            _cboOutcome.SelectedIndex = 0;
            RefreshGrid();
            _txtBarcode.Focus();
        }
    }
}