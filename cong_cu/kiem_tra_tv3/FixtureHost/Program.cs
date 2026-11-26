using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using ThuVienDAU.Circulation;
using ThuVienDAU.Protocol;
using ThuVienDAU.Server.Controllers;
using ThuVienDAU.Server.Services;

// Test executable only. Production server has no commands to override time/calendar.
var clock = new FixtureClock();
var rules = CirculationRules.Load();
var auth = new AuthenticationService();
var authController = new AuthenticationController(auth);
var service = new CirculationService(auth, clock, rules);
var listener = new TcpListener(IPAddress.Loopback, int.Parse(args[0]));
listener.Start();
while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = Handle(client);
}
async Task Handle(TcpClient client)
{
    using (client)
    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
    {
        try
        {
            var stream = client.GetStream();
            var request = await TcpProtocol.ReadAsync<TcpRequest>(stream, timeout.Token);
            TcpResponse response;
            if (request.Command == "FIXTURE_CLOCK")
            {
                clock.Set(request.Payload!.Value.GetProperty("now").GetDateTimeOffset());
                response = new(1, request.RequestId, true, JsonSerializer.SerializeToElement(new { }, TcpProtocol.Json));
            }
            else if (request.Command == "FIXTURE_CALENDAR")
            {
                rules.CalendarConfirmed = request.Payload!.Value.GetProperty("confirmed").GetBoolean();
                rules.Holidays.Clear();
                foreach (var day in request.Payload.Value.GetProperty("holidays").EnumerateArray()) rules.Holidays.Add(DateOnly.Parse(day.GetString()!));
                response = new(1, request.RequestId, true, JsonSerializer.SerializeToElement(new { }, TcpProtocol.Json));
            }
            else response = request.Command.StartsWith("AUTH_", StringComparison.Ordinal)
                ? await authController.HandleAsync(request, timeout.Token)
                : await service.HandleAsync(request, timeout.Token);
            await TcpProtocol.WriteAsync(stream, response, timeout.Token);
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); }
    }
}
sealed class FixtureClock : TimeProvider
{
    private long ticks = DateTimeOffset.Parse("2026-11-26T09:00:00+07:00").UtcTicks;
    public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref ticks), TimeSpan.Zero);
    public void Set(DateTimeOffset value) => Interlocked.Exchange(ref ticks, value.UtcTicks);
}
