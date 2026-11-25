using System.Text.Json;
using MySql.Data.MySqlClient;
using ThuVienDAU.Configuration;
using ThuVienDAU.Data;
using ThuVienDAU.Models;
using ThuVienDAU.Server.Services;

namespace ThuVienDAU.Server.Modules.TV1_HuynhCongThu.HT;

public sealed class SystemAdministrationService(DatabaseOptions options, AuthenticationService authentication)
{
    private readonly MySqlConnectionFactory connectionFactory = new(options);

    public async Task<SystemConfigurationInfo> GetConfigurationAsync(string sessionId,CancellationToken cancellationToken)
    {
        await RequireStaff(sessionId,cancellationToken);
        var path = Path.Combine(AppContext.BaseDirectory, "Cau_hinh_du_an.json");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException("Không tìm thấy file cấu hình Cau_hinh_du_an.json cạnh server.");
        }

        var json = File.ReadAllText(path);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var pending = new List<string>();
        if (!root.TryGetProperty("DaXacNhanLichMienPhat", out var confirmed) || !confirmed.GetBoolean())
        {
            pending.Add("Lịch lễ/Tết miễn phạt chưa được xác nhận; không tự chốt tiền phạt theo ngày.");
        }
        pending.Add("Giờ mở cửa và giờ đóng cửa chưa được chốt.");
        pending.Add("Số lần và thời hạn gia hạn chưa được chốt.");
        pending.Add("Quy tắc đình chỉ cần được Quản lý thư viện xác nhận trước khi áp dụng.");
        return new SystemConfigurationInfo(json, pending);
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetAuditLogAsync(
        string sessionId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        await RequireAuditor(sessionId,cancellationToken);
        if (from >= to)
        {
            throw new ArgumentException("Ngày bắt đầu phải trước ngày kết thúc.");
        }
        if (to - from > TimeSpan.FromDays(366))
        {
            throw new ArgumentException("Mỗi lần tra cứu nhật ký tối đa 366 ngày.");
        }

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT n.IdNhatKy, n.ThoiDiem, n.IdTaiKhoan, a.TenDangNhap,
                   n.HanhDong, n.BangDuLieu, n.KhoaBanGhi, CAST(n.ChiTiet AS CHAR)
            FROM NhatKy n
            LEFT JOIN TaiKhoan a ON a.IdTaiKhoan=n.IdTaiKhoan
            WHERE n.ThoiDiem>=@from AND n.ThoiDiem<@to
            ORDER BY n.ThoiDiem DESC, n.IdNhatKy DESC
            LIMIT 500";
        command.Parameters.AddWithValue("@from", from);
        command.Parameters.AddWithValue("@to", to);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var entries = new List<AuditLogEntry>();
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new AuditLogEntry(
                reader.GetInt64(0), reader.GetDateTime(1),
                reader.IsDBNull(2) ? null : reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7)));
        }
        return entries;
    }

    private async Task<SessionInfo> RequireStaff(string sessionId,CancellationToken cancellationToken)
    {
        var session=await authentication.ValidateSessionAsync(sessionId,cancellationToken);
        if (session.Role is not ("QUAN_TRI" or "QUAN_LY_THU_VIEN" or "BIEN_MUC" or "THU_THU"))
        {
            throw new UnauthorizedAccessException("Cần đăng nhập bằng tài khoản nhân viên.");
        }
        return session;
    }

    private async Task<SessionInfo> RequireAuditor(string sessionId,CancellationToken cancellationToken)
    {
        var session=await authentication.ValidateSessionAsync(sessionId,cancellationToken);
        if (session.Role is not ("QUAN_TRI" or "QUAN_LY_THU_VIEN"))
        {
            throw new UnauthorizedAccessException("Chỉ Quản trị hoặc Quản lý thư viện được tra cứu nhật ký.");
        }
        return session;
    }
}
