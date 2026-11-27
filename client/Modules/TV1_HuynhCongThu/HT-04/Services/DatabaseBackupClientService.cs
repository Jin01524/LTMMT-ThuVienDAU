using ThuVienDAU.Models;

namespace ThuVienDAU.Services;

public sealed class DatabaseBackupClientService(TcpCommandClient transport)
{
    private readonly FeatureCommandClient client = new(transport);

    public Task<BackupListResult> ListAsync(string sessionId, CancellationToken ct = default)
        => client.CallAsync<BackupListResult>("TV1_BACKUP_LIST", new { }, sessionId, ct);

    public Task<BackupInfo> CreateAsync(string sessionId, CancellationToken ct = default)
        => client.CallAsync<BackupInfo>("TV1_BACKUP_CREATE", new { }, sessionId, ct);

    public Task<RestoreResult> RestoreAsync(string sessionId, RestoreBackupRequest request, CancellationToken ct = default)
        => client.CallAsync<RestoreResult>("TV1_BACKUP_RESTORE", request, sessionId, ct);
}
