using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using ThuVienDAU.Configuration;
using ThuVienDAU.Data;
using ThuVienDAU.Models;
using ThuVienDAU.Catalog;
namespace ThuVienDAU.Server.Services;
public sealed class AuthenticationService(DatabaseOptions? options = null) : ICatalogSessionResolver
{
    private sealed record SessionEntry(SessionInfo Session,DateTimeOffset Expires);
    private readonly ConcurrentDictionary<string,SessionEntry> sessions=new();
    private MySqlConnectionFactory Factory()=>new(options??DatabaseOptions.Load());
    public bool TryGetSession(string? id,out SessionInfo session)
    {
        session=null!;
        if(id is null || id.Length!=64 || !sessions.TryGetValue(id,out var entry))return false;
        if(entry.Expires<=DateTimeOffset.UtcNow){sessions.TryRemove(id,out _);return false;}
        session=entry.Session;return true;
    }
    public int? Resolve(string? token)=>TryGetSession(token,out var session)?session.AccountId:null;
    public async Task<SessionInfo> ValidateSessionAsync(string token,CancellationToken ct)
    {
        if(!TryGetSession(token,out var session))throw new UnauthorizedAccessException("Phiên đã hết hạn hoặc đã đăng xuất.");
        await using var db=await Factory().OpenAsync(ct);using var cmd=db.CreateCommand();
        cmd.CommandText="SELECT VaiTro FROM TaiKhoan WHERE IdTaiKhoan=@id AND DangHoatDong=1";cmd.Parameters.AddWithValue("@id",session.AccountId);
        var role=await cmd.ExecuteScalarAsync(ct) as string;
        if(role is null){Logout(token);throw new UnauthorizedAccessException("Tài khoản đã khóa hoặc không còn tồn tại.");}
        return session with{Role=role};
    }
    public void Logout(string id){sessions.TryRemove(id,out _);}
    public async Task<SessionInfo> LoginAsync(LoginRequest request,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(request.Username)||request.Username.Length>100||string.IsNullOrWhiteSpace(request.Password)||request.Password.Length>256)throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không đúng.");
        await using var db=await Factory().OpenAsync(ct);using var cmd=db.CreateCommand();
        cmd.CommandText="SELECT t.IdTaiKhoan,t.IdDocGia,t.TenDangNhap,t.VaiTro,t.MatKhauDaBam,d.HoTen FROM TaiKhoan t LEFT JOIN DocGia d ON d.IdDocGia=t.IdDocGia WHERE t.TenDangNhap=@name AND t.DangHoatDong=1";
        cmd.Parameters.AddWithValue("@name",request.Username.Trim());await using var reader=await cmd.ExecuteReaderAsync(ct);
        if(!await reader.ReadAsync(ct)||reader.IsDBNull(4)||!VerifyPassword(request.Password,(byte[])reader[4]))throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không đúng.");
        foreach(var pair in sessions)if(pair.Value.Expires<=DateTimeOffset.UtcNow)sessions.TryRemove(pair.Key,out _);
        var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var session=new SessionInfo(token,reader.GetInt32(0),reader.IsDBNull(1)?null:reader.GetInt32(1),reader.GetString(2),reader.GetString(3),reader.IsDBNull(5)?reader.GetString(2):reader.GetString(5));
        sessions[token]=new(session,DateTimeOffset.UtcNow.AddMinutes(30));return session;
    }
    private async Task<SessionInfo> RequireAsync(MySqlConnection db,MySqlTransaction tx,string token,string[] roles,CancellationToken ct)
    {
        if(!TryGetSession(token,out var session))throw new UnauthorizedAccessException("Phiên đã hết hạn hoặc đã đăng xuất. Đăng nhập lại.");
        using var cmd=new MySqlCommand("SELECT VaiTro FROM TaiKhoan WHERE IdTaiKhoan=@id AND DangHoatDong=1 FOR UPDATE",db,tx);cmd.Parameters.AddWithValue("@id",session.AccountId);
        var role=await cmd.ExecuteScalarAsync(ct) as string;
        if(role is null){Logout(token);throw new UnauthorizedAccessException("Tài khoản đã bị khóa hoặc không còn tồn tại.");}
        if(!roles.Contains(role))throw new UnauthorizedAccessException("Không đủ quyền thực hiện thao tác này.");
        return session with { Role=role };
    }
    public async Task<IReadOnlyList<AccountItem>> GetAccountsAsync(string token,CancellationToken ct)
    {
        await using var db=await Factory().OpenAsync(ct);await using var tx=await db.BeginTransactionAsync(ct);await RequireAsync(db,tx,token,new[]{"QUAN_TRI"},ct);
        using var cmd=new MySqlCommand("SELECT IdTaiKhoan,TenDangNhap,VaiTro,IdDocGia,DangHoatDong FROM TaiKhoan ORDER BY IdTaiKhoan LIMIT 500",db,tx);await using var reader=await cmd.ExecuteReaderAsync(ct);var result=new List<AccountItem>();
        while(await reader.ReadAsync(ct))result.Add(new(reader.GetInt32(0),reader.GetString(1),reader.GetString(2),reader.IsDBNull(3)?null:reader.GetInt32(3),reader.GetBoolean(4)));return result;
    }
    public async Task<IReadOnlyList<ReaderAccountItem>> GetReaderAccountsAsync(string token,CancellationToken ct)
    {
        await using var db=await Factory().OpenAsync(ct);await using var tx=await db.BeginTransactionAsync(ct);await RequireAsync(db,tx,token,new[]{"BIEN_MUC"},ct);
        using var cmd=new MySqlCommand("SELECT t.IdTaiKhoan,d.IdDocGia,d.MaDinhDanhTruong,d.HoTen,t.TenDangNhap,t.DangHoatDong FROM TaiKhoan t JOIN DocGia d ON d.IdDocGia=t.IdDocGia WHERE t.VaiTro='DOC_GIA' ORDER BY d.MaDinhDanhTruong LIMIT 500",db,tx);await using var reader=await cmd.ExecuteReaderAsync(ct);var result=new List<ReaderAccountItem>();
        while(await reader.ReadAsync(ct))result.Add(new(reader.GetInt32(0),reader.GetInt32(1),reader.GetString(2),reader.GetString(3),reader.GetString(4),reader.GetBoolean(5)));return result;
    }
    public async Task<int> ProvisionAccountAsync(ProvisionAccountRequest request,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(request.Username)||request.Username.Trim().Length>100||string.IsNullOrWhiteSpace(request.Password)||request.Password.Length is <8 or >256)throw new ArgumentException("Tên đăng nhập tối đa 100 ký tự, mật khẩu từ 8–256 ký tự.");
        if(!new[]{"QUAN_TRI","QUAN_LY_THU_VIEN","BIEN_MUC","THU_THU","DOC_GIA"}.Contains(request.Role)||request.ReaderId is <=0||request.Role=="DOC_GIA"&&request.ReaderId is null)throw new ArgumentException("Vai trò/hồ sơ độc giả không hợp lệ.");
        var hash=HashPassword(request.Password);
        await using var db=await Factory().OpenAsync(ct);await using var tx=await db.BeginTransactionAsync(ct);var actor=await RequireAsync(db,tx,request.SessionId,new[]{"QUAN_TRI"},ct);
        using var cmd=new MySqlCommand("INSERT INTO TaiKhoan(TenDangNhap,MatKhauDaBam,VaiTro,DangHoatDong,IdDocGia) VALUES(@name,@hash,@role,1,@reader)",db,tx);
        cmd.Parameters.AddWithValue("@name",request.Username.Trim());cmd.Parameters.AddWithValue("@hash",hash);cmd.Parameters.AddWithValue("@role",request.Role);cmd.Parameters.AddWithValue("@reader",(object?)request.ReaderId??DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);var id=checked((int)cmd.LastInsertedId);cmd.Parameters.Clear();
        cmd.CommandText="INSERT INTO NhatKy(IdTaiKhoan,HanhDong,BangDuLieu,KhoaBanGhi,ChiTiet) VALUES(@actor,'CAP_TAI_KHOAN','TaiKhoan',@id,JSON_OBJECT('TenDangNhap',@name,'VaiTro',@role))";cmd.Parameters.AddWithValue("@actor",actor.AccountId);cmd.Parameters.AddWithValue("@id",id);cmd.Parameters.AddWithValue("@name",request.Username.Trim());cmd.Parameters.AddWithValue("@role",request.Role);await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);return id;
    }
    public async Task SetReaderAccountStatusAsync(ReaderAccountStatusRequest request,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(request.Reason)||request.Reason.Length>300)throw new ArgumentException("Lý do bắt buộc, tối đa 300 ký tự.");
        await using var db=await Factory().OpenAsync(ct);await using var tx=await db.BeginTransactionAsync(ct);var actor=await RequireAsync(db,tx,request.SessionId,new[]{"BIEN_MUC"},ct);
        using var cmd=new MySqlCommand("CALL DoiTrangThaiDangNhapDocGia(@actor,@target,@active,@reason)",db,tx);cmd.Parameters.AddWithValue("@actor",actor.AccountId);cmd.Parameters.AddWithValue("@target",request.AccountId);cmd.Parameters.AddWithValue("@active",request.IsActive);cmd.Parameters.AddWithValue("@reason",request.Reason.Trim());await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
        if(!request.IsActive)foreach(var pair in sessions)if(pair.Value.Session.AccountId==request.AccountId)sessions.TryRemove(pair.Key,out _);
    }
    public static byte[] HashPassword(string password)
    {
        var salt=RandomNumberGenerator.GetBytes(16);var derived=Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password),salt,100_000,HashAlgorithmName.SHA256,32);return new byte[]{1}.Concat(salt).Concat(derived).ToArray();
    }
    private static bool VerifyPassword(string password,byte[] hash)
    {
        if(hash.Length!=49||hash[0]!=1)return false;var derived=Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password),hash.AsSpan(1,16),100_000,HashAlgorithmName.SHA256,32);return CryptographicOperations.FixedTimeEquals(derived,hash.AsSpan(17,32));
    }
}
