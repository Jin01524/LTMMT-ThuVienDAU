using ThuVienDAU.Models;
namespace ThuVienDAU.Services;
public sealed class SystemAdministrationClientService(TcpCommandClient transport)
{
    private readonly FeatureCommandClient client = new(transport);
    public Task<SystemConfigurationInfo> GetConfigurationAsync(string sessionId, CancellationToken ct = default)
        => client.CallAsync<SystemConfigurationInfo>("TV1_CONFIGURATION", new { }, sessionId, ct);
    public async Task<IReadOnlyList<AuditLogEntry>> GetAuditLogAsync(string sessionId, DateTime from, DateTime to, CancellationToken ct = default)
        => await client.CallAsync<AuditLogEntry[]>("TV1_AUDIT", new AuditQuery(from, to), sessionId, ct);
}
