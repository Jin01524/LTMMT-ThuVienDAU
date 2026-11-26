using ThuVienDAU.Models;
using ThuVienDAU.Circulation.LT01;
using ThuVienDAU.Circulation.LT02;
using ThuVienDAU.Circulation.LT03;
using ThuVienDAU.Circulation.LT04;
using ThuVienDAU.Circulation.VP01;
using ThuVienDAU.Circulation.VP02;
using ThuVienDAU.Circulation.VP03;
using ThuVienDAU.Circulation.PD01;
using ThuVienDAU.Circulation.PH01;
using ThuVienDAU.Circulation.BC03;

namespace ThuVienDAU.Circulation;

public sealed class CirculationWorkspaceForm : Form
{
    public CirculationWorkspaceForm(SessionInfo session)
    {
        Text = "TV3 · Lưu thông và dịch vụ thư viện"; Size = new(760, 750); StartPosition = FormStartPosition.CenterParent;
        var body = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new(20) };
        body.Controls.Add(new Label { AutoSize = true, Text = $"{session.DisplayName} ({session.Role})", Font = new("Segoe UI", 14, FontStyle.Bold) });
        var modules = new (string Name, Func<Form> Create)[] {
            ("LT-01 · Mượn sách tại quầy", () => new BorrowForm(session)),
            ("LT-02 · Trả sách và ghi nhận mất/hỏng", () => new ReturnForm(session)),
            ("LT-03 · Gia hạn tại quầy", () => new RenewForm(session)),
            ("LT-04 · Lịch sử, in phiếu mượn và biên lai", () => new LoanHistoryForm(session)),
            ("VP-01 · Quá hạn, bồi thường và thông báo đơn vị", () => new ViolationCalcForm(session)),
            ("VP-02 · Thu tiền và biên lai", () => new FineReceiptForm(session)),
            ("VP-03 · Hủy hiệu lực phiếu phạt", () => new VoidViolationForm(session)),
            ("PD-01 · Vào/ra phòng đọc", () => new ReadingRoomForm(session)),
            ("PH-01 · Góp ý, đánh giá, đề xuất sách", () => new FeedbackForm(session)),
            ("BC-03 · Thống kê lưu thông và thu tiền", () => new CirculationStatsForm(session))
        };
        foreach (var (name, create) in modules)
        {
            bool allowed = session.Role is "THU_THU" or "QUAN_LY_THU_VIEN" || name.StartsWith("PH-01");
            if (name.StartsWith("VP-03") && session.Role != "QUAN_LY_THU_VIEN") allowed = false;
            if (!allowed) continue;
            var button = new Button { Text = name, Width = 660, Height = 46 };
            button.Click += (_, _) => { using var form = create(); form.ShowDialog(this); }; body.Controls.Add(button);
        }
        Controls.Add(body);
    }
}
