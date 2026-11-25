using MySql.Data.MySqlClient;
using ThuVienDAU.Configuration;
using ThuVienDAU.Data;
using ThuVienDAU.Models;
using ThuVienDAU.Server.Services;

namespace ThuVienDAU.Server.Modules.TV1_HuynhCongThu.DG;

public sealed class ReaderManagementService(DatabaseOptions options, AuthenticationService authentication)
{
    private readonly MySqlConnectionFactory connectionFactory = new(options);

    public async Task<IReadOnlyList<ReaderProfile>> SearchAsync(
        string sessionId, string query, CancellationToken cancellationToken)
    {
        await RequireStaff(sessionId,cancellationToken);
        var term = query?.Trim()??"";
        if (term.Length < 2 || term.Length > 100)
        {
            throw new ArgumentException("Nhập ít nhất 2 ký tự mã thẻ hoặc họ tên để tra cứu.");
        }

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT d.IdDocGia, d.MaDinhDanhTruong, d.HoTen, d.LoaiDocGia,
                   k.TenKhoa, n.TenNganh, d.TrangThaiHoSo, d.DuocMuon,
                   d.KhoaDen, d.LyDoKhoa,
                   (SELECT COUNT(*) FROM ChiTietPhieuMuon c
                    WHERE c.IdDocGia=d.IdDocGia AND c.TrangThai='DANG_MUON'),
                   (SELECT COUNT(*) FROM ViPham v
                    WHERE v.IdDocGia=d.IdDocGia AND v.TrangThai='CHUA_XU_LY')
            FROM DocGia d
            LEFT JOIN Khoa k ON k.IdKhoa=d.IdKhoa
            LEFT JOIN NganhDaoTao n ON n.MaNganh=d.MaNganh
            WHERE d.MaDinhDanhTruong LIKE @term OR d.HoTen LIKE @term
            ORDER BY d.HoTen, d.MaDinhDanhTruong
            LIMIT 100";
        command.Parameters.AddWithValue("@term", $"%{term}%");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<ReaderProfile>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ReaderProfile(
                reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetString(6), reader.GetBoolean(7),
                reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.GetInt32(10), reader.GetInt32(11)));
        }
        return results;
    }

    public async Task<BorrowPermissionResult> SetBorrowPermissionAsync(
        int readerId, BorrowPermissionRequest request, CancellationToken cancellationToken)
    {
        var manager = await RequireManager(request.SessionId,cancellationToken);
        var reason = request.Reason?.Trim()??"";
        if (reason.Length == 0 || reason.Length > 300)
        {
            throw new ArgumentException("Nhập lý do từ 1 đến 300 ký tự.");
        }
        if (readerId <= 0)
        {
            throw new ArgumentException("Mã hồ sơ độc giả không hợp lệ.");
        }

        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = (MySqlTransaction)transaction;
            command.CommandText = @"
                SELECT d.DuocMuon,
                       (SELECT COUNT(*) FROM ChiTietPhieuMuon c
                        WHERE c.IdDocGia=d.IdDocGia AND c.TrangThai='DANG_MUON'),
                       (SELECT COUNT(*) FROM ViPham v
                        WHERE v.IdDocGia=d.IdDocGia AND v.TrangThai='CHUA_XU_LY'),
                       (d.KhoaDen IS NOT NULL AND d.KhoaDen>CURRENT_TIMESTAMP)
                FROM DocGia d WHERE d.IdDocGia=@readerId FOR UPDATE";
            command.Parameters.AddWithValue("@readerId", readerId);
            bool currentlyAllowed;
            int activeLoans;
            int unresolvedFines;
            bool suspensionActive;
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new KeyNotFoundException("Không tìm thấy hồ sơ độc giả.");
                }
                currentlyAllowed = reader.GetBoolean(0);
                activeLoans = reader.GetInt32(1);
                unresolvedFines = reader.GetInt32(2);
                suspensionActive = reader.GetBoolean(3);
            }

            if (request.CanBorrow && (activeLoans > 0 || unresolvedFines > 0 || suspensionActive))
            {
                throw new InvalidOperationException(
                    $"Chưa thể mở quyền mượn: còn {activeLoans} bản đang mượn, {unresolvedFines} vi phạm chưa xử lý " +
                    "hoặc thời hạn đình chỉ chưa kết thúc.");
            }

            if (currentlyAllowed == request.CanBorrow)
            {
                await transaction.CommitAsync(cancellationToken);
                return new BorrowPermissionResult(readerId, currentlyAllowed, null, null, activeLoans, unresolvedFines);
            }

            command.Parameters.Clear();
            command.CommandText = @"
                UPDATE DocGia
                SET DuocMuon=@canBorrow,
                    KhoaDen=NULL,
                    LyDoKhoa=CASE WHEN @canBorrow=0 THEN @reason ELSE NULL END,
                    IdNguoiDoiQuyen=@managerId
                WHERE IdDocGia=@readerId";
            command.Parameters.AddWithValue("@canBorrow", request.CanBorrow);
            command.Parameters.AddWithValue("@reason", reason);
            command.Parameters.AddWithValue("@managerId", manager.AccountId);
            command.Parameters.AddWithValue("@readerId", readerId);
            await command.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new BorrowPermissionResult(
                readerId, request.CanBorrow, null, request.CanBorrow ? null : reason, activeLoans, unresolvedFines);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<SessionInfo> RequireStaff(string sessionId,CancellationToken cancellationToken)
    {
        var session=await authentication.ValidateSessionAsync(sessionId,cancellationToken);
        if (session.Role is not ("QUAN_TRI" or "QUAN_LY_THU_VIEN" or "BIEN_MUC" or "THU_THU"))
        {
            throw new UnauthorizedAccessException("Cần đăng nhập bằng tài khoản nhân viên để tra cứu độc giả.");
        }
        return session;
    }

    private async Task<SessionInfo> RequireManager(string sessionId,CancellationToken cancellationToken)
    {
        var session=await authentication.ValidateSessionAsync(sessionId,cancellationToken);
        if (session.Role != "QUAN_LY_THU_VIEN")
        {
            throw new UnauthorizedAccessException("Chỉ Quản lý thư viện được khóa hoặc mở quyền mượn.");
        }
        return session;
    }
}
