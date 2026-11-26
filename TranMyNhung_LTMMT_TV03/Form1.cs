using System;
using System.Drawing;
using System.Windows.Forms;
using TranMyNhung_LTMMT_TV03.BC03;
using TranMyNhung_LTMMT_TV03.LT01;
using TranMyNhung_LTMMT_TV03.LT02;
using TranMyNhung_LTMMT_TV03.LT03;
using TranMyNhung_LTMMT_TV03.LT04;
using TranMyNhung_LTMMT_TV03.PD01;
using TranMyNhung_LTMMT_TV03.PH01;
using TranMyNhung_LTMMT_TV03.VP01;
using TranMyNhung_LTMMT_TV03.VP02;
using TranMyNhung_LTMMT_TV03.VP03;

namespace TranMyNhung_LTMMT_TV03
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Text = "TV03 · Trần Mỹ Nhung (2351220273) - Lưu thông, Vi phạm & Báo cáo";
            Size = new Size(640, 440);
            StartPosition = FormStartPosition.CenterScreen;

            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), AutoScroll = true };
            var modules = new (string Code, string Title, Action OpenAction)[]
            {
                ("LT-01", "Mượn sách tại quầy", () => new BorrowForm().ShowDialog(this)),
                ("LT-02", "Trả sách và tình trạng bản sách", () => new ReturnForm().ShowDialog(this)),
                ("LT-03", "Gia hạn trực tiếp hoặc qua cổng độc giả", () => new RenewForm().ShowDialog(this)),
                ("LT-04", "Lịch sử mượn trả và phiếu đang mở", () => new LoanHistoryForm().ShowDialog(this)),
                ("VP-01", "Quá hạn và bồi thường", () => new ViolationCalcForm().ShowDialog(this)),
                ("VP-02", "Thu tiền và biên lai", () => new FineReceiptForm().ShowDialog(this)),
                ("VP-03", "Hủy hiệu lực phiếu phạt", () => new VoidViolationForm().ShowDialog(this)),
                ("PD-01", "Lượt vào và ra phòng đọc", () => new ReadingRoomForm().ShowDialog(this)),
                ("PH-01", "Góp ý, đánh giá và đề xuất sách", () => new FeedbackForm().ShowDialog(this)),
                ("BC-03", "Thống kê lưu thông, quá hạn và thu tiền", () => new CirculationStatsForm().ShowDialog(this))
            };

            foreach (var item in modules)
            {
                var btn = new Button
                {
                    Text = $"{item.Code} · {item.Title}",
                    Width = 280,
                    Height = 46,
                    Margin = new Padding(6),
                    Enabled = item.OpenAction != null
                };
                if (item.OpenAction != null)
                {
                    var action = item.OpenAction;
                    btn.Click += (s, e) => action();
                }
                panel.Controls.Add(btn);
            }

            Controls.Add(panel);
        }
    }
}