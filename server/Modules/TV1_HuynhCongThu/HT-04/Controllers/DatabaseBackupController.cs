using ThuVienDAU.Models;
using ThuVienDAU.Protocol;
using ThuVienDAU.Server.Modules.TV1_HuynhCongThu.HT;

namespace ThuVienDAU.Server.Controllers;

public sealed class DatabaseBackupController(DatabaseBackupService service)
{
    public async Task<object> HandleAsync(TcpRequest request, string sessionToken, CancellationToken ct)
        => request.Command switch
        {
            "TV1_BACKUP_LIST" => await service.ListAsync(sessionToken, ct),
            "TV1_BACKUP_CREATE" => await service.CreateAsync(sessionToken, ct),
            "TV1_BACKUP_RESTORE" => await service.RestoreAsync(sessionToken,
                TV1Controller.Body<RestoreBackupRequest>(request), ct),
            _ => throw new ArgumentException("Lệnh sao lưu/phục hồi không được hỗ trợ.")
        };
}
