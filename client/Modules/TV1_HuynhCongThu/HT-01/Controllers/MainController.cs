using System.Net.Sockets;
using System.Text.Json;
using ThuVienDAU.Models;
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
        catch (DatabaseHealthCheckException exception)
        {
            var message = exception.ErrorNumber is int number
                ? $"{exception.Message}\nMã MySQL: {number}"
                : exception.Message;
            view.ShowConnectionCheckFailed(message);
        }
        catch (SocketException)
        {
            view.ShowConnectionCheckFailed("Không kết nối được server. Kiểm tra Server.Host/Port, mạng và tiến trình server.");
        }
        catch (OperationCanceledException)
        {
            view.ShowConnectionCheckFailed("Server phản hồi quá thời gian chờ. Kiểm tra kết nối rồi thử lại.");
        }
        catch (IOException)
        {
            view.ShowConnectionCheckFailed("Kết nối TCP bị ngắt hoặc bản tin không hợp lệ. Kiểm tra server rồi thử lại.");
        }
        catch (JsonException)
        {
            view.ShowConnectionConfigurationError("Cấu hình client hoặc dữ liệu JSON trả về từ server không hợp lệ.");
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException)
        {
            view.ShowConnectionConfigurationError("Không đọc được file cấu hình client. Kiểm tra đường dẫn và quyền truy cập.");
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
