using System.Net.Http;
using ThuVienDAU.Controllers;
using ThuVienDAU.Forms;
using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var sessionContext = new SessionContext();
        var authenticationService = new AuthenticationService(httpClient);
        using var loginForm = new LoginForm(authenticationService, sessionContext);
        if (loginForm.ShowDialog() != DialogResult.OK || sessionContext.Current is null)
        {
            return;
        }
        var readerDirectoryService = new ReaderDirectoryService(httpClient);
        var systemAdministrationService = new SystemAdministrationClientService(httpClient);
        var readerReportService = new ReaderReportClientService(httpClient);
        using var mainForm = new MainForm(authenticationService, sessionContext, readerDirectoryService,
            systemAdministrationService, readerReportService);
        mainForm.ShowLogin(sessionContext.Current);
        _ = new MainController(mainForm, new DatabaseHealthService(httpClient));
        Application.Run(mainForm);
    }
}
