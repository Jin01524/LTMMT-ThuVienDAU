using System.Net.Sockets;
using ThuVienDAU.Configuration;
using ThuVienDAU.Protocol;
namespace ThuVienDAU.Services;
public sealed class TcpCommandClient
{
    public async Task<TcpResponse> SendAsync(string command, CancellationToken token = default)
    {
        var options = ServerOptions.Load();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        using var client = new TcpClient();
        await client.ConnectAsync(options.Host, options.Port, timeout.Token);
        var request = new TcpRequest(TcpProtocol.Version, Guid.NewGuid().ToString("N"), command);
        var stream = client.GetStream();
        await TcpProtocol.WriteAsync(stream, request, timeout.Token);
        var response = await TcpProtocol.ReadAsync<TcpResponse>(stream, timeout.Token);
        if (response.Version != TcpProtocol.Version || response.RequestId != request.RequestId)
            throw new InvalidDataException("Phản hồi TCP không khớp yêu cầu hoặc phiên bản.");
        return response;
    }
}
