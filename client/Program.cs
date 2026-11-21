using System.Net.Http;
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
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _ = new MainController(mainForm, new DatabaseHealthService(httpClient));
        Application.Run(mainForm);
    }
}
