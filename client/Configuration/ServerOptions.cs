using System.Text.Json;
namespace ThuVienDAU.Configuration;
public sealed class ServerOptions
{
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 5080;
    public int TimeoutSeconds { get; init; } = 15;
    public static ServerOptions Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
        if (!File.Exists(path)) return new();
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("Server", out var section))
            throw new InvalidOperationException("Cấu hình client thiếu mục Server.");
        if (section.TryGetProperty("BaseUrl", out _))
            throw new InvalidOperationException("Đổi cấu hình HTTP cũ: thay BaseUrl bằng Host, Port và TimeoutSeconds cho TCP.");
        var options = section.Deserialize<ServerOptions>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Cấu hình TCP không hợp lệ.");
        if (string.IsNullOrWhiteSpace(options.Host) || options.Host.Contains("://") || options.Port is < 1 or > 65535 || options.TimeoutSeconds is < 1 or > 120)
            throw new InvalidOperationException("Kiểm tra Server.Host, Port (1–65535), TimeoutSeconds (1–120). Host không chứa http://.");
        return options;
    }
}
