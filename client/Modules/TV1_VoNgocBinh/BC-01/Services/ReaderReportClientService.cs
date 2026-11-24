using System.Net.Http.Json;
using ThuVienDAU.Configuration;
using ThuVienDAU.Models;

namespace ThuVienDAU.Services;

public sealed class ReaderReportClientService(HttpClient httpClient)
{
    public async Task<ReaderReportResult> GetAsync(
        string sessionId, string? readerType, string? faculty, string? major,
        DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            "sessionId=" + Uri.EscapeDataString(sessionId),
            "from=" + Uri.EscapeDataString(from.ToString("O")),
            "to=" + Uri.EscapeDataString(to.ToString("O"))
        };
        Add("readerType", readerType);
        Add("faculty", faculty);
        Add("major", major);
        var uri = BuildUri("api/reports/readers?" + string.Join("&", query));
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthError>(cancellationToken);
            throw new InvalidOperationException(error?.Message ?? "Không tải được báo cáo độc giả.");
        }
        return await response.Content.ReadFromJsonAsync<ReaderReportResult>(cancellationToken)
            ?? throw new InvalidOperationException("Server không trả báo cáo độc giả hợp lệ.");

        void Add(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                query.Add(name + "=" + Uri.EscapeDataString(value.Trim()));
            }
        }
    }

    private static Uri BuildUri(string path) =>
        new(new Uri(ServerOptions.Load().BaseUrl.TrimEnd('/') + "/"), path);
}
