using System.Text.Json;
using ThuVienDAU.Models;
using ThuVienDAU.Protocol;
namespace ThuVienDAU.Services;
public sealed class DatabaseHealthService(TcpCommandClient client)
{
    public async Task<DatabaseConnectionInfo> CheckAsync(CancellationToken cancellationToken = default)
    {
        var response = await client.SendAsync("DATABASE_HEALTH", cancellationToken);
        if (!response.Success)
            throw new DatabaseHealthCheckException(response.Error ?? "Server chưa kiểm tra được MySQL.", response.ErrorNumber);
        return response.Data?.Deserialize<DatabaseConnectionInfo>(TcpProtocol.Json)
            ?? throw new InvalidDataException("Server trả về kết quả kiểm tra MySQL rỗng.");
    }
}
