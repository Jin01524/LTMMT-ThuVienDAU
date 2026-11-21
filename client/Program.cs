using ThuVienDAU.Controllers;
using ThuVienDAU.Forms;
using ThuVienDAU.Services;

namespace ThuVienDAU;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var mainForm = new MainForm();
        _ = new MainController(mainForm, new DatabaseHealthService(new TcpCommandClient()));
        Application.Run(mainForm);
    }
}
