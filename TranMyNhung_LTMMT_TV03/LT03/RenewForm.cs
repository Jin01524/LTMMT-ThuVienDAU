using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TranMyNhung_LTMMT_TV03.LT03
{
    public class RenewForm : Form
    {
        private readonly RenewService _service = new RenewService();

        private readonly ComboBox _cboChannel = new ComboBox { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox _txtKeyword = new TextBox { Width = 230 };
        private readonly NumericUpDown _numDays = new NumericUpDown { Width = 65, Minimum = 1, Maximum = 15, Value = 7 };
        private readonly Button _btnSearch = new Button { Text = "Tra cứu sách mượn", Width = 140 };

        private readonly DataGridView _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        private readonly Button _btnRenew = new Button { Text = "Xác nhận gia hạn dòng chọn", Width = 210, Height = 36, Enabled = false };
        private List<RenewableLoanItem> _items = new List<RenewableLoanItem>();

        public RenewForm()
        {
            Text = "LT-03 · Gia hạn trực tiếp hoặc qua cổng độc giả (TV03)";
            Size = new Size(980, 540);
            StartPosition = FormStartPosition.CenterParent;

            _cboChannel.Items.AddRange(new object[] { "TAI_QUAY", "CONG_DOC_GIA" });
            _cboChannel.SelectedIndex = 0;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var grpFilter = new GroupBox { Text = "Tra cứu khoản mượn & Chọn kênh gia hạn", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var row = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            row.Controls.Add(new Label { Text = "Kênh thực hiện:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            row.Controls.Add(_cboChannel);
            row.Controls.Add(new Label { Text = "Mã thẻ / Barcode:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            row.Controls.Add(_txtKeyword);
            row.Controls.Add(new Label { Text = "Số ngày thêm:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            row.Controls.Add(_numDays);
            row.Controls.Add(_btnSearch);
            grpFilter.Controls.Add(row);

            _grid.Columns.Add("LoanId", "Mã phiếu");
            _grid.Columns.Add("Reader", "Độc giả");
            _grid.Columns.Add("Barcode", "Mã vạch");
            _grid.Columns.Add("TitleName", "Nhan đề tài liệu");
            _grid.Columns.Add("DueDate", "Hạn trả hiện tại");
            _grid.Columns.Add("RenewCount", "Đã gia hạn");
            _grid.Columns.Add("Status", "Trạng thái điều kiện");

            var bottomBar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
            bottomBar.Controls.Add(_btnRenew);

            root.Controls.Add(grpFilter, 0, 0);
            root.Controls.Add(_grid, 0, 1);
            root.Controls.Add(bottomBar, 0, 2);
            Controls.Add(root);

            _btnSearch.Click += async (s, e) => await SearchAsync();
            _txtKeyword.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SearchAsync(); } };
            _cboChannel.SelectedIndexChanged += async (s, e) => { if (!string.IsNullOrWhiteSpace(_txtKeyword.Text)) await SearchAsync(); };
            _grid.SelectionChanged += (s, e) => UpdateRenewButton();
            _btnRenew.Click += async (s, e) => await RenewSelectedAsync();
        }

        private async Task SearchAsync()
        {
            try
            {
                _btnSearch.Enabled = false;
                string channel = _cboChannel.SelectedItem?.ToString() ?? "TAI_QUAY";
                _items = await _service.GetActiveLoansAsync(_txtKeyword.Text, channel);
                RefreshGrid();
            }
            catch (Exception ex)
            {
                _items.Clear();
                RefreshGrid();
                MessageBox.Show(this, ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnSearch.Enabled = true;
            }
        }

        private void RefreshGrid()
        {
            _grid.Rows.Clear();
            foreach (var x in _items)
            {
                int rowIdx = _grid.Rows.Add(
                    $"#{x.LoanId}",
                    $"{x.ReaderName} ({x.ReaderCode})",
                    x.Barcode,
                    x.TitleName,
                    x.DueDate.ToString("dd/MM/yyyy HH:mm"),
                    $"{x.RenewCount} lần",
                    x.BlockReason);

                _grid.Rows[rowIdx].DefaultCellStyle.ForeColor = x.CanRenew ? Color.DarkGreen : Color.Firebrick;
            }
            UpdateRenewButton();
        }

        private void UpdateRenewButton()
        {
            if (_grid.CurrentRow != null && _grid.CurrentRow.Index >= 0 && _grid.CurrentRow.Index < _items.Count)
                _btnRenew.Enabled = _items[_grid.CurrentRow.Index].CanRenew;
            else
                _btnRenew.Enabled = false;
        }

        private async Task RenewSelectedAsync()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.Index < 0 || _grid.CurrentRow.Index >= _items.Count) return;

            var selected = _items[_grid.CurrentRow.Index];
            string channel = _cboChannel.SelectedItem?.ToString() ?? "TAI_QUAY";
            int days = (int)_numDays.Value;

            try
            {
                _btnRenew.Enabled = false;
                var res = await _service.RenewAsync(selected.DetailId, channel, days);
                MessageBox.Show(this,
                    $"Gia hạn thành công ({res.Channel})!\nTài liệu: [{res.Barcode}] {res.TitleName}\nHạn cũ: {res.OldDueDate:dd/MM/yyyy HH:mm}\nHạn mới: {res.NewDueDate:dd/MM/yyyy HH:mm}\nSố lần đã gia hạn: {res.NewRenewCount}",
                    "Hoàn tất LT-03", MessageBoxButtons.OK, MessageBoxIcon.Information);

                await SearchAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không thể gia hạn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdateRenewButton();
            }
        }
    }
}