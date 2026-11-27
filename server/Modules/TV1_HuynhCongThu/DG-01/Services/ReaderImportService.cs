using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using ThuVienDAU.Configuration;
using ThuVienDAU.Data;
using ThuVienDAU.Models;
using ThuVienDAU.Server.Services;

namespace ThuVienDAU.Server.Modules.TV1_HuynhCongThu.DG;

public sealed class ReaderImportService(DatabaseOptions options, AuthenticationService authentication)
{
    private const int MaxRows = 500;
    private readonly MySqlConnectionFactory connectionFactory = new(options);
    private static readonly HashSet<string> ReaderTypes = new(StringComparer.Ordinal)
    {
        "SINH_VIEN", "CAN_BO", "GIANG_VIEN", "TRO_GIANG", "CNV"
    };

    public Task<ReaderImportResult> PreviewAsync(string sessionToken, ReaderImportCommand request, CancellationToken ct)
        => ProcessAsync(sessionToken, request, false, ct);

    public Task<ReaderImportResult> CommitAsync(string sessionToken, ReaderImportCommand request, CancellationToken ct)
        => ProcessAsync(sessionToken, request, true, ct);

    private async Task<ReaderImportResult> ProcessAsync(
        string sessionToken, ReaderImportCommand request, bool commit, CancellationToken ct)
    {
        var actor = await authentication.ValidateSessionAsync(sessionToken, ct);
        if (actor.Role != "QUAN_LY_THU_VIEN")
            throw new UnauthorizedAccessException("Chỉ Quản lý thư viện được nhập hồ sơ độc giả.");
        if (!Guid.TryParseExact(request.BatchId, "N", out _))
            throw new ArgumentException("Mã lô nhập không hợp lệ.");
        if (request.Rows is null || request.Rows.Length is < 1 or > MaxRows)
            throw new ArgumentException($"Mỗi lần nhập cần từ 1 đến {MaxRows} dòng dữ liệu.");
        if (request.Rows.Any(row => row is null))
            throw new ArgumentException("Danh sách có dòng dữ liệu rỗng.");

        var rows = request.Rows;
        var duplicates = rows
            .Select((row, index) => (Code: SchoolCode(row), Index: index))
            .Where(item => item.Code.Length > 0)
            .GroupBy(item => item.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Select(item => item.Index))
            .ToHashSet();

        await using var connection = await connectionFactory.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            if (commit)
            {
                using var duplicateBatch = new MySqlCommand(
                    "SELECT COUNT(*) FROM NhatKy WHERE HanhDong='NHAP_DU_LIEU_DOC_GIA' AND KhoaBanGhi=@batchId",
                    connection, (MySqlTransaction)transaction);
                duplicateBatch.Parameters.AddWithValue("@batchId", request.BatchId);
                if (Convert.ToInt32(await duplicateBatch.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture) > 0)
                    throw new InvalidOperationException("Lô nhập này đã được xử lý; không gửi lại để tránh ghi trùng.");
            }

            var results = new ReaderImportRowResult[rows.Length];
            var prepared = new PreparedRow?[rows.Length];
            for (var index = 0; index < rows.Length; index++)
            {
                var row = rows[index];
                var lineNumber = row.RowNumber > 0 ? row.RowNumber : index + 2;
                if (duplicates.Contains(index))
                {
                    results[index] = new(lineNumber, SchoolCode(row), "", "Lỗi: mã trường trùng trong file.");
                    continue;
                }
                try
                {
                    prepared[index] = await ValidateRowAsync(connection, (MySqlTransaction)transaction, row, ct);
                    var action = prepared[index]!.ExistingId is null ? "THÊM_MỚI" : "CẬP_NHẬT";
                    results[index] = new(lineNumber, prepared[index]!.Code, action, null);
                }
                catch (ArgumentException exception)
                {
                    results[index] = new(lineNumber, SchoolCode(row), "", exception.Message);
                }
                catch (KeyNotFoundException exception)
                {
                    results[index] = new(lineNumber, SchoolCode(row), "", exception.Message);
                }
            }

            var validRows = prepared.Where(row => row is not null).ToArray();
            if (commit)
            {
                foreach (var row in validRows)
                    await SaveRowAsync(connection, (MySqlTransaction)transaction, row!, ct);

                using var audit = new MySqlCommand(@"
                    INSERT INTO NhatKy(IdTaiKhoan,HanhDong,BangDuLieu,KhoaBanGhi,ChiTiet)
                    VALUES(@actor,'NHAP_DU_LIEU_DOC_GIA','DocGia',@batchId,
                        JSON_OBJECT('TongDong',@total,'HopLe',@valid,'Loi',@errors,
                                    'ThemMoi',@created,'CapNhat',@updated))", connection, (MySqlTransaction)transaction);
                audit.Parameters.AddWithValue("@actor", actor.AccountId);
                audit.Parameters.AddWithValue("@batchId", request.BatchId);
                audit.Parameters.AddWithValue("@total", rows.Length);
                audit.Parameters.AddWithValue("@valid", validRows.Length);
                audit.Parameters.AddWithValue("@errors", rows.Length - validRows.Length);
                audit.Parameters.AddWithValue("@created", validRows.Count(row => row!.ExistingId is null));
                audit.Parameters.AddWithValue("@updated", validRows.Count(row => row!.ExistingId is not null));
                await audit.ExecuteNonQueryAsync(ct);
            }
            await transaction.CommitAsync(ct);

            return new ReaderImportResult(
                request.BatchId,
                rows.Length,
                validRows.Length,
                rows.Length - validRows.Length,
                validRows.Count(row => row!.ExistingId is null),
                validRows.Count(row => row!.ExistingId is not null),
                commit,
                results);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task<PreparedRow> ValidateRowAsync(
        MySqlConnection connection, MySqlTransaction transaction, ReaderImportRow row, CancellationToken ct)
    {
        var code = SchoolCode(row);
        if (code.Length is < 1 or > 50)
            throw new ArgumentException("Thiếu mã sinh viên hoặc mã cán bộ/giảng viên, hoặc mã vượt 50 ký tự.");
        var studentCode = NormalizeCode(row.MaSinhVien);
        var employeeCode = NormalizeCode(row.MaGiangVien);
        if ((studentCode.Length > 0) == (employeeCode.Length > 0))
            throw new ArgumentException("Điền đúng một mã: MaSinhVien cho sinh viên hoặc MaGiangVien cho cán bộ/giảng viên.");
        var type = NormalizeReaderType(row.LoaiDocGia);
        if (type.Length == 0 || !ReaderTypes.Contains(type))
            throw new ArgumentException("LoaiDocGia không thuộc danh sách cho phép.");
        if ((type == "SINH_VIEN") != (studentCode.Length > 0))
            throw new ArgumentException("Loại độc giả không khớp với cột mã sinh viên/mã cán bộ-giảng viên.");
        var name = (row.HoTen ?? "").Trim();
        if (name.Length is < 1 or > 200)
            throw new ArgumentException("Họ tên bắt buộc và tối đa 200 ký tự.");
        var status = NormalizeToken(row.TrangThaiHocTap);
        if (status.Length == 0 || !TryMapProfileStatus(type, status, out var profileStatus))
            throw new ArgumentException("Trạng thái trường trống hoặc chưa được hỗ trợ; kiểm tra trạng thái học tập/công tác trong danh sách cho phép.");

        var facultyCode = NormalizeCode(row.MaKhoa);
        var majorCode = NormalizeCode(row.MaNganh);
        if (majorCode.Length > 0 && type != "SINH_VIEN")
            throw new ArgumentException("MaNganh chỉ áp dụng cho sinh viên.");
        if (facultyCode.Length > 40)
            throw new ArgumentException("MaKhoa tối đa 40 ký tự.");
        if (majorCode.Length > 7 || majorCode.Length > 0 && !Regex.IsMatch(majorCode, "^[0-9]{7}$", RegexOptions.CultureInvariant))
            throw new ArgumentException("MaNganh phải gồm đúng 7 chữ số.");
        var cohort = CleanOptional(row.KhoaHoc, 30, "KhoaHoc");
        var sourceStatus = CleanOptional(row.TrangThaiHocTap, 50, "TrangThaiHocTap")!;
        var email = CleanOptional(row.ThuDienTu, 254, "ThuDienTu");
        if (!string.IsNullOrEmpty(email) && (!email.Contains('@') || email.StartsWith('@') || email.EndsWith('@')))
            throw new ArgumentException("ThuDienTu không đúng định dạng email cơ bản.");
        var fullFaculty = await ResolveFacultyAsync(connection, transaction, facultyCode, ct);
        var fullMajor = await ResolveMajorAsync(connection, transaction, majorCode, ct);

        int? existingId = null;
        int? oldFaculty = null;
        string? oldMajor = null;
        using (var find = new MySqlCommand(@"
            SELECT IdDocGia,IdKhoa,MaNganh
            FROM DocGia
            WHERE UPPER(REGEXP_REPLACE(MaDinhDanhTruong,'[[:space:]]+',''))=@code
            FOR UPDATE", connection, transaction))
        {
            find.Parameters.AddWithValue("@code", code);
            await using var reader = await find.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                existingId = reader.GetInt32(0);
                oldFaculty = reader.IsDBNull(1) ? null : reader.GetInt32(1);
                oldMajor = reader.IsDBNull(2) ? null : reader.GetString(2);
                if (await reader.ReadAsync(ct))
                    throw new InvalidOperationException("Có nhiều hồ sơ trùng mã sau chuẩn hóa; cần xử lý dữ liệu trước khi nhập.");
            }
        }
        if (type != "SINH_VIEN" && oldMajor is not null)
            throw new ArgumentException("Hồ sơ hiện có ngành đào tạo; không thể đổi sang nhóm cán bộ bằng import.");
        var effectiveFaculty = fullFaculty ?? oldFaculty;
        var effectiveMajor = majorCode.Length > 0 ? majorCode : oldMajor;
        if (effectiveMajor is not null)
        {
            var majorFaculty = fullMajor?.FacultyId ?? await GetMajorFacultyAsync(connection, transaction, effectiveMajor, ct);
            if (effectiveFaculty is not null && majorFaculty != effectiveFaculty)
                throw new ArgumentException("MaNganh không thuộc MaKhoa đã nhập hoặc hồ sơ hiện có.");
            if (effectiveFaculty is null)
                throw new ArgumentException("Không thể xác nhận quan hệ ngành-khoa khi MaKhoa đang trống.");
        }

        return new PreparedRow(code, name, type, fullFaculty, majorCode.Length == 0 ? null : majorCode,
            cohort, sourceStatus, profileStatus, email, existingId);
    }

    private static async Task<int?> ResolveFacultyAsync(MySqlConnection connection, MySqlTransaction transaction, string code, CancellationToken ct)
    {
        if (code.Length == 0) return null;
        using var command = new MySqlCommand(@"
            SELECT IdKhoa FROM Khoa
            WHERE UPPER(REGEXP_REPLACE(MaKhoa,'[[:space:]]+',''))=@code
               OR UPPER(REGEXP_REPLACE(TenKhoa,'[[:space:]]+',''))=@code", connection, transaction);
        command.Parameters.AddWithValue("@code", code);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new KeyNotFoundException("Không tìm thấy mã/tên khoa hoặc phòng ban trong danh mục.");
        var facultyId = reader.GetInt32(0);
        if (await reader.ReadAsync(ct)) throw new ArgumentException("Giá trị MaKhoa/tên khoa khớp nhiều mục; dùng MaKhoa duy nhất.");
        return facultyId;
    }

    private static async Task<(string Code, int FacultyId)?> ResolveMajorAsync(MySqlConnection connection, MySqlTransaction transaction, string code, CancellationToken ct)
    {
        if (code.Length == 0) return null;
        using var command = new MySqlCommand("SELECT MaNganh,IdKhoa FROM NganhDaoTao WHERE MaNganh=@code", connection, transaction);
        command.Parameters.AddWithValue("@code", code);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new KeyNotFoundException("Không tìm thấy MaNganh trong danh mục.");
        return (reader.GetString(0), reader.GetInt32(1));
    }

    private static async Task<int> GetMajorFacultyAsync(MySqlConnection connection, MySqlTransaction transaction, string code, CancellationToken ct)
    {
        using var command = new MySqlCommand("SELECT IdKhoa FROM NganhDaoTao WHERE MaNganh=@code", connection, transaction);
        command.Parameters.AddWithValue("@code", code);
        var value = await command.ExecuteScalarAsync(ct);
        return value is null or DBNull ? throw new KeyNotFoundException("Không tìm thấy ngành đang có trong danh mục.") : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static async Task SaveRowAsync(MySqlConnection connection, MySqlTransaction transaction, PreparedRow row, CancellationToken ct)
    {
        using var command = new MySqlCommand();
        command.Connection = connection;
        command.Transaction = transaction;
        if (row.ExistingId is null)
        {
            command.CommandText = @"
                INSERT INTO DocGia(MaDinhDanhTruong,HoTen,LoaiDocGia,IdKhoa,MaNganh,KhoaHoc,TrangThaiHocTap,ThuDienTu,TrangThaiHoSo)
                VALUES(@code,@name,@type,@faculty,@major,@cohort,@sourceStatus,@email,@profileStatus)";
            command.Parameters.AddWithValue("@code", row.Code);
        }
        else
        {
            command.CommandText = @"
                UPDATE DocGia
                SET HoTen=@name,LoaiDocGia=@type,
                    IdKhoa=COALESCE(@faculty,IdKhoa),MaNganh=COALESCE(@major,MaNganh),
                    KhoaHoc=COALESCE(@cohort,KhoaHoc),TrangThaiHocTap=@sourceStatus,
                    ThuDienTu=COALESCE(@email,ThuDienTu),TrangThaiHoSo=@profileStatus
                WHERE IdDocGia=@readerId";
            command.Parameters.AddWithValue("@readerId", row.ExistingId.Value);
        }
        command.Parameters.AddWithValue("@name", row.Name);
        command.Parameters.AddWithValue("@type", row.Type);
        command.Parameters.AddWithValue("@faculty", (object?)row.FacultyId ?? DBNull.Value);
        command.Parameters.AddWithValue("@major", (object?)row.MajorCode ?? DBNull.Value);
        command.Parameters.AddWithValue("@cohort", (object?)row.Cohort ?? DBNull.Value);
        command.Parameters.AddWithValue("@sourceStatus", row.SourceStatus);
        command.Parameters.AddWithValue("@email", (object?)row.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@profileStatus", row.ProfileStatus);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static string? CleanOptional(string? value, int maxLength, string field)
    {
        var cleaned = value?.Trim() ?? "";
        if (cleaned.Length > maxLength) throw new ArgumentException($"{field} tối đa {maxLength} ký tự.");
        return cleaned.Length == 0 ? null : cleaned;
    }

    private static string NormalizeReaderType(string? value)
    {
        var normalized = NormalizeToken(value);
        return normalized switch
        {
            "SINHVIEN" or "SV" => "SINH_VIEN",
            "CANBO" or "NHANVIEN" => "CAN_BO",
            "GIANGVIEN" or "GV" => "GIANG_VIEN",
            "TROGIANG" => "TRO_GIANG",
            "CNV" => "CNV",
            _ => ""
        };
    }

    private static string SchoolCode(ReaderImportRow row)
        => NormalizeCode(row.MaSinhVien).Length > 0 ? NormalizeCode(row.MaSinhVien) : NormalizeCode(row.MaGiangVien);

    private static string NormalizeCode(string? value)
        => string.Concat((value ?? "").Where(character => !char.IsWhiteSpace(character))).ToUpperInvariant();

    private static string NormalizeToken(string? value)
    {
        var normalized = (value ?? "").Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(character))
                builder.Append(character);
        return builder.ToString().Replace('đ', 'd').Replace('Đ', 'D').ToUpperInvariant();
    }

    private static bool TryMapProfileStatus(string type, string status, out string profileStatus)
    {
        var active = type == "SINH_VIEN"
            ? new[] { "DANGHOC", "DANGTHEOHOC", "DANGHOCTAP" }
            : new[] { "DANGCONGTAC", "DANGLAMVIEC", "DANGHOATDONG" };
        var inactive = type == "SINH_VIEN"
            ? new[] { "BAOLUU", "TAMNGHI", "NGHIHOC", "THOIHOC", "DATHOIHOC", "DATOTNGHIEP", "TOTNGHIEP", "NGUNGHOATDONG" }
            : new[] { "NGHIVIEC", "DANGHI", "DANGHIVIEC", "THOIVIEC", "NGUNGCONGTAC", "TAMNGHI", "NGUNGHOATDONG" };
        if (active.Contains(status, StringComparer.Ordinal)) { profileStatus = "HOAT_DONG"; return true; }
        if (inactive.Contains(status, StringComparer.Ordinal)) { profileStatus = "NGUNG_HOAT_DONG"; return true; }
        profileStatus = "";
        return false;
    }

    private sealed record PreparedRow(
        string Code, string Name, string Type, int? FacultyId, string? MajorCode,
        string? Cohort, string SourceStatus, string ProfileStatus, string? Email, int? ExistingId);
}
