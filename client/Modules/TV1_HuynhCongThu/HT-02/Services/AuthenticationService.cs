using System.Text.Json;
using ThuVienDAU.Models;
using ThuVienDAU.Protocol;
namespace ThuVienDAU.Services;
public sealed class AuthenticationService(TcpCommandClient transport)
{
    private async Task<T> CallAsync<T>(string command,object payload,string? token,CancellationToken ct)
    {
        var response=await transport.SendAsync(command,payload,token,ct);
        if(!response.Success)throw new UnauthorizedAccessException(response.Error??"Yêu cầu tài khoản bị từ chối.");
        return response.Data is JsonElement data ? data.Deserialize<T>(TcpProtocol.Json)??throw new InvalidDataException("Phản hồi đăng nhập rỗng.") : throw new InvalidDataException("Thiếu phản hồi đăng nhập.");
    }
    public Task<SessionInfo> LoginAsync(string username,string password,CancellationToken ct=default)=>CallAsync<SessionInfo>("AUTH_LOGIN",new LoginRequest(username,password),null,ct);
    public async Task<IReadOnlyList<ReaderAccountItem>> GetReaderAccountsAsync(string id,CancellationToken ct=default)=>await CallAsync<ReaderAccountItem[]>("AUTH_READER_ACCOUNTS",new{},id,ct);
    public async Task<IReadOnlyList<AccountItem>> GetAccountsAsync(string id,CancellationToken ct=default)=>await CallAsync<AccountItem[]>("AUTH_ACCOUNTS",new{},id,ct);
    public async Task ProvisionAccountAsync(ProvisionAccountRequest request,CancellationToken ct=default)=>_ = await CallAsync<int>("AUTH_PROVISION",request,request.SessionId,ct);
    public async Task SetReaderAccountStatusAsync(ReaderAccountStatusRequest request,CancellationToken ct=default)=>_ = await CallAsync<JsonElement>("AUTH_READER_STATUS",request,request.SessionId,ct);
    public async Task LogoutAsync(string id)=>_ = await CallAsync<JsonElement>("AUTH_LOGOUT",new{},id,CancellationToken.None);
}
