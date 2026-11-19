using ThuVienDAU.Data;

namespace ThuVienDAU.Services;

public sealed class DatabaseHealthService(MySqlConnectionFactory connectionFactory)
{
    public async Task<string> CheckAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT VERSION(), DATABASE();";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Không đọc được thông tin máy chủ MySQL.");
        }

        return $"Kết nối thành công.\nMySQL: {reader.GetString(0)}\nCSDL: {reader.GetString(1)}";
    }
}
