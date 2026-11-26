using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TranMyNhung_LTMMT_TV03.VP03
{
    public class VoidViolationForm : Form
    {
        private readonly VoidViolationService _service = new VoidViolationService();

        private readonly TextBox _txtKeyword = new TextBox { Width = 220 };
        private readonly CheckBox _chkShowVoided = new CheckBox { Text = "Hiển thị cả phiếu đã hủy", AutoSize = true, Padding = new Padding(6, 5, 0, 0) };
        private readonly Button _btnSearch = new Button { Text = "Tìm phiếu phạt", Width = 120 };

        private readonly DataGridView _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        private readonly NumericUpDown _numManagerId = new NumericUpDown { Width = 70, Minimum = 1, Maximum = 9999, Value = 1 };
        private readonly TextBox _txtReason = new TextBox { Width = 380 };
        private readonly Button _btnVoid = new Button { Text = "Xác nhận hủy hiệu lực phiếu phạt", Width = 230, Height = 36, Enabled = false };

        private List<ViolationLookupItem> _items = new List<ViolationLookupItem>();

        public VoidViolationForm()
        {
            Text = "VP-03 · Hủy hiệu lực phiếu phạt (Chỉ Quản lý thư viện - TV03)";
            Size = new Size(1040, 580);
            StartPosition = FormStartPosition.CenterParent;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var grpTop = new GroupBox { Text = "1. Tra cứu biên bản / phiếu phạt", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var topRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            topRow.Controls.Add(new Label { Text = "Mã thẻ / Họ tên / Mã VP:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            topRow.Controls.Add(_txtKeyword);
            topRow.Controls.Add(_chkShowVoided);
            topRow.Controls.Add(_btnSearch);
            grpTop.Controls.Add(topRow);

            _grid.Columns.Add("Id", "Mã VP");
            _grid.Columns.Add("Reader", "Độc giả");
            _grid.Columns.Add("Type", "Loại vi phạm");
            _grid.Columns.Add("Amount", "Số tiền");
            _grid.Columns.Add("Status", "Trạng thái");
            _grid.Columns.Add("Receipt", "Chứng từ thu");
            _grid.Columns.Add("CreatedAt", "Ngày lập");
            _grid.Columns.Add("Desc", "Mô tả & Lịch sử xử lý");

            var grpBottom = new GroupBox { Text = "2. Hủy hiệu lực (Yêu cầu quyền QUAN_LY_THU_VIEN & Bắt buộc ghi lý do)", Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(10) };
            var bottomRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            bottomRow.Controls.Add(new Label { Text = "ID Tài khoản Quản lý:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            bottomRow.Controls.Add(_numManagerId);
            bottomRow.Controls.Add(new Label { Text = "Lý do hủy hiệu lực:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            bottomRow.Controls.Add(_txtReason);
            bottomRow.Controls.Add(_btnVoid);
            grpBottom.Controls.Add(bottomRow);

            root.Controls.Add(grpTop, 0, 0);
            root.Controls.Add(_grid, 0, 1);
            root.Controls.Add(grpBottom, 0, 2);
            Controls.Add(root);

            _btnSearch.Click += async (s, e) => await LoadDataAsync();
            _txtKeyword.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await LoadDataAsync(); } };
            _chkShowVoided.CheckedChanged += async (s, e) => await LoadDataAsync();
            _grid.SelectionChanged += (s, e) => UpdateVoidButton();
            _btnVoid.Click += async (s, e) => await SubmitVoidAsync();

            Load += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _btnSearch.Enabled = false;
                _items = await _service.SearchViolationsAsync(_txtKeyword.Text, _chkShowVoided.Checked);
                _grid.Rows.Clear();

                foreach (var x in _items)
                {
                    int idx = _grid.Rows.Add(
                        $"#{x.ViolationId}",
                        $"{x.ReaderName} ({x.ReaderCode})",
                        x.ViolationType,
                        $"{x.Amount:N0} đ",
                        x.Status,
                        x.ReceiptId.HasValue ? $"Phiếu thu #{x.ReceiptId.Value}" : "Chưa thu tiền",
                        x.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        x.Description);

                    if (x.Status == "DA_HUY")
                        _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.Gray;
                    else if (x.Status == "DA_XU_LY")
                        _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.DarkGreen;
                    else
                        _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.Firebrick;
                }

                UpdateVoidButton();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Lỗi tải danh sách vi phạm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnSearch.Enabled = true;
            }
        }

        private void UpdateVoidButton()
        {
            if (_grid.CurrentRow != null && _grid.CurrentRow.Index >= 0 && _grid.CurrentRow.Index < _items.Count)
                _btnVoid.Enabled = _items[_grid.CurrentRow.Index].Status != "DA_HUY";
            else
                _btnVoid.Enabled = false;
        }

        private async Task SubmitVoidAsync()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.Index < 0 || _grid.CurrentRow.Index >= _items.Count) return;
            var selected = _items[_grid.CurrentRow.Index];

            string warn = selected.ReceiptId.HasValue
                ? $"Phiếu phạt #{selected.ViolationId} ĐÃ THU TIỀN (Chứng từ PhieuThu #{selected.ReceiptId.Value}).\nKhi hủy hiệu lực, hệ thống vẫn GIỮ NGUYÊN chứng từ tiền đã thu và ghi nhật ký đối soát.\n\nBạn có chắc chắn muốn tiếp tục?"
                : $"Bạn có chắc chắn muốn hủy hiệu lực phiếu phạt #{selected.ViolationId} ({selected.Amount:N0} đ) của độc giả {selected.ReaderName}?";

            if (MessageBox.Show(this, warn, "Xác nhận hủy hiệu lực", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                _btnVoid.Enabled = false;
                var req = new VoidViolationRequest
                {
                    ViolationId = selected.ViolationId,
                    ManagerAccountId = (int)_numManagerId.Value,
                    Reason = _txtReason.Text
                };

                var res = await _service.VoidViolationAsync(req);

                string receiptNote = res.HasExistingReceipt
                    ? $"\nChứng từ thu tiền: Vẫn giữ nguyên PhieuThu #{res.ReceiptId.Value} theo quy định kế toán."
                    : "";
                string unlockNote = res.ReaderUnlocked
                    ? "\nTrạng thái thẻ: Đã tự động mở khóa quyền mượn."
                    : "";

                MessageBox.Show(this,
                    $"Đã hủy hiệu lực phiếu phạt #{res.ViolationId} thành công!\n" +
                    $"Người duyệt hủy: {res.ManagerName} | Thời gian: {res.VoidedAt:dd/MM/yyyy HH:mm}\n" +
                    $"Lý do đã lưu nhật ký: {res.Reason}" + receiptNote + unlockNote,
                    "Hoàn tất VP-03", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _txtReason.Clear();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không thể hủy hiệu lực", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateVoidButton();
            }
        }
    }
}