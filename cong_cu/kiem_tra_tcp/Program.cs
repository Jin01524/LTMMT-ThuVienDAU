using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using ThuVienDAU.Protocol;
using ThuVienDAU.Services;
using ThuVienDAU.Models;

var root = Path.GetFullPath(args.FirstOrDefault() ?? ".");
var config = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
var previous = File.Exists(config) ? File.ReadAllBytes(config) : null;
var port = FreePort();
var psi = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
psi.ArgumentList.Add(Path.Combine(root, "server/bin/Release/net10.0/ThuVienDAU.Server.dll"));
psi.ArgumentList.Add("--port"); psi.ArgumentList.Add(port.ToString());
using var process = Process.Start(psi) ?? throw new Exception("Start server failed");
var count = 0;
try
{
    Configure(port);
    for (var i = 0; ; i++)
    {
        try { await Send("PING"); break; }
        catch (SocketException) when (i < 50) { await Task.Delay(100); }
    }
    var ping = await Send("PING"); Check(ping.Success && ping.Data?.GetProperty("status").GetString() == "ok", "real server PING");
    var health = await Send("DATABASE_HEALTH");
    Check(health.Success ? health.Data?.GetProperty("databaseName").GetString() is not null : !string.IsNullOrWhiteSpace(health.Error), "database health response");
    Check(!(await Send("UNKNOWN")).Success, "unknown command rejected");
    using (var tcp = new TcpClient())
    {
        await tcp.ConnectAsync(IPAddress.Loopback, port);
        var request = new TcpRequest(1, "fragmented", "PING");
        await Fragmented(tcp.GetStream(), request);
        var response = await TcpProtocol.ReadAsync<TcpResponse>(tcp.GetStream(), CancellationToken.None);
        Check(response.Success && response.RequestId == "fragmented", "fragmented request header/body");
        Check(await tcp.GetStream().ReadAsync(new byte[1]) == 0, "server closes after one response");
    }
    using (var tcp = new TcpClient())
    {
        await tcp.ConnectAsync(IPAddress.Loopback, port);
        await TcpProtocol.WriteAsync(tcp.GetStream(), new TcpRequest(9, "version", "PING"), CancellationToken.None);
        Check(!(await TcpProtocol.ReadAsync<TcpResponse>(tcp.GetStream(), CancellationToken.None)).Success, "unsupported version");
    }
    foreach (var bytes in new[] { new byte[] {0,0,0,0}, new byte[] {0,16,0,1}, new byte[] {0,0,0,1,123}, new byte[] {0,0} })
    {
        using var tcp = new TcpClient(); await tcp.ConnectAsync(IPAddress.Loopback, port);
        await tcp.GetStream().WriteAsync(bytes); tcp.Client.Shutdown(SocketShutdown.Send);
    }
    Check((await Send("PING")).Success, "malformed/oversized/truncated client does not stop listener");
    Check((await Task.WhenAll(Enumerable.Range(0,40).Select(_ => Send("PING")))).All(x => x.Success), "40 concurrent clients");
    await Fake(async (stream, req) => await Fragmented(stream, new TcpResponse(1, req.RequestId, true, JsonSerializer.SerializeToElement(new DatabaseConnectionInfo("8.test", "ThuVienDAU"), TcpProtocol.Json))), async () =>
    {
        var result = await new DatabaseHealthService(new TcpCommandClient()).CheckAsync();
        Check(result.DatabaseName == "ThuVienDAU", "client reads fragmented success response");
    });
    await Fake(async (stream, req) => await TcpProtocol.WriteAsync(stream, new TcpResponse(1, req.RequestId, false, Error: "Lỗi thử", ErrorNumber: 1045), CancellationToken.None), async () =>
    {
        try { await new DatabaseHealthService(new TcpCommandClient()).CheckAsync(); throw new Exception("Expected database error"); }
        catch (DatabaseHealthCheckException ex) { Check(ex.ErrorNumber == 1045, "typed database error"); }
    });
    await Fake(async (stream, req) =>
    {
        Check(req.Payload?.GetProperty("text").GetString()=="tv2" && req.SessionToken=="synthetic-session", "optional TV2 payload/session transported");
        await TcpProtocol.WriteAsync(stream,new TcpResponse(1,req.RequestId,true),CancellationToken.None);
    },async()=> { await new TcpCommandClient().SendAsync("TV2_TITLE_LIST",new { text="tv2" },"synthetic-session"); });
    await Expect<InvalidDataException>(async (s, q) => await TcpProtocol.WriteAsync(s, new TcpResponse(1, "wrong-id", true), CancellationToken.None), "request ID mismatch");
    await Expect<EndOfStreamException>((s, q) => Task.CompletedTask, "disconnect mid-response");
    await Expect<InvalidDataException>(async (s, q) => await s.WriteAsync(new byte[] {0,16,0,1}), "oversized response");
    await Expect<JsonException>(async (s, q) => await s.WriteAsync(new byte[] {0,0,0,4,110,117,108,108}), "null JSON response");
    await Expect<OperationCanceledException>(async (s, q) => await Task.Delay(1500), "client timeout");
    File.WriteAllText(config, "{bad-json");
    try { await Send("PING"); throw new Exception("Expected invalid config"); } catch(JsonException) { Check(true, "invalid configuration"); }
    File.WriteAllText(config, "{\"Server\":{\"BaseUrl\":\"http://localhost:5080\"}}");
    try { await Send("PING"); throw new Exception("Expected legacy config rejection"); } catch(InvalidOperationException) { Check(true, "legacy HTTP configuration rejected"); }
    Configure(FreePort());
    try { await Send("PING"); throw new Exception("Expected refused connection"); } catch(Exception ex) when (ex is SocketException or OperationCanceledException) { Check(true, "unavailable server (refused or connect timeout)"); }
    Console.WriteLine($"PASS: {count} TCP integration checks; real server and actual client transport/service. UI and LAN not tested.");
}
finally
{
    if (!process.HasExited) { process.Kill(); await process.WaitForExitAsync(); }
    if (previous is null) File.Delete(config); else File.WriteAllBytes(config, previous);
}
void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL " + message); count++; Console.WriteLine("PASS " + message); }
void Configure(int target, int timeout = 5) => File.WriteAllText(config, JsonSerializer.Serialize(new { Server = new { Host = "127.0.0.1", Port = target, TimeoutSeconds = timeout } }));
Task<TcpResponse> Send(string command) => new TcpCommandClient().SendAsync(command);
static int FreePort() { var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var p = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return p; }
async Task Fake(Func<NetworkStream,TcpRequest,Task> serve, Func<Task> action)
{
    var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); Configure(((IPEndPoint)listener.LocalEndpoint).Port, 1);
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var server = Task.Run(async () =>
    {
        using var peer = await listener.AcceptTcpClientAsync(deadline.Token);
        var stream = peer.GetStream(); var request = await TcpProtocol.ReadAsync<TcpRequest>(stream, deadline.Token);
        await serve(stream, request);
    });
    try { await action(); await server; }
    finally { deadline.Cancel(); listener.Stop(); try { await server; } catch (OperationCanceledException) { } }
}
Task Expect<T>(Func<NetworkStream,TcpRequest,Task> serve, string label) where T : Exception => Fake(serve, async () =>
{
    try { await Send("PING"); throw new Exception("Expected " + typeof(T).Name); }
    catch (T) { Check(true, label); }
});
static async Task Fragmented<T>(Stream stream, T value)
{
    var data = JsonSerializer.SerializeToUtf8Bytes(value, TcpProtocol.Json); var header = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(header, data.Length);
    foreach (var b in header) await stream.WriteAsync(new byte[] { b });
    for (var i=0;i<data.Length;i+=3) await stream.WriteAsync(data.AsMemory(i, Math.Min(3,data.Length-i)));
}
