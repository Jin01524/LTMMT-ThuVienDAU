using MySql.Data.MySqlClient;
using ThuVienDAU.Configuration;

namespace ThuVienDAU.Data;

public sealed class MySqlConnectionFactory(DatabaseOptions options)
{
    // The caller owns the returned connection and must dispose it.
    public async Task<MySqlConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(options.BuildConnectionString());
        try
        {
            await connection.OpenAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "SET SESSION time_zone = '+07:00';";
            await command.ExecuteNonQueryAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
