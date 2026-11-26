using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TranMyNhung_LTMMT_TV03.VP02
{
    public class FineReceiptForm : Form
    {
        private readonly FineReceiptService _service = new FineReceiptService();

        private readonly TextBox _txtKeyword = new TextBox { Width = 220 };
        private readonly Button _btnSearch = new Button { Text = "Tải khoản chưa thu", Width = 140 };

        private readonly DataGridView _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        private readonly ComboBox _cboMethod = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox _txtNote = new TextBox { Width = 260 };
        private readonly Label _lblTotal = new Label { AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold), Text = "Đã chọn: 0 khoản | Tổng thu: 0 đ" };
        private readonly Button _btnCollect = new Button { Text = "Thu tiền & In biên lai", Width = 190, Height = 36, Enabled = false };

        private List<UnpaidViolationItem> _items = new List<UnpaidViolationItem>();

        public FineReceiptForm()
        {
            Text = "VP-02 · Thu tiền và biên lai (TV03 - Trần Mỹ Nhung)";
            Size = new Size(1040, 580);
            StartPosition = FormStartPosition.CenterParent;

            _cboMethod.Items.AddRange(new object[] { "TIEN_MAT", "CHUYEN_KHOAN" });
            _cboMethod.SelectedIndex = 0;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var grpTop = new GroupBox { Text = "1. Tra cứu các khoản vi phạm chưa thanh toán", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var topRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            topRow.Controls.Add(new Label { Text = "Mã thẻ độc giả / Họ tên / Mã VP:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            topRow.Controls.Add(_txtKeyword);
            topRow.Controls.Add(_btnSearch);
            grpTop.Controls.Add(topRow);

            var chkCol = new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "Chọn thu", Width = 70 };
            _grid.Columns.Add(chkCol);
            _grid.Columns.Add("ViolationId", "Mã VP");
            _grid.Columns.Add("Reader", "Độc giả");
            _grid.Columns.Add("Type", "Loại vi phạm");
            _grid.Columns.Add("Book", "Tài liệu liên quan");
            _grid.Columns.Add("Amount", "Số tiền phải thu");
            _grid.Columns.Add("CreatedAt", "Ngày lập");
            _grid.Columns.Add("Desc", "Căn cứ / Mô tả");

            foreach (DataGridViewColumn col in _grid.Columns)
            {
                if (col.Name != "Selected") col.ReadOnly = true;
            }

            var grpBottom = new GroupBox { Text = "2. Lập biên lai thu tiền (gộp nhiều khoản cùng độc giả)", Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(10) };
            var bottomRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            bottomRow.Controls.Add(new Label { Text = "Hình thức thu:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            bottomRow.Controls.Add(_cboMethod);
            bottomRow.Controls.Add(new Label { Text = "Ghi chú biên lai:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            bottomRow.Controls.Add(_txtNote);
            bottomRow.Controls.Add(_lblTotal);
            bottomRow.Controls.Add(_btnCollect);
            grpBottom.Controls.Add(bottomRow);

            root.Controls.Add(grpTop, 0, 0);
            root.Controls.Add(_grid, 0, 1);
            root.Controls.Add(grpBottom, 0, 2);
            Controls.Add(root);

            _btnSearch.Click += async (s, e) => await LoadDataAsync();
            _txtKeyword.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await LoadDataAsync(); } };
            _grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.CellValueChanged += (s, e) => { if (e.ColumnIndex == 0) RecalculateSelected(); };
            _btnCollect.Click += async (s, e) => await SubmitCollectAsync();

            Load += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _btnSearch.Enabled = false;
                _items = await _service.GetUnpaidViolationsAsync(_txtKeyword.Text);
                _grid.Rows.Clear();

                foreach (var x in _items)
                {
                    _grid.Rows.Add(
                        false,
                        $"#{x.ViolationId}",
                        $"{x.ReaderName} ({x.ReaderCode})",
                        x.ViolationType,
                        $"[{x.Barcode}] {x.TitleName}",
                        $"{x.Amount:N0} đ",
                        x.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        x.Description);
                }

                RecalculateSelected();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Lỗi tải danh sách nợ phạt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnSearch.Enabled = true;
            }
        }

        private List<UnpaidViolationItem> GetCheckedItems()
        {
            var selected = new List<UnpaidViolationItem>();
            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                bool isChecked = Convert.ToBoolean(_grid.Rows[i].Cells[0].Value ?? false);
                if (isChecked && i < _items.Count)
                    selected.Add(_items[i]);
            }
            return selected;
        }

        private void RecalculateSelected()
        {
            var selected = GetCheckedItems();
            decimal sum = selected.Sum(x => x.Amount);
            bool sameReader = selected.Select(x => x.ReaderId).Distinct().Count() <= 1;

            _lblTotal.ForeColor = sameReader ? Color.DarkGreen : Color.Firebrick;
            _lblTotal.Text = sameReader
                ? $"Đã chọn: {selected.Count} khoản | Tổng thu: {sum:N0} đ   "
                : "Lỗi: Chỉ được chọn các khoản của CÙNG MỘT độc giả!   ";

            _btnCollect.Enabled = selected.Count > 0 && sameReader;
        }

        private async Task SubmitCollectAsync()
        {
            var selected = GetCheckedItems();
            if (selected.Count == 0) return;

            try
            {
                _btnCollect.Enabled = false;
                var req = new CollectFineRequest
                {
                    LibrarianAccountId = 1,
                    PaymentMethod = _cboMethod.SelectedItem?.ToString() ?? "TIEN_MAT",
                    ReferenceNote = _txtNote.Text.Trim(),
                    ViolationIds = selected.Select(x => x.ViolationId).ToList()
                };

                var receipt = await _service.CollectFinesAsync(req);

                string unlockNote = receipt.ReaderUnlocked
                    ? "\nTrạng thái thẻ: Đã tất toán toàn bộ nợ phạt → Tự động mở khóa quyền mượn."
                    : "\nTrạng thái thẻ: Độc giả vẫn còn khoản vi phạm khác chưa xử lý.";

                MessageBox.Show(this,
                    $"BIÊN LAI THU TIỀN #{receipt.ReceiptCode}\n" +
                    $"Độc giả: {receipt.ReaderName} ({receipt.ReaderCode})\n" +
                    $"Người thu: {receipt.CollectorName} | Ngày thu: {receipt.CollectedAt:dd/MM/yyyy HH:mm}\n" +
                    $"Hình thức: {receipt.PaymentMethod} | Số khoản thu: {receipt.ItemCount}\n" +
                    $"Tổng tiền đã thu: {receipt.TotalAmount:N0} đ\n" +
                    $"Căn cứ thu: {receipt.BasisSummary}" + unlockNote,
                    "Hoàn tất VP-02", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _txtNote.Clear();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không thể lập biên lai", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RecalculateSelected();
            }
        }
    }
}