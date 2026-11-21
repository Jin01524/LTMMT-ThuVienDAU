using ThuVienDAU.Configuration;
using ThuVienDAU.Data;
using ThuVienDAU.Models;

namespace ThuVienDAU.Services;

public sealed class DatabaseHealthService
{
    public async Task<DatabaseConnectionInfo> CheckAsync(CancellationToken cancellationToken = default)
    {
        var connectionFactory = new MySqlConnectionFactory(DatabaseOptions.Load());
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT VERSION(), DATABASE();";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Không đọc được thông tin máy chủ MySQL.");
        }

        return new DatabaseConnectionInfo(reader.GetString(0), reader.GetString(1));
    }
}
