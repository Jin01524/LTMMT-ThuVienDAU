using System.Text.Json;

namespace ThuVienDAU.Configuration;

public sealed class ServerOptions
{
    public string BaseUrl { get; init; } = "http://localhost:5080/";

    public static ServerOptions Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
        if (!File.Exists(path))
        {
            return new ServerOptions();
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("Server", out var section))
        {
            throw new InvalidOperationException("Cấu hình client thiếu mục Server.");
        }

        var options = section.Deserialize<ServerOptions>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Cấu hình server không hợp lệ.");

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException("Server.BaseUrl phải là địa chỉ HTTP hoặc HTTPS hợp lệ.");
        }

        return options;
    }

    public Uri GetDatabaseHealthUri()
    {
        return new Uri(new Uri(BaseUrl.TrimEnd('/') + "/"), "api/database/health");
    }

    public Uri GetAuthLoginUri()
    {
        return new Uri(new Uri(BaseUrl.TrimEnd('/') + "/"), "api/auth/login");
    }

    public Uri GetAuthLogoutUri()
    {
        return new Uri(new Uri(BaseUrl.TrimEnd('/') + "/"), "api/auth/logout");
    }

    public Uri GetAuthReaderAccountsUri()
    {
        return new Uri(new Uri(BaseUrl.TrimEnd('/') + "/"), "api/auth/reader-accounts");
    }

    public Uri GetAuthReaderStatusUri()
    {
        return new Uri(new Uri(BaseUrl.TrimEnd('/') + "/"), "api/auth/reader-accounts/status");
    }
}
