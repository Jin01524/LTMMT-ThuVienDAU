using System.Net.Http.Json;
using ThuVienDAU.Configuration;
using ThuVienDAU.Models;

namespace ThuVienDAU.Services;

public sealed class ReaderDirectoryService(HttpClient httpClient)
{
    public async Task<IReadOnlyList<ReaderProfile>> SearchAsync(
        string sessionId, string query, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri($"api/readers?sessionId={Uri.EscapeDataString(sessionId)}&q={Uri.EscapeDataString(query)}");
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthError>(cancellationToken);
            throw new InvalidOperationException(error?.Message ?? "Không tra cứu được độc giả.");
        }
        var result = await response.Content.ReadFromJsonAsync<ReaderSearchResult>(cancellationToken);
        return result?.Readers ?? throw new InvalidOperationException("Server không trả danh sách độc giả hợp lệ.");
    }

    public async Task<BorrowPermissionResult> SetBorrowPermissionAsync(
        int readerId, BorrowPermissionRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            BuildUri($"api/readers/{readerId}/borrow-permission"), request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthError>(cancellationToken);
            throw new InvalidOperationException(error?.Message ?? "Không cập nhật được quyền mượn.");
        }
        return await response.Content.ReadFromJsonAsync<BorrowPermissionResult>(cancellationToken)
            ?? throw new InvalidOperationException("Server không trả kết quả cập nhật quyền mượn.");
    }

    private static Uri BuildUri(string path) =>
        new(new Uri(ServerOptions.Load().BaseUrl.TrimEnd('/') + "/"), path);
}
