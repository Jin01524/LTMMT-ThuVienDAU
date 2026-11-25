using MySql.Data.MySqlClient;
using ThuVienDAU.Configuration;
using ThuVienDAU.Data;
using ThuVienDAU.Models;
using ThuVienDAU.Server.Services;

namespace ThuVienDAU.Server.Modules.TV1_HuynhCongThu.BC;

public sealed class ReaderReportService(DatabaseOptions options, AuthenticationService authentication)
{
    private const int MaxReaderRows = 5000;
    private readonly MySqlConnectionFactory connectionFactory = new(options);

    public async Task<ReaderReportResult> GetAsync(
        string sessionId, string? readerType, string? faculty, string? major,
        DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        await RequireReporter(sessionId,cancellationToken);
        if (from >= to)
        {
            throw new ArgumentException("Ngày bắt đầu phải trước ngày kết thúc.");
        }
        if (to - from > TimeSpan.FromDays(366))
        {
            throw new ArgumentException("Mỗi lần xem nhật ký tối đa 366 ngày.");
        }

        readerType = NormalizeFilter(readerType);
        faculty = NormalizeFilter(faculty);
        major = NormalizeFilter(major);
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        const string joins = @"
            FROM DocGia d
            LEFT JOIN Khoa k ON k.IdKhoa=d.IdKhoa
            LEFT JOIN NganhDaoTao n ON n.MaNganh=d.MaNganh
            WHERE (@readerType IS NULL OR d.LoaiDocGia=@readerType)
              AND (@faculty IS NULL OR k.TenKhoa=@faculty)
              AND (@major IS NULL OR n.TenNganh=@major)";

        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = $"SELECT COUNT(*), SUM(d.TrangThaiHoSo='HOAT_DONG'), SUM(d.DuocMuon=1) {joins}";
        AddFilters(countCommand, readerType, faculty, major);
        await using var countReader = await countCommand.ExecuteReaderAsync(cancellationToken);
        await countReader.ReadAsync(cancellationToken);
        var total = countReader.GetInt32(0);
        var active = countReader.IsDBNull(1) ? 0 : countReader.GetInt32(1);
        var canBorrow = countReader.IsDBNull(2) ? 0 : countReader.GetInt32(2);
        await countReader.CloseAsync();

        var byType = await GetGroupsAsync(connection, "Loại độc giả", "d.LoaiDocGia", "d.LoaiDocGia", joins,
            readerType, faculty, major, cancellationToken);
        var byFaculty = await GetGroupsAsync(connection, "Khoa", "COALESCE(k.TenKhoa,'Chưa phân khoa')", "k.TenKhoa", joins,
            readerType, faculty, major, cancellationToken);
        var byMajor = await GetGroupsAsync(connection, "Ngành", "COALESCE(n.TenNganh,'Chưa phân ngành')", "n.TenNganh", joins,
            readerType, faculty, major, cancellationToken);

        using var readersCommand = connection.CreateCommand();
        readersCommand.CommandText = $@"
            SELECT d.IdDocGia, d.MaDinhDanhTruong, d.HoTen, d.LoaiDocGia,
                   k.TenKhoa, n.TenNganh, d.TrangThaiHoSo, d.DuocMuon,
                   d.KhoaDen, d.LyDoKhoa,
                   (SELECT COUNT(*) FROM ChiTietPhieuMuon c
                    WHERE c.IdDocGia=d.IdDocGia AND c.TrangThai='DANG_MUON'),
                   (SELECT COUNT(*) FROM ViPham v
                    WHERE v.IdDocGia=d.IdDocGia AND v.TrangThai='CHUA_XU_LY')
            {joins}
            ORDER BY d.HoTen, d.MaDinhDanhTruong
            LIMIT {MaxReaderRows + 1}";
        AddFilters(readersCommand, readerType, faculty, major);
        await using var reader = await readersCommand.ExecuteReaderAsync(cancellationToken);
        var readers = new List<ReaderProfile>();
        while (await reader.ReadAsync(cancellationToken))
        {
            readers.Add(new ReaderProfile(
                reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetString(6), reader.GetBoolean(7),
                reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.GetInt32(10), reader.GetInt32(11)));
        }
        var truncated = readers.Count > MaxReaderRows;
        if (truncated) readers.RemoveAt(readers.Count - 1);
        await reader.CloseAsync();

        var activity = await GetActivityAsync(connection, from, to, cancellationToken);
        return new ReaderReportResult(total, active, canBorrow, truncated,
            byType, byFaculty, byMajor, readers, activity);
    }

    private static async Task<IReadOnlyList<ReaderGroupCount>> GetGroupsAsync(
        MySqlConnection connection, string groupName, string selectExpression, string groupExpression, string joins,
        string? readerType, string? faculty, string? major, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {selectExpression}, COUNT(*) {joins} GROUP BY {groupExpression} ORDER BY 1";
        AddFilters(command, readerType, faculty, major);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var groups = new List<ReaderGroupCount>();
        while (await reader.ReadAsync(cancellationToken))
        {
            groups.Add(new ReaderGroupCount(groupName, reader.GetString(0), reader.GetInt32(1)));
        }
        return groups;
    }

    private static async Task<IReadOnlyList<AuditLogEntry>> GetActivityAsync(
        MySqlConnection connection, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT n.IdNhatKy,n.ThoiDiem,n.IdTaiKhoan,a.TenDangNhap,n.HanhDong,
                   n.BangDuLieu,n.KhoaBanGhi,CAST(n.ChiTiet AS CHAR)
            FROM NhatKy n LEFT JOIN TaiKhoan a ON a.IdTaiKhoan=n.IdTaiKhoan
            WHERE n.BangDuLieu='DocGia' AND n.ThoiDiem>=@from AND n.ThoiDiem<@to
            ORDER BY n.ThoiDiem DESC,n.IdNhatKy DESC LIMIT 500";
        command.Parameters.AddWithValue("@from", from);
        command.Parameters.AddWithValue("@to", to);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var entries = new List<AuditLogEntry>();
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new AuditLogEntry(reader.GetInt64(0), reader.GetDateTime(1),
                reader.IsDBNull(2) ? null : reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetString(4),
                reader.GetString(5), reader.GetString(6), reader.GetString(7)));
        }
        return entries;
    }

    private async Task<SessionInfo> RequireReporter(string sessionId,CancellationToken cancellationToken)
    {
        var session=await authentication.ValidateSessionAsync(sessionId,cancellationToken);
        if (session.Role is not ("QUAN_TRI" or "QUAN_LY_THU_VIEN"))
        {
            throw new UnauthorizedAccessException("Chỉ Quản trị hoặc Quản lý thư viện được xem báo cáo độc giả.");
        }
        return session;
    }

    private static string? NormalizeFilter(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddFilters(MySqlCommand command, string? readerType, string? faculty, string? major)
    {
        command.Parameters.AddWithValue("@readerType", (object?)readerType ?? DBNull.Value);
        command.Parameters.AddWithValue("@faculty", (object?)faculty ?? DBNull.Value);
        command.Parameters.AddWithValue("@major", (object?)major ?? DBNull.Value);
    }
}
