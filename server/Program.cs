using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using ThuVienDAU.Protocol;
using ThuVienDAU.Server.Controllers;
using ThuVienDAU.Services;

// One request/response per connection. Bound concurrent clients and per-request lifetime.
var host = "127.0.0.1";
var port = 5080;
for (var i = 0; i < args.Length; i += 2)
{
    if (i + 1 >= args.Length) throw new ArgumentException("Dùng --host IP --port PORT.");
    if (args[i] == "--host") host = args[i + 1];
    else if (args[i] == "--port") port = int.Parse(args[i + 1]);
    else throw new ArgumentException("Tham số không hợp lệ: " + args[i]);
}
if (!IPAddress.TryParse(host, out var address) || port is < 1 or > 65535)
    throw new ArgumentException("Host phải là địa chỉ IP, port từ 1–65535.");
using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };
var listener = new TcpListener(address, port);
using var slots = new SemaphoreSlim(32);
var pending = new ConcurrentDictionary<long, Task>();
long sequence = 0;
listener.Start();
Console.WriteLine($"ThuVienDAU TCP listening on {host}:{port}");
try
{
    while (!shutdown.IsCancellationRequested)
    {
        await slots.WaitAsync(shutdown.Token);
        TcpClient client;
        try { client = await listener.AcceptTcpClientAsync(shutdown.Token); }
        catch { slots.Release(); throw; }
        var id = ++sequence;
        pending[id] = HandleAsync(client);
        _ = pending[id].ContinueWith(completed => { pending.TryRemove(id, out _); }, TaskScheduler.Default);
    }
}
catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }
finally
{
    listener.Stop();
    shutdown.Cancel();
    await Task.WhenAll(pending.Values);
}
async Task HandleAsync(TcpClient client)
{
    using (client)
    using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token))
    {
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            var stream = client.GetStream();
            var request = await TcpProtocol.ReadAsync<TcpRequest>(stream, timeout.Token);
            var controller = new DatabaseHealthController(new DatabaseHealthService());
            var response = await controller.HandleAsync(request, timeout.Token);
            await TcpProtocol.WriteAsync(stream, response, timeout.Token);
        }
        catch (Exception exception) when (exception is IOException or SocketException or JsonException or OperationCanceledException)
        {
            Console.Error.WriteLine("Đóng kết nối TCP lỗi hoặc hết hạn: " + exception.GetType().Name);
        }
        catch (Exception exception)
        {
            // Keep one failed client from stopping the listener; details stay on server.
            Console.Error.WriteLine("Yêu cầu TCP thất bại: " + exception.GetType().Name);
        }
        finally { slots.Release(); }
    }
}
