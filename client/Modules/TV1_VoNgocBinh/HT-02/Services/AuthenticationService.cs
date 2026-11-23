using System.Net.Http.Json;
using ThuVienDAU.Configuration;
using ThuVienDAU.Models;

namespace ThuVienDAU.Services;

public sealed class AuthenticationService(HttpClient httpClient)
{
    public async Task<SessionInfo> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(ServerOptions.Load().GetAuthLoginUri(),
            new LoginRequest(username, password), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthError>(cancellationToken);
            throw new UnauthorizedAccessException(error?.Message ?? "Đăng nhập thất bại.");
        }
        return await response.Content.ReadFromJsonAsync<SessionInfo>(cancellationToken)
            ?? throw new InvalidOperationException("Server không trả về phiên đăng nhập.");
    }

    public async Task<IReadOnlyList<ReaderAccountItem>> GetReaderAccountsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(ServerOptions.Load().GetAuthReaderAccountsUri(), "?sessionId=" + Uri.EscapeDataString(sessionId));
        return await httpClient.GetFromJsonAsync<IReadOnlyList<ReaderAccountItem>>(uri, cancellationToken)
            ?? Array.Empty<ReaderAccountItem>();
    }

    public async Task<IReadOnlyList<AccountItem>> GetAccountsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(ServerOptions.Load().GetAuthAccountsUri(), "?sessionId=" + Uri.EscapeDataString(sessionId));
        var response = await httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthError>(cancellationToken);
            throw new UnauthorizedAccessException(error?.Message ?? "Không truy cập được danh sách tài khoản.");
        }
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<AccountItem>>(cancellationToken)
            ?? Array.Empty<AccountItem>();
    }

    public async Task ProvisionAccountAsync(ProvisionAccountRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(ServerOptions.Load().GetAuthAccountsUri(), request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthError>(cancellationToken);
            throw new InvalidOperationException(error?.Message ?? "Không cấp được tài khoản.");
        }
    }

    public async Task SetReaderAccountStatusAsync(ReaderAccountStatusRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync(ServerOptions.Load().GetAuthReaderStatusUri(), request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<AuthError>(cancellationToken);
            throw new InvalidOperationException(error?.Message ?? "Không thay đổi được trạng thái tài khoản.");
        }
    }

    public async Task LogoutAsync(string sessionId)
    {
        await httpClient.PostAsJsonAsync(ServerOptions.Load().GetAuthLogoutUri(), sessionId);
    }
}
