using System.Text.Json;
using MySql.Data.MySqlClient;
using ThuVienDAU.Protocol;
using ThuVienDAU.Services;
namespace ThuVienDAU.Server.Controllers;
public sealed class DatabaseHealthController(DatabaseHealthService service)
{
    public async Task<TcpResponse> HandleAsync(TcpRequest request, CancellationToken token)
    {
        if (request.Version != TcpProtocol.Version || string.IsNullOrWhiteSpace(request.RequestId) || request.RequestId.Length > 64)
            return new(1, request.RequestId, false, Error: "Phiên bản hoặc mã yêu cầu không hợp lệ.");
        if (request.Command == "PING")
            return new(1, request.RequestId, true, JsonSerializer.SerializeToElement(new { status = "ok", application = "ThuVienDAU.Server" }, TcpProtocol.Json));
        if (request.Command != "DATABASE_HEALTH")
            return new(1, request.RequestId, false, Error: "Lệnh TCP không được hỗ trợ.");
        try
        {
            return new(1, request.RequestId, true, JsonSerializer.SerializeToElement(await service.CheckAsync(token), TcpProtocol.Json));
        }
        catch (MySqlException exception)
        {
            return new(1, request.RequestId, false, Error: "Server chưa kết nối được MySQL. Kiểm tra cấu hình và quyền truy cập.", ErrorNumber: exception.Number);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or JsonException or IOException or UnauthorizedAccessException)
        {
            return new(1, request.RequestId, false, Error: "Server cần cấu hình MySQL. Kiểm tra appsettings.local.json trong server và build lại.");
        }
    }
}
