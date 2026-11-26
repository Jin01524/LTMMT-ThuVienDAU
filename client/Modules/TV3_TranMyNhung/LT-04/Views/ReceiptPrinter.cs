using System.Drawing.Printing;
using ThuVienDAU.Models;

namespace ThuVienDAU.Circulation;

public static class ReceiptPrinter
{
    public static async Task ShowAsync(IWin32Window owner, SessionInfo session, string kind, int id)
    {
        try
        {
            var receipt = await new CirculationClient(session).Call<ReceiptDocument>("TV3_RECEIPT", new ReceiptQuery(kind, id));
            using var preview = new ReceiptPreviewForm(receipt); preview.ShowDialog(owner);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.Net.Sockets.SocketException or OperationCanceledException or System.Text.Json.JsonException)
        { MessageBox.Show(owner, $"Chưa xem/in được {kind} #{id}. Nếu vừa lưu nghiệp vụ, không lập lại phiếu; vào LT-04 để in lại.\n{ex.Message}", "In phiếu", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
}

public sealed class ReceiptPreviewForm : Form
{
    public string ReceiptText { get; }
    public ReceiptPreviewForm(ReceiptDocument receipt)
    {
        Text = receipt.Title; Size = new(900, 680); StartPosition = FormStartPosition.CenterParent;
        ReceiptText = string.Join(Environment.NewLine, receipt.Lines);
        var text = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, Font = new("Segoe UI", 11), Text = ReceiptText };
        var print = new Button { Text = "Xem trước / In", Dock = DockStyle.Bottom, Height = 44 };
        print.Click += (_, _) =>
        {
            try
            {
                using var document = new PrintDocument { DocumentName = receipt.Title };
                using var font = new Font("Segoe UI", 11); int offset = 0;
                document.BeginPrint += (_, _) => offset = 0;
                document.PrintPage += (_, e) =>
                {
                    string remaining = ReceiptText[offset..];
                    var area = new SizeF(e.MarginBounds.Width, e.MarginBounds.Height);
                    var graphics = e.Graphics ?? throw new InvalidOperationException("Không có vùng vẽ máy in.");
                    graphics.MeasureString(remaining, font, area, StringFormat.GenericTypographic, out int count, out int fittedLines);
                    if (count <= 0) throw new InvalidOperationException("Khổ giấy/vùng in không đủ.");
                    graphics.DrawString(remaining[..count], font, Brushes.Black, e.MarginBounds, StringFormat.GenericTypographic);
                    offset += count; e.HasMorePages = offset < ReceiptText.Length;
                };
                using var dialog = new PrintPreviewDialog { Document = document, Width = 1050, Height = 760 }; dialog.ShowDialog(this);
            }
            catch (Exception ex) when (ex is InvalidPrinterException or System.ComponentModel.Win32Exception or InvalidOperationException)
            { MessageBox.Show(this, "Không mở được máy in. Nội dung phiếu vẫn được giữ để xem/in lại.\n" + ex.Message); }
        };
        Controls.Add(text); Controls.Add(print);
    }
}
