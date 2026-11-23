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

    public async Task EnsureDemoAccountsAsync(CancellationToken cancellationToken)
    {
        var accounts = new[]
        {
            (Username: "quantri", Role: "QUAN_TRI"),
            (Username: "quanly", Role: "QUAN_LY_THU_VIEN"),
            (Username: "bienmuc", Role: "BIEN_MUC"),
            (Username: "thuthu", Role: "THU_THU"),
        };

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        foreach (var account in accounts)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO TaiKhoan(TenDangNhap, MatKhauDaBam, VaiTro, DangHoatDong)
                SELECT @username, @password, @role, 1
                WHERE NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = @username)";
            command.Parameters.AddWithValue("@username", account.Username);
            command.Parameters.AddWithValue("@password", HashPassword("demo123"));
            command.Parameters.AddWithValue("@role", account.Role);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
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

    public async Task<int> ProvisionAccountAsync(ProvisionAccountRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetSession(request.SessionId, out var session) || session.Role != "QUAN_TRI")
        {
            throw new UnauthorizedAccessException("Chỉ Quản trị được cấp tài khoản.");
        }
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Nhập tên đăng nhập và mật khẩu.");
        }
        if (request.Password.Length < 8)
        {
            throw new ArgumentException("Mật khẩu phải có ít nhất 8 ký tự.");
        }

        var allowedRoles = new[] { "QUAN_TRI", "QUAN_LY_THU_VIEN", "BIEN_MUC", "THU_THU", "DOC_GIA" };
        if (!allowedRoles.Contains(request.Role))
        {
            throw new ArgumentException("Vai trò không hợp lệ.");
        }
        if (request.Role == "DOC_GIA" && request.ReaderId is null)
        {
            throw new ArgumentException("Tài khoản độc giả phải gắn với hồ sơ đọc giả.");
        }

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.Transaction = (MySqlTransaction)transaction;
        command.CommandText = @"
            INSERT INTO TaiKhoan(TenDangNhap, MatKhauDaBam, VaiTro, DangHoatDong, IdDocGia)
            VALUES(@username, @password, @role, 1, @readerId)";
        command.Parameters.AddWithValue("@username", request.Username.Trim());
        command.Parameters.AddWithValue("@password", HashPassword(request.Password));
        command.Parameters.AddWithValue("@role", request.Role);
        command.Parameters.AddWithValue("@readerId", request.ReaderId);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            var accountId = await GetLastInsertIdAsync(command, cancellationToken);

            command.Parameters.Clear();
            command.CommandText = @"
                INSERT INTO NhatKy(IdTaiKhoan, HanhDong, BangDuLieu, KhoaBanGhi, ChiTiet)
                VALUES(@nguoi, 'CAP_TAI_KHOAN', 'TaiKhoan', @accountId,
                       JSON_OBJECT('TenDangNhap', @username, 'VaiTro', @role))";
            command.Parameters.AddWithValue("@nguoi", session.AccountId);
            command.Parameters.AddWithValue("@accountId", accountId);
            command.Parameters.AddWithValue("@username", request.Username.Trim());
            command.Parameters.AddWithValue("@role", request.Role);
            await command.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return accountId;
        }
        catch (MySqlException) when (request.Username.Length > 100)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ArgumentException("Tên đăng nhập không được vượt quá 100 ký tự.");
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ArgumentException("Tên đăng nhập đã tồn tại.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<AccountItem>> GetAccountsAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (!TryGetSession(sessionId, out var session) || session.Role != "QUAN_TRI")
        {
            throw new UnauthorizedAccessException("Chỉ Quản trị được xem danh sách tài khoản.");
        }
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT IdTaiKhoan, TenDangNhap, VaiTro, IdDocGia, DangHoatDong
            FROM TaiKhoan ORDER BY IdTaiKhoan";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<AccountItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AccountItem(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetInt32(3), reader.GetBoolean(4)));
        }
        return items;
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

    private static async Task<int> GetLastInsertIdAsync(MySqlCommand command, CancellationToken cancellationToken)
    {
        command.Parameters.Clear();
        command.CommandText = "SELECT LAST_INSERT_ID()";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value);
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
