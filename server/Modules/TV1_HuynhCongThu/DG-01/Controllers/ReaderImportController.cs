using ThuVienDAU.Models;
using ThuVienDAU.Protocol;
using ThuVienDAU.Server.Modules.TV1_HuynhCongThu.DG;

namespace ThuVienDAU.Server.Controllers;

public sealed class ReaderImportController(ReaderImportService service)
{
    public async Task<object> HandleAsync(TcpRequest request, string sessionToken, CancellationToken ct)
    {
        var command = TV1Controller.Body<ReaderImportCommand>(request);
        return request.Command switch
        {
            "TV1_READER_IMPORT_PREVIEW" => await service.PreviewAsync(sessionToken, command, ct),
            "TV1_READER_IMPORT_COMMIT" => await service.CommitAsync(sessionToken, command, ct),
            _ => throw new ArgumentException("Lệnh nhập độc giả không được hỗ trợ.")
        };
    }
}
