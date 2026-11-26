using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TranMyNhung_LTMMT_TV03.PH01
{
    public class FeedbackForm : Form
    {
        private readonly FeedbackService _service = new FeedbackService();

        // Khu vực 1: Gửi góp ý / đánh giá / đề xuất sách
        private readonly TextBox _txtReaderCode = new TextBox { Width = 150 };
        private readonly ComboBox _cboType = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown _numTitleId = new NumericUpDown { Width = 80, Minimum = 0, Maximum = 999999, Value = 0 };
        private readonly NumericUpDown _numRating = new NumericUpDown { Width = 60, Minimum = 1, Maximum = 5, Value = 5, Enabled = false };
        private readonly TextBox _txtSubject = new TextBox { Width = 280 };
        private readonly TextBox _txtContent = new TextBox { Width = 520 };
        private readonly Button _btnSubmit = new Button { Text = "Gửi phản hồi / Đề xuất", Width = 175, Height = 30 };

        // Khu vực 2: Danh sách & Xử lý theo quyền
        private readonly TextBox _txtSearch = new TextBox { Width = 190 };
        private readonly ComboBox _cboFilterType = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _cboFilterStatus = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Button _btnLoad = new Button { Text = "Tải danh sách", Width = 115 };

        private readonly DataGridView _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        private readonly ComboBox _cboProcessStatus = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox _txtReply = new TextBox { Width = 420 };
        private readonly Button _btnProcess = new Button { Text = "Cập nhật xử lý (Thủ thư/QL)", Width = 200, Height = 32, Enabled = false };

        private List<FeedbackItem> _items = new List<FeedbackItem>();

        public FeedbackForm()
        {
            Text = "PH-01 · Góp ý, đánh giá và đề xuất sách (TV03 - Trần Mỹ Nhung)";
            Size = new Size(1080, 640);
            StartPosition = FormStartPosition.CenterParent;

            _cboType.Items.AddRange(new object[] { "GOP_Y", "DANH_GIA", "DE_XUAT_SACH" });
            _cboType.SelectedIndex = 0;

            _cboFilterType.Items.AddRange(new object[] { "TAT_CA", "GOP_Y", "DANH_GIA", "DE_XUAT_SACH" });
            _cboFilterType.SelectedIndex = 0;

            _cboFilterStatus.Items.AddRange(new object[] { "TAT_CA", "MOI_GUI", "DA_TIEP_NHAN", "DA_PHAN_HOI", "TU_CHOI" });
            _cboFilterStatus.SelectedIndex = 0;

            _cboProcessStatus.Items.AddRange(new object[] { "DA_TIEP_NHAN", "DA_PHAN_HOI", "TU_CHOI" });
            _cboProcessStatus.SelectedIndex = 1;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Group 1: Gửi phản hồi
            var grpSend = new GroupBox { Text = "1. Gửi góp ý, đánh giá tài liệu hoặc đề xuất bổ sung sách (Lưu ý: Đề xuất sách không phải đặt trước)", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var sendLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, AutoSize = true };

            var sRow1 = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            sRow1.Controls.Add(new Label { Text = "Mã thẻ độc giả:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            sRow1.Controls.Add(_txtReaderCode);
            sRow1.Controls.Add(new Label { Text = "Loại:", AutoSize = true, Padding = new Padding(6, 6, 0, 0) });
            sRow1.Controls.Add(_cboType);
            sRow1.Controls.Add(new Label { Text = "ID Đầu sách (nếu có):", AutoSize = true, Padding = new Padding(6, 6, 0, 0) });
            sRow1.Controls.Add(_numTitleId);
            sRow1.Controls.Add(new Label { Text = "Số sao (1-5):", AutoSize = true, Padding = new Padding(6, 6, 0, 0) });
            sRow1.Controls.Add(_numRating);
            sRow1.Controls.Add(new Label { Text = "Tiêu đề / Tên sách:", AutoSize = true, Padding = new Padding(6, 6, 0, 0) });
            sRow1.Controls.Add(_txtSubject);

            var sRow2 = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
            sRow2.Controls.Add(new Label { Text = "Nội dung chi tiết:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            sRow2.Controls.Add(_txtContent);
            sRow2.Controls.Add(_btnSubmit);

            sendLayout.Controls.Add(sRow1, 0, 0);
            sendLayout.Controls.Add(sRow2, 0, 1);
            grpSend.Controls.Add(sendLayout);

            // Group 2: Danh sách & lọc
            var grpList = new GroupBox { Text = "2. Danh sách phản hồi & Đề xuất sách", Dock = DockStyle.Fill, Padding = new Padding(10) };
            var listLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            listLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var filterRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            filterRow.Controls.Add(new Label { Text = "Từ khóa:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            filterRow.Controls.Add(_txtSearch);
            filterRow.Controls.Add(new Label { Text = "Loại:", AutoSize = true, Padding = new Padding(6, 6, 0, 0) });
            filterRow.Controls.Add(_cboFilterType);
            filterRow.Controls.Add(new Label { Text = "Trạng thái:", AutoSize = true, Padding = new Padding(6, 6, 0, 0) });
            filterRow.Controls.Add(_cboFilterStatus);
            filterRow.Controls.Add(_btnLoad);

            _grid.Columns.Add("Id", "Mã");
            _grid.Columns.Add("Reader", "Độc giả");
            _grid.Columns.Add("Type", "Phân loại");
            _grid.Columns.Add("Title", "Tài liệu / Đề xuất");
            _grid.Columns.Add("Rating", "Đánh giá");
            _grid.Columns.Add("Content", "Nội dung");
            _grid.Columns.Add("Status", "Trạng thái");
            _grid.Columns.Add("Reply", "Phản hồi của thư viện");

            listLayout.Controls.Add(filterRow, 0, 0);
            listLayout.Controls.Add(_grid, 0, 1);
            grpList.Controls.Add(listLayout);

            // Group 3: Xử lý theo quyền
            var grpProcess = new GroupBox { Text = "3. Xử lý & Phản hồi (Dành cho Thủ thư / Quản lý thư viện)", Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(10) };
            var pRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            pRow.Controls.Add(new Label { Text = "Trạng thái xử lý:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            pRow.Controls.Add(_cboProcessStatus);
            pRow.Controls.Add(new Label { Text = "Nội dung phản hồi:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            pRow.Controls.Add(_txtReply);
            pRow.Controls.Add(_btnProcess);
            grpProcess.Controls.Add(pRow);

            root.Controls.Add(grpSend, 0, 0);
            root.Controls.Add(grpList, 0, 1);
            root.Controls.Add(grpProcess, 0, 2);
            Controls.Add(root);

            _cboType.SelectedIndexChanged += (s, e) =>
            {
                string t = _cboType.SelectedItem?.ToString() ?? "GOP_Y";
                _numRating.Enabled = (t == "DANH_GIA");
            };
            _btnSubmit.Click += async (s, e) => await SubmitNewFeedbackAsync();
            _btnLoad.Click += async (s, e) => await LoadDataAsync();
            _cboFilterType.SelectedIndexChanged += async (s, e) => await LoadDataAsync();
            _cboFilterStatus.SelectedIndexChanged += async (s, e) => await LoadDataAsync();
            _grid.SelectionChanged += (s, e) =>
            {
                _btnProcess.Enabled = _grid.CurrentRow != null && _grid.CurrentRow.Index >= 0 && _grid.CurrentRow.Index < _items.Count;
            };
            _btnProcess.Click += async (s, e) => await ProcessSelectedAsync();

            Load += async (s, e) => await LoadDataAsync();
        }

        private async Task SubmitNewFeedbackAsync()
        {
            try
            {
                _btnSubmit.Enabled = false;
                string type = _cboType.SelectedItem?.ToString() ?? "GOP_Y";

                var req = new SubmitFeedbackRequest
                {
                    ReaderCode = _txtReaderCode.Text,
                    FeedbackType = type,
                    TitleId = _numTitleId.Value > 0 ? (int?)_numTitleId.Value : null,
                    Rating = type == "DANH_GIA" ? (int?)_numRating.Value : null,
                    Subject = _txtSubject.Text,
                    Content = _txtContent.Text
                };

                int newId = await _service.SubmitFeedbackAsync(req);

                string extra = type == "DE_XUAT_SACH"
                    ? "\n(Lưu ý: Đề xuất bổ sung sách đã được ghi nhận cho bộ phận bổ sung, không phải phiếu đặt trước mượn sách)."
                    : "";

                MessageBox.Show(this, $"Đã gửi phản hồi #{newId} thành công!" + extra, "Hoàn tất gửi PH-01", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _txtSubject.Clear();
                _txtContent.Clear();
                _numTitleId.Value = 0;
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không thể gửi phản hồi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnSubmit.Enabled = true;
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _btnLoad.Enabled = false;
                string typeF = _cboFilterType.SelectedItem?.ToString() ?? "TAT_CA";
                string statusF = _cboFilterStatus.SelectedItem?.ToString() ?? "TAT_CA";

                _items = await _service.SearchFeedbacksAsync(_txtSearch.Text, typeF, statusF);
                _grid.Rows.Clear();

                foreach (var x in _items)
                {
                    string titleCol = x.TitleId.HasValue ? $"[#{x.TitleId.Value}] {x.TitleName} — {x.Subject}" : x.Subject;
                    string ratingCol = x.Rating.HasValue ? $"{x.Rating.Value} ★" : "—";

                    int idx = _grid.Rows.Add(
                        $"#{x.FeedbackId}",
                        $"{x.ReaderName} ({x.ReaderCode})",
                        x.FeedbackType,
                        titleCol,
                        ratingCol,
                        x.Content,
                        x.Status,
                        x.StaffReply);

                    if (x.Status == "MOI_GUI")
                        _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.DarkBlue;
                    else if (x.Status == "TU_CHOI")
                        _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.Firebrick;
                    else
                        _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.DarkGreen;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Lỗi tải phản hồi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnLoad.Enabled = true;
            }
        }

        private async Task ProcessSelectedAsync()
        {
            if (_grid.CurrentRow == null || _grid.CurrentRow.Index < 0 || _grid.CurrentRow.Index >= _items.Count) return;
            var selected = _items[_grid.CurrentRow.Index];

            try
            {
                _btnProcess.Enabled = false;
                var req = new ProcessFeedbackRequest
                {
                    FeedbackId = selected.FeedbackId,
                    StaffAccountId = 1,
                    NewStatus = _cboProcessStatus.SelectedItem?.ToString() ?? "DA_PHAN_HOI",
                    StaffReply = _txtReply.Text
                };

                string staffName = await _service.ProcessFeedbackAsync(req);
                MessageBox.Show(this, $"Cán bộ {staffName} đã cập nhật xử lý cho phản hồi #{selected.FeedbackId}!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _txtReply.Clear();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Lỗi xử lý phản hồi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _btnProcess.Enabled = true;
            }
        }
    }
}