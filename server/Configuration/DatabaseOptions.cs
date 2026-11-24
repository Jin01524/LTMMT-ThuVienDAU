using System.Text.Json;
using MySql.Data.MySqlClient;

namespace ThuVienDAU.Configuration;

public sealed class DatabaseOptions
{
    public string Server { get; init; } = "localhost";
    public uint Port { get; init; } = 3306;
    public string Database { get; init; } = "ThuVienDAU";
    public string User { get; init; } = "thuvien_app";
    public string Password { get; init; } = "";
    public string SslMode { get; init; } = "Preferred";

    public static DatabaseOptions Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                "Chưa có cấu hình MySQL. Sao chép appsettings.example.json thành " +
                "appsettings.local.json trong project, điền thông tin rồi build lại.");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("MySql", out var section))
        {
            throw new InvalidOperationException("Cấu hình thiếu mục MySql.");
        }

        var options = section.Deserialize<DatabaseOptions>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Cấu hình MySQL không hợp lệ.");

        if (string.IsNullOrWhiteSpace(options.Server) ||
            string.IsNullOrWhiteSpace(options.Database) ||
            string.IsNullOrWhiteSpace(options.User) || options.Port is 0 or > 65535)
        {
            throw new InvalidOperationException("Kiểm tra máy chủ, cổng, tên CSDL và tài khoản MySQL.");
        }

        if (!Enum.TryParse<MySqlSslMode>(options.SslMode, true, out var sslMode) ||
            !Enum.IsDefined(sslMode))
        {
            throw new InvalidOperationException("Giá trị SslMode không hợp lệ.");
        }

        return options;
    }

    public string BuildConnectionString()
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = Server,
            Port = Port,
            Database = Database,
            UserID = User,
            Password = Password,
            ConnectionProtocol = MySqlConnectionProtocol.Tcp,
            SslMode = Enum.Parse<MySqlSslMode>(SslMode, true),
            ConnectionTimeout = 5,
            DefaultCommandTimeout = 10,
            CharacterSet = "utf8mb4",
            PersistSecurityInfo = false
        };
        return builder.ConnectionString;
    }
}
