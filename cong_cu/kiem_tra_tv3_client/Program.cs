using System.ComponentModel;
using System.Reflection;
using ThuVienDAU.Circulation;
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
using ThuVienDAU.Models;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var output = Path.Combine(root, "kiem_tra/anh_tv3"); Directory.CreateDirectory(output);
        var logs = new List<string>();
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); logs.Add("PASS " + message); }
        var manager = new SessionInfo("fixture-session", 2, null, "fixture", "QUAN_LY_THU_VIEN", "Quản lý thử");
        using (var readerWorkspace = new CirculationWorkspaceForm(manager with { Role = "DOC_GIA", ReaderId = 1 }))
            Check(AllControls(readerWorkspace).OfType<Button>().Count() == 1, "reader workspace exposes feedback only");
        using (var librarianWorkspace = new CirculationWorkspaceForm(manager with { Role = "THU_THU" }))
            Check(!AllControls(librarianWorkspace).OfType<Button>().Any(x => x.Text.StartsWith("VP-03")), "librarian workspace hides manager-only void");
        var forms = new Form[] { new CirculationWorkspaceForm(manager), new BorrowForm(manager), new ReturnForm(manager), new RenewForm(manager), new LoanHistoryForm(manager), new ViolationCalcForm(manager), new FineReceiptForm(manager), new VoidViolationForm(manager), new ReadingRoomForm(manager), new FeedbackForm(manager), new CirculationStatsForm(manager), new ReceiptPreviewForm(new("Phiếu mượn", new[] { "THƯ VIỆN ĐẠI HỌC KIẾN TRÚC ĐÀ NẴNG", "PHIẾU MƯỢN #123", "Bạn đọc: Võ Ngọc Bình", "Sách: Cơ sở dữ liệu", "Hạn trả: 06/12/2026" })) };
        foreach (var form in forms)
        using (form)
        {
            // Disable startup data loading only. This test renders actual controls without pretending to test clicks or a physical printer.
            var events = (EventHandlerList)typeof(Component).GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
            foreach (var field in typeof(Form).GetFields(BindingFlags.Static | BindingFlags.NonPublic).Where(f => f.Name.Contains("shown", StringComparison.OrdinalIgnoreCase) || f.Name.Contains("load", StringComparison.OrdinalIgnoreCase)))
            {
                var key = field.GetValue(null); if (key is not null && events[key] is Delegate handler) events.RemoveHandler(key, handler);
            }
            form.StartPosition = FormStartPosition.Manual; form.Location = new(-20000, -20000); form.Show(); Application.DoEvents(); form.PerformLayout();
            Check(form.IsHandleCreated, "construct native controls " + form.GetType().Name);
            if (form is CirculationWorkspaceForm) Check(AllControls(form).OfType<Button>().Count() == 10, "manager has all 10 TV3 modules");
            if (form is VoidViolationForm) Check(!AllControls(form).OfType<NumericUpDown>().Any(x => x.Visible), "void screen has no editable manager ID");
            if (form is CirculationStatsForm) Check(!AllControls(form).OfType<NumericUpDown>().Any(x => x.Visible), "statistics uses logged-in viewer identity");
            if (form is LoanHistoryForm) Check(AllControls(form).OfType<Button>().Any(x => x.Text.Contains("In lại")), "LT04 supports reprinting");
            if (form is ReceiptPreviewForm receipt) Check(receipt.ReceiptText.Contains("Võ Ngọc Bình") && AllControls(form).OfType<Button>().Any(x => x.Text.Contains("In")), "print preview preserves Vietnamese and exposes actual print action");
            using var bitmap = new Bitmap(form.Width, form.Height); form.DrawToBitmap(bitmap, new(0, 0, form.Width, form.Height)); bitmap.Save(Path.Combine(output, form.GetType().Name + ".png")); form.Hide();
        }
        File.WriteAllLines(Path.Combine(root, "kiem_tra/TV3_Client_ket_qua.txt"), logs); Console.WriteLine(string.Join(Environment.NewLine, logs));
    }
    private static IEnumerable<Control> AllControls(Control root)
    {
        foreach (Control child in root.Controls) { yield return child; foreach (var nested in AllControls(child)) yield return nested; }
    }
}
