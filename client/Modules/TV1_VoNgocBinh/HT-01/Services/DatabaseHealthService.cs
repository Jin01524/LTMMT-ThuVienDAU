using System.Net.Http;
using System.Net.Http.Json;
using ThuVienDAU.Configuration;
using ThuVienDAU.Models;

namespace ThuVienDAU.Services;

public sealed class DatabaseHealthService(HttpClient httpClient)
{
    public async Task<DatabaseConnectionInfo> CheckAsync(CancellationToken cancellationToken = default)
    {
        var options = ServerOptions.Load();
        using var response = await httpClient.GetAsync(options.GetDatabaseHealthUri(), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<DatabaseHealthError>(cancellationToken);
            throw new DatabaseHealthCheckException(
                error?.Message ?? "Server không thực hiện được yêu cầu kiểm tra MySQL.",
                error?.ErrorNumber);
        }

        return await response.Content.ReadFromJsonAsync<DatabaseConnectionInfo>(cancellationToken)
            ?? throw new InvalidOperationException("Server trả về kết quả kiểm tra MySQL rỗng.");
    }
}
