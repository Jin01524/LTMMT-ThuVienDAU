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
        var transport=new TcpCommandClient();var sessions=new SessionContext();var authentication=new AuthenticationService(transport);
        using var login=new LoginForm(authentication,sessions);
        if(login.ShowDialog()!=DialogResult.OK||sessions.Current is null)return;
        using var main=new MainForm(authentication,sessions,new ReaderDirectoryService(transport),new SystemAdministrationClientService(transport),new ReaderReportClientService(transport),new ReaderImportClientService(transport),new DatabaseBackupClientService(transport));main.ShowLogin(sessions.Current);
        _ = new MainController(main,new DatabaseHealthService(transport));Application.Run(main);
    }
}
