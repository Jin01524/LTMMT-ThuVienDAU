using System.Text.Json;
using MySql.Data.MySqlClient;
using ThuVienDAU.Forms;
using ThuVienDAU.Services;

namespace ThuVienDAU.Controllers;

internal sealed class MainController
{
    private readonly IMainView view;
    private readonly DatabaseHealthService databaseHealthService;

    public MainController(IMainView view, DatabaseHealthService databaseHealthService)
    {
        this.view = view;
        this.databaseHealthService = databaseHealthService;
        view.ConnectionCheckRequested += CheckConnection;
    }

    private async void CheckConnection(object? sender, EventArgs e)
    {
        view.ShowConnectionCheckStarted();
        try
        {
            var connectionInfo = await databaseHealthService.CheckAsync();
            view.ShowConnectionCheckSucceeded(connectionInfo);
        }
        catch (MySqlException exception)
        {
            view.ShowConnectionCheckFailed(exception.Number);
        }
        catch (JsonException)
        {
            view.ShowConnectionConfigurationError("File appsettings.local.json không đúng định dạng JSON.");
        }
        catch (IOException)
        {
            view.ShowConnectionConfigurationError("Không đọc được file cấu hình MySQL. Kiểm tra đường dẫn và quyền truy cập.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            view.ShowConnectionConfigurationError(exception.Message);
        }
        finally
        {
            view.EnableConnectionCheck();
        }
    }
}
