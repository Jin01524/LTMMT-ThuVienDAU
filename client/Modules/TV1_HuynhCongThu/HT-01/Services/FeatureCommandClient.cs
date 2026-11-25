using System.Text.Json;
using ThuVienDAU.Protocol;
namespace ThuVienDAU.Services;
internal sealed class FeatureCommandClient(TcpCommandClient transport)
{
    internal async Task<T> CallAsync<T>(string command, object payload, string sessionId, CancellationToken ct) where T : class
    {
        var response = await transport.SendAsync(command, payload, sessionId, ct);
        if (!response.Success) throw new InvalidOperationException(response.Error ?? "Server từ chối thao tác.");
        return response.Data?.Deserialize<T>(TcpProtocol.Json) ?? throw new InvalidDataException("Server thiếu dữ liệu phản hồi.");
    }
}
