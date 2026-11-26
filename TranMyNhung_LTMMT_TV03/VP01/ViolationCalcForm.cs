using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TranMyNhung_LTMMT_TV03.VP01
{
    public class ViolationCalcForm : Form
    {
        private readonly ViolationService _service = new ViolationService();

        private readonly TextBox _txtKeyword = new TextBox { Width = 220 };
        private readonly Button _btnRefresh = new Button { Text = "Rà soát vi phạm", Width = 130 };

        private readonly DataGridView _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        private readonly ComboBox _cboType = new ComboBox { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown _numDays = new NumericUpDown { Width = 70, Minimum = 0, Maximum = 365 };
        private readonly NumericUpDown _numAmount = new NumericUpDown { Width = 130, Minimum = 0, Maximum = 50000000, Increment = 1000, ThousandsSeparator = true };
        private readonly TextBox _txtDesc = new TextBox { Width = 340 };
        private readonly CheckBox _chkForceCalendar = new CheckBox { Text = "Xác nhận đã rà soát ngày nghỉ lễ", AutoSize = true, Padding = new Padding(6, 5, 0, 0) };
        private readonly Button _btnCreate = new Button { Text = "Lập biên bản / Thông báo vi phạm", Width = 230, Height = 36, Enabled = false };

        private List<ViolationCandidate> _candidates = new List<ViolationCandidate>();

        public ViolationCalcForm()
        {
            Text = "VP-01 · Quá hạn và bồi thường (TV03 - Trần Mỹ Nhung)";
            Size = new Size(1060, 600);
            StartPosition = FormStartPosition.CenterParent;

            _cboType.Items.AddRange(new object[] { "QUA_HAN", "THONG_BAO_DON_VI", "HU_HONG", "MAT" });
            _cboType.SelectedIndex = 0;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var grpTop = new GroupBox { Text = "1. Danh sách khoản mượn quá hạn / mất / hư hỏng chưa lập biên bản", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var topRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            topRow.Controls.Add(new Label { Text = "Tìm theo mã thẻ / tên / barcode:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            topRow.Controls.Add(_txtKeyword);
            topRow.Controls.Add(_btnRefresh);
            grpTop.Controls.Add(topRow);

            _grid.Columns.Add("LoanId", "Mã phiếu");
            _grid.Columns.Add("Reader", "Độc giả");
            _grid.Columns.Add("Group", "Đối tượng");
            _grid.Columns.Add("Book", "Tài liệu");
            _grid.Columns.Add("Status", "Tình trạng");
            _grid.Columns.Add("Days", "Số ngày tính phạt");
            _grid.Columns.Add("Amount", "Mức đề xuất");
            _grid.Columns.Add("Note", "Diễn giải quy tắc");

            var grpAction = new GroupBox { Text = "2. Chốt mức phạt / Bồi thường / Thông báo đơn vị", Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(10) };
            var actionLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, AutoSize = true };

            var row1 = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            row1.Controls.Add(new Label { Text = "Loại xử lý:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            row1.Controls.Add(_cboType);
            row1.Controls.Add(new Label { Text = "Ngày trễ thực tính:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            row1.Controls.Add(_numDays);
            row1.Controls.Add(new Label { Text = "Số tiền (VNĐ):", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            row1.Controls.Add(_numAmount);
            row1.Controls.Add(_chkForceCalendar);

            var row2 = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
            row2.Controls.Add(new Label { Text = "Nội dung / Căn cứ:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            row2.Controls.Add(_txtDesc);
            row2.Controls.Add(_btnCreate);

            actionLayout.Controls.Add(row1, 0, 0);
            actionLayout.Controls.Add(row2, 0, 1);
            grpAction.Controls.Add(actionLayout);

            root.Controls.Add(grpTop, 0, 0);
            root.Controls.Add(_grid, 0, 1);
            root.Controls.Add(grpAction, 0, 2);
            Controls.Add(root);

            _btnRefresh.Click += async (s, e) => await LoadCandidatesAsync();
            _txtKeyword.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await LoadCandidatesAsync(); } };
            _grid.SelectionChanged += (s, e) => BindSelectedCandidate();
            _numDays.ValueChanged += (s, e) => RecalculateByDays();
            _btnCreate.Click += async (s, e) => await SubmitViolationAsync();

            Load += async (s, e) => await LoadCandidatesAsync();
        }

        private async Task LoadCandidatesAsync()
        {
            try
            {
                _btnRefresh.Enabled = false;
                _candidates = await _service.GetPendingCandidatesAsync(_txtKeyword.Text);
                _grid.Rows.Clear();

                foreach (var c in _candidates)
                {
                    int idx = _grid.Rows.Add(
                        $"#{c.LoanId}",
                        $"{c.ReaderName} ({c.ReaderCode})",
                        c.ReaderType,
                        $"[{c.Barcode}] {c.TitleName}",
                        c.DetailStatus,
                        $"{c.ChargeableDays} ngày (thô {c.RawOverdueDays})",
                        c.ViolationType == "THONG_BAO_DON_VI" ? "Thông báo ĐV" : $"{c.SuggestedAmount:N0} đ",
                        c.CalculationNote);

                    if (!c.IsCalendarFinalized && c.ViolationType == "QUA_HAN")
                        _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.DarkOrange;
                    else
                        _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.Firebrick;
                }

                BindSelectedCandidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Lỗi rà soát vi phạm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnRefresh.Enabled = true;
            }
        }

        private void BindSelectedCandidate()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.Index < 0 || _grid.CurrentRow.Index >= _candidates.Count)
            {
                _btnCreate.Enabled = false;
                return;
            }

            var c = _candidates[_grid.CurrentRow.Index];
            _cboType.SelectedItem = c.ViolationType;
            _numDays.Value = Math.Min(_numDays.Maximum, Math.Max(0, c.ChargeableDays));
            _numAmount.Value = Math.Min(_numAmount.Maximum, Math.Max(0, c.SuggestedAmount));
            _txtDesc.Text = c.CalculationNote;
            _chkForceCalendar.Checked = c.IsCalendarFinalized;
            _btnCreate.Enabled = true;
        }

        private void RecalculateByDays()
        {
            if (_cboType.SelectedItem?.ToString() == "QUA_HAN")
            {
                _numAmount.Value = _numDays.Value * 2000m;
            }
        }

        private async Task SubmitViolationAsync()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.Index < 0 || _grid.CurrentRow.Index >= _candidates.Count) return;
            var c = _candidates[_grid.CurrentRow.Index];

            try
            {
                _btnCreate.Enabled = false;
                var input = new CreateViolationInput
                {
                    DetailId = c.DetailId,
                    ViolationType = _cboType.SelectedItem?.ToString() ?? c.ViolationType,
                    ChargeableDays = (int)_numDays.Value,
                    Amount = _numAmount.Value,
                    Description = _txtDesc.Text.Trim(),
                    ConfirmUnfinalizedCalendar = _chkForceCalendar.Checked
                };

                var res = await _service.CreateViolationAsync(input);
                string msg = res.ViolationType == "THONG_BAO_DON_VI"
                    ? $"Đã lập Thông báo đơn vị #{res.ViolationId} cho CB/GV {res.ReaderName} ({res.ReaderCode})."
                    : $"Đã lập biên bản vi phạm #{res.ViolationId} cho {res.ReaderName} ({res.ReaderCode}).\nSố tiền: {res.Amount:N0} đ\nTrạng thái thẻ: Đã tự động khóa quyền mượn cho đến khi thanh toán.";

                MessageBox.Show(this, msg, "Hoàn tất VP-01", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadCandidatesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không thể lập biên bản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _btnCreate.Enabled = true;
            }
        }
    }
}