using ThuVienDAU.Models;
using ThuVienDAU.Protocol;
using ThuVienDAU.Server.Modules.TV1_HuynhCongThu.HT;
namespace ThuVienDAU.Server.Controllers;
public sealed class SystemAdministrationController(SystemAdministrationService service)
{
    public async Task<object> HandleAsync(TcpRequest request, string token, CancellationToken ct)
    {
        if (request.Command == "TV1_CONFIGURATION") return await service.GetConfigurationAsync(token, ct);
        var query = TV1Controller.Body<AuditQuery>(request);
        return await service.GetAuditLogAsync(token, query.From, query.To, ct);
    }
}
