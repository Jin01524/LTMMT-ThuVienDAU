#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThuVienDAU.Circulation.LT01
{
    public class BorrowForm : Form
    {
        private readonly BorrowService _service;

        private readonly TextBox _txtReaderCode = new TextBox { Width = 240 };
        private readonly Button _btnFindReader = new Button { Text = "Tìm độc giả", Width = 110 };
        private readonly Label _lblReaderInfo = new Label { AutoSize = true, Text = "Chưa chọn độc giả." };

        private readonly TextBox _txtBarcode = new TextBox { Width = 240, Enabled = false };
        private readonly Button _btnAddCopy = new Button { Text = "Thêm sách", Width = 110, Enabled = false };
        private readonly Button _btnRemoveCopy = new Button { Text = "Xóa dòng chọn", Width = 120, Enabled = false };

        private readonly DataGridView _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        private readonly Button _btnSubmit = new Button { Text = "Xác nhận lập phiếu mượn", Width = 190, Height = 36, Enabled = false };
        private readonly Button _btnReset = new Button { Text = "Làm mới", Width = 100, Height = 36 };

        private BorrowReaderLookup _currentReader;
        private readonly List<BorrowCopyLookup> _cart = new List<BorrowCopyLookup>();

        public BorrowForm(ThuVienDAU.Models.SessionInfo session)
        {
            _service = new BorrowService(session);
            Text = "LT-01 · Mượn sách tại quầy (TV03 - Trần Mỹ Nhung)";
            Size = new Size(880, 560);
            StartPosition = FormStartPosition.CenterParent;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var grpReader = new GroupBox { Text = "1. Định danh độc giả bằng thẻ trường", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var readerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, AutoSize = true };
            var readerSearchRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            readerSearchRow.Controls.Add(new Label { Text = "Mã thẻ:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            readerSearchRow.Controls.Add(_txtReaderCode);
            readerSearchRow.Controls.Add(_btnFindReader);
            readerLayout.Controls.Add(readerSearchRow, 0, 0);
            readerLayout.Controls.Add(_lblReaderInfo, 0, 1);
            grpReader.Controls.Add(readerLayout);

            var grpCopy = new GroupBox { Text = "2. Quét mã vạch bản sách", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var copyRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            copyRow.Controls.Add(new Label { Text = "Barcode / ĐKCB:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            copyRow.Controls.Add(_txtBarcode);
            copyRow.Controls.Add(_btnAddCopy);
            copyRow.Controls.Add(_btnRemoveCopy);
            grpCopy.Controls.Add(copyRow);

            _grid.Columns.Add("Barcode", "Mã vạch");
            _grid.Columns.Add("RegNo", "Số ĐKCB");
            _grid.Columns.Add("TitleName", "Nhan đề tài liệu");
            _grid.Columns.Add("Condition", "Tình trạng");
            _grid.Columns.Add("Location", "Vị trí kệ");

            var bottomBar = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
            bottomBar.Controls.Add(_btnSubmit);
            bottomBar.Controls.Add(_btnReset);

            root.Controls.Add(grpReader, 0, 0);
            root.Controls.Add(grpCopy, 0, 1);
            root.Controls.Add(_grid, 0, 2);
            root.Controls.Add(bottomBar, 0, 3);
            Controls.Add(root);

            _btnFindReader.Click += async (s, e) => await FindReaderAsync();
            _txtReaderCode.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await FindReaderAsync(); } };
            _btnAddCopy.Click += async (s, e) => await AddCopyAsync();
            _txtBarcode.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await AddCopyAsync(); } };
            _btnRemoveCopy.Click += (s, e) =>
            {
                if (_grid.CurrentRow != null && _grid.CurrentRow.Index >= 0 && _grid.CurrentRow.Index < _cart.Count)
                {
                    _cart.RemoveAt(_grid.CurrentRow.Index);
                    RefreshGrid();
                }
            };
            _btnReset.Click += (s, e) => ResetForm();
            _btnSubmit.Click += async (s, e) => await SubmitBorrowAsync();
        }

        private async Task FindReaderAsync()
        {
            try
            {
                _btnFindReader.Enabled = false;
                var reader = await _service.LookupReaderAsync(_txtReaderCode.Text);
                _currentReader = reader;
                _cart.Clear();
                RefreshGrid();

                bool eligible = reader.CanBorrow && reader.ProfileStatus == "HOAT_DONG" && reader.UnpaidFineTotal <= 0 && reader.OverdueCount == 0 && reader.ActiveBorrowCount < reader.MaxBorrowLimit;
                _lblReaderInfo.ForeColor = eligible ? Color.DarkGreen : Color.Firebrick;
                _lblReaderInfo.Text = $"Họ tên: {reader.FullName} ({reader.ReaderCode}) | Nhóm: {reader.ReaderType} | Hạn: {reader.LoanDays} ngày | Đang mượn: {reader.ActiveBorrowCount}/{reader.MaxBorrowLimit} | Nợ phạt: {reader.UnpaidFineTotal:N0} đ";

                _txtBarcode.Enabled = eligible;
                _btnAddCopy.Enabled = eligible;
                if (eligible) _txtBarcode.Focus();
                else MessageBox.Show(this, "Độc giả chưa đủ điều kiện mượn sách.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnFindReader.Enabled = true;
            }
        }

        private async Task AddCopyAsync()
        {
            if (_currentReader == null) return;
            try
            {
                _btnAddCopy.Enabled = false;
                var copy = await _service.LookupCopyAsync(_txtBarcode.Text);
                if (copy.Status != "SAN_SANG") throw new InvalidOperationException($"Sách không sẵn sàng (Trạng thái: {copy.Status}).");
                if (_cart.Any(x => x.CopyId == copy.CopyId)) throw new InvalidOperationException("Bản sách này đã có trong danh sách.");
                if (_currentReader.ActiveTitleIds.Contains(copy.TitleId) || _cart.Any(x => x.TitleId == copy.TitleId))
                    throw new InvalidOperationException($"Chặn trùng nhan đề: '{copy.TitleName}'.");
                if (_currentReader.ActiveBorrowCount + _cart.Count + 1 > _currentReader.MaxBorrowLimit)
                    throw new InvalidOperationException($"Vượt hạn mức tối đa {_currentReader.MaxBorrowLimit} đầu sách.");

                _cart.Add(copy);
                _txtBarcode.Clear();
                _txtBarcode.Focus();
                RefreshGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không thể thêm sách", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnAddCopy.Enabled = true;
            }
        }

        private async Task SubmitBorrowAsync()
        {
            if (_currentReader == null || _cart.Count == 0) return;
            try
            {
                _btnSubmit.Enabled = false;
                var receipt = await _service.CreateBorrowAsync(_currentReader.ReaderId, _cart.Select(x => x.CopyId).ToList());
                await _service.PreviewAsync(this,"LOAN",receipt.LoanId);
                MessageBox.Show(this, $"Lập phiếu mượn #{receipt.LoanId} thành công!\nHạn trả: {receipt.Items.First().DueDate:dd/MM/yyyy HH:mm}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _btnSubmit.Enabled = true;
            }
        }

        private void RefreshGrid()
        {
            _grid.Rows.Clear();
            foreach (var item in _cart)
                _grid.Rows.Add(item.Barcode, item.RegistrationNumber, item.TitleName, item.Condition, item.LocationText);

            _btnRemoveCopy.Enabled = _cart.Count > 0;
            _btnSubmit.Enabled = _currentReader != null && _cart.Count > 0;
        }

        private void ResetForm()
        {
            _currentReader = null;
            _cart.Clear();
            _txtReaderCode.Clear();
            _txtBarcode.Clear();
            _txtBarcode.Enabled = false;
            _btnAddCopy.Enabled = false;
            _lblReaderInfo.ForeColor = SystemColors.ControlText;
            _lblReaderInfo.Text = "Chưa chọn độc giả.";
            RefreshGrid();
        }
    }
}