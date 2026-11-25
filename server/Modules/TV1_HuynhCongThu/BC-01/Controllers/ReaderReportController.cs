using ThuVienDAU.Models;
using ThuVienDAU.Protocol;
using ThuVienDAU.Server.Modules.TV1_HuynhCongThu.BC;
namespace ThuVienDAU.Server.Controllers;
public sealed class ReaderReportController(ReaderReportService service)
{
    public async Task<object> HandleAsync(TcpRequest request, string token, CancellationToken ct)
    {
        var query = TV1Controller.Body<ReaderReportQuery>(request);
        return await service.GetAsync(token, query.ReaderType, query.Faculty, query.Major, query.From, query.To, ct);
    }
}
