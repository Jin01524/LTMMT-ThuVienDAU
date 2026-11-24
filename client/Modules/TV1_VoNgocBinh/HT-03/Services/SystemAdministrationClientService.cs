using System.Net.Http.Json;
using ThuVienDAU.Configuration;
using ThuVienDAU.Models;

namespace ThuVienDAU.Services;

public sealed class SystemAdministrationClientService(HttpClient httpClient)
{
    public async Task<SystemConfigurationInfo> GetConfigurationAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri($"api/system/configuration?sessionId={Uri.EscapeDataString(sessionId)}");
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthError>(cancellationToken);
            throw new InvalidOperationException(error?.Message ?? "Không đọc được cấu hình hệ thống.");
        }
        return await response.Content.ReadFromJsonAsync<SystemConfigurationInfo>(cancellationToken)
            ?? throw new InvalidOperationException("Server không trả cấu hình hệ thống hợp lệ.");
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetAuditLogAsync(
        string sessionId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri($"api/system/audit?sessionId={Uri.EscapeDataString(sessionId)}&from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}");
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthError>(cancellationToken);
            throw new InvalidOperationException(error?.Message ?? "Không tra cứu được nhật ký.");
        }
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<AuditLogEntry>>(cancellationToken)
            ?? throw new InvalidOperationException("Server không trả danh sách nhật ký hợp lệ.");
    }

    private static Uri BuildUri(string path) =>
        new(new Uri(ServerOptions.Load().BaseUrl.TrimEnd('/') + "/"), path);
}
