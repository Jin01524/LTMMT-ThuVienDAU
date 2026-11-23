using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using ThuVienDAU.Configuration;
using ThuVienDAU.Data;
using ThuVienDAU.Models;

namespace ThuVienDAU.Server.Services;

public sealed class AuthenticationService
{
    private readonly MySqlConnectionFactory connectionFactory;
    private readonly Dictionary<string, SessionInfo> sessions = new();

    public AuthenticationService(DatabaseOptions options)
    {
        connectionFactory = new MySqlConnectionFactory(options);
    }

    public async Task<SessionInfo> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT t.IdTaiKhoan, t.IdDocGia, t.TenDangNhap, t.VaiTro,
                   t.MatKhauDaBam, d.MaDocGia, d.HoTen
            FROM TaiKhoan t
            LEFT JOIN DocGia d ON d.IdDocGia = t.IdDocGia
            WHERE t.TenDangNhap = @username AND t.DangHoatDong = 1";
        command.Parameters.AddWithValue("@username", request.Username.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken) || reader.IsDBNull(4))
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        var hash = (byte[])reader[4];
        if (!VerifyPassword(request.Password, hash))
        {
            throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var session = new SessionInfo(
            sessionId,
            reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetInt32(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(6) ? reader.GetString(2) : reader.GetString(6));

        sessions[sessionId] = session;
        return session;
    }

    public bool TryGetSession(string? sessionId, out SessionInfo session)
    {
        session = null!;
        return !string.IsNullOrWhiteSpace(sessionId) && sessions.TryGetValue(sessionId, out session);
    }

    public void Logout(string sessionId)
    {
        sessions.Remove(sessionId);
    }

    public async Task<IReadOnlyList<ReaderAccountItem>> GetReaderAccountsAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (!TryGetSession(sessionId, out var session) || session.Role != "BIEN_MUC")
        {
            throw new UnauthorizedAccessException("Chỉ Biên mục được quản lý tài khoản độc giả.");
        }

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT t.IdTaiKhoan, d.IdDocGia, d.MaDocGia, d.HoTen, t.TenDangNhap, t.DangHoatDong
            FROM TaiKhoan t JOIN DocGia d ON d.IdDocGia = t.IdDocGia
            WHERE t.VaiTro = 'DOC_GIA'
            ORDER BY d.MaDocGia";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<ReaderAccountItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ReaderAccountItem(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetBoolean(5)));
        }
        return items;
    }

    public async Task SetReaderAccountStatusAsync(ReaderAccountStatusRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetSession(request.SessionId, out var session) || session.Role != "BIEN_MUC")
        {
            throw new UnauthorizedAccessException("Chỉ Biên mục được khóa/mở tài khoản độc giả.");
        }
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Bắt buộc nhập lý do.");
        }

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.Transaction = (MySqlTransaction)transaction;
        command.CommandText = "CALL DoiTrangThaiDangNhapDocGia(@nguoi, @taiKhoan, @hoatDong, @lyDo)";
        command.Parameters.AddWithValue("@nguoi", session.AccountId);
        command.Parameters.AddWithValue("@taiKhoan", request.AccountId);
        command.Parameters.AddWithValue("@hoatDong", request.IsActive);
        command.Parameters.AddWithValue("@lyDo", request.Reason.Trim());

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public static byte[] HashPassword(string password)
    {
        Span<byte> salt = stackalloc byte[16];
        RandomNumberGenerator.Fill(salt);
        var derived = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt.ToArray(), 100_000, HashAlgorithmName.SHA256, 32);
        var value = new byte[1 + 16 + 32];
        value[0] = 1;
        salt.CopyTo(value.AsSpan(1, 16));
        derived.CopyTo(value.AsSpan(17, 32));
        return value;
    }

    private static bool VerifyPassword(string password, byte[] hash)
    {
        if (hash.Length != 49 || hash[0] != 1)
        {
            return false;
        }
        var salt = hash.AsSpan(1, 16).ToArray();
        var derived = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(derived, hash.AsSpan(17, 32));
    }
}
