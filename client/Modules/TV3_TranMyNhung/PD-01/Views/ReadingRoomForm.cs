#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ThuVienDAU.Circulation.PD01
{
    public class ReadingRoomForm : Form
    {
        private readonly ReadingRoomService _service;

        private readonly TextBox _txtCardCode = new TextBox { Width = 210 };
        private readonly TextBox _txtLocker = new TextBox { Width = 110 };
        private readonly TextBox _txtNote = new TextBox { Width = 200 };
        private readonly Button _btnScan = new Button { Text = "Quét Vào / Ra phòng", Width = 160, Height = 32 };

        private readonly CheckBox _chkOnlyInside = new CheckBox { Text = "Chỉ hiện người đang trong phòng", AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        private readonly Button _btnRefresh = new Button { Text = "Làm mới", Width = 95 };

        private readonly DataGridView _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        private readonly Label _lblRule = new Label
        {
            AutoSize = true,
            ForeColor = Color.DarkBlue,
            Text = "Quy định phòng đọc: Giữ thẻ trường + giao chìa khóa tủ khi VÀO; tối đa 2 cuốn đọc tại chỗ/lượt; thu hồi chìa khóa + trả thẻ khi RA."
        };

        private List<ReadingRoomVisitItem> _visits = new List<ReadingRoomVisitItem>();

        public ReadingRoomForm(ThuVienDAU.Models.SessionInfo session)
        {
            _service = new ReadingRoomService(session);
            Text = "PD-01 · Lượt vào và ra phòng đọc (TV03 - Trần Mỹ Nhung)";
            Size = new Size(1000, 580);
            StartPosition = FormStartPosition.CenterParent;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var grpScan = new GroupBox { Text = "1. Quét thẻ trường ghi nhận Vào / Ra phòng đọc", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var row = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            row.Controls.Add(new Label { Text = "Mã thẻ trường:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
            row.Controls.Add(_txtCardCode);
            row.Controls.Add(new Label { Text = "Số tủ/chìa khóa:", AutoSize = true, Padding = new Padding(8, 8, 0, 0) });
            row.Controls.Add(_txtLocker);
            row.Controls.Add(new Label { Text = "Ghi chú:", AutoSize = true, Padding = new Padding(8, 8, 0, 0) });
            row.Controls.Add(_txtNote);
            row.Controls.Add(_btnScan);
            grpScan.Controls.Add(row);

            var grpList = new GroupBox { Text = "2. Nhật ký lượt vào / ra trong ngày", Dock = DockStyle.Fill, Padding = new Padding(10) };
            var listLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            listLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var filterBar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            filterBar.Controls.Add(_chkOnlyInside);
            filterBar.Controls.Add(_btnRefresh);

            _grid.Columns.Add("VisitId", "Mã lượt");
            _grid.Columns.Add("CardCode", "Mã thẻ");
            _grid.Columns.Add("Name", "Họ và tên");
            _grid.Columns.Add("Group", "Đối tượng");
            _grid.Columns.Add("CheckIn", "Giờ vào");
            _grid.Columns.Add("CheckOut", "Giờ ra");
            _grid.Columns.Add("Locker", "Chìa khóa tủ");
            _grid.Columns.Add("Status", "Trạng thái");
            _grid.Columns.Add("Note", "Ghi chú");

            listLayout.Controls.Add(filterBar, 0, 0);
            listLayout.Controls.Add(_grid, 0, 1);
            grpList.Controls.Add(listLayout);

            root.Controls.Add(grpScan, 0, 0);
            root.Controls.Add(grpList, 0, 1);
            root.Controls.Add(_lblRule, 0, 2);
            Controls.Add(root);

            _btnScan.Click += async (s, e) => await ProcessScanAsync();
            _txtCardCode.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await ProcessScanAsync(); } };
            _chkOnlyInside.CheckedChanged += async (s, e) => await LoadDataAsync();
            _btnRefresh.Click += async (s, e) => await LoadDataAsync();
            _grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < _visits.Count)
                {
                    _txtCardCode.Text = _visits[e.RowIndex].ReaderCode;
                    _txtCardCode.Focus();
                }
            };

            Load += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _btnRefresh.Enabled = false;
                _visits = await _service.GetTodayVisitsAsync("", _chkOnlyInside.Checked);
                _grid.Rows.Clear();

                foreach (var v in _visits)
                {
                    int idx = _grid.Rows.Add(
                        $"#{v.VisitId}",
                        v.ReaderCode,
                        v.ReaderName,
                        v.ReaderType,
                        v.CheckInTime.ToString("HH:mm:ss"),
                        v.CheckOutTime.HasValue ? v.CheckOutTime.Value.ToString("HH:mm:ss") : "—",
                        string.IsNullOrEmpty(v.LockerKey) ? "Không" : v.LockerKey,
                        v.IsInside ? "ĐANG TRONG PHÒNG" : "Đã ra phòng",
                        v.Note);

                    _grid.Rows[idx].DefaultCellStyle.ForeColor = v.IsInside ? Color.DarkGreen : Color.DimGray;
                }

                int insideCount = _visits.Count(x => x.IsInside);
                _lblRule.Text = $"Hôm nay: {_visits.Count} lượt | Đang có mặt trong phòng đọc: {insideCount} người | Quy định: Tối đa 2 cuốn đọc tại chỗ/lượt, giữ thẻ + giao chìa khóa khi vào.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Lỗi tải nhật ký phòng đọc", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _btnRefresh.Enabled = true;
            }
        }

        private async Task ProcessScanAsync()
        {
            try
            {
                _btnScan.Enabled = false;
                if (MessageBox.Show(this,"Xác nhận đã thực hiện giữ/trả thẻ trường và giao/nhận chìa khóa theo lượt vào/ra?", "Bàn giao tại quầy", MessageBoxButtons.YesNo,MessageBoxIcon.Question) != DialogResult.Yes) return;
                var res = await _service.ScanCardCheckInOrOutAsync(_txtCardCode.Text, _txtLocker.Text, _txtNote.Text);

                string title = res.ActionType == "CHECK_IN"
                    ? $"GHI NHẬN VÀO PHÒNG ĐỌC (#{res.VisitId})"
                    : $"GHI NHẬN RA PHÒNG ĐỌC (#{res.VisitId})";

                MessageBox.Show(this,
                    $"Độc giả: {res.ReaderName} ({res.ReaderCode}) - {res.ReaderType}\n" +
                    $"Thời điểm: {res.Timestamp:dd/MM/yyyy HH:mm:ss}\n\n" +
                    res.ReminderMessage,
                    title, MessageBoxButtons.OK, MessageBoxIcon.Information);

                _txtCardCode.Clear();
                _txtLocker.Clear();
                _txtNote.Clear();
                _txtCardCode.Focus();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không thể ghi nhận lượt vào/ra", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtCardCode.SelectAll();
                _txtCardCode.Focus();
            }
            finally
            {
                _btnScan.Enabled = true;
            }
        }
    }
}