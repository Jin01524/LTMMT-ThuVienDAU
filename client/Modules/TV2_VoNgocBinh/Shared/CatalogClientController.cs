using System.Text.Json;
using ThuVienDAU.Protocol;
using ThuVienDAU.Services;
namespace ThuVienDAU.Catalog;
public sealed class CatalogClientController
{
    private readonly TcpCommandClient transport = new();
    public async Task<T> CallAsync<T>(string command, object payload, string session) where T : class
    {
        var response = await transport.SendAsync(command,payload,session);
        if(!response.Success) throw new InvalidOperationException(response.Error ?? "Server từ chối yêu cầu.");
        return response.Data?.Deserialize<T>(TcpProtocol.Json) ?? throw new InvalidDataException("Server thiếu dữ liệu danh mục.");
    }
}
