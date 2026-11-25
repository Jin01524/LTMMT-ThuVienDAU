using ThuVienDAU.Models;
using ThuVienDAU.Protocol;
using ThuVienDAU.Server.Modules.TV1_HuynhCongThu.DG;
namespace ThuVienDAU.Server.Controllers;
public sealed class ReaderManagementController(ReaderManagementService service)
{
    public async Task<object> HandleAsync(TcpRequest request, string token, CancellationToken ct)
    {
        if (request.Command == "TV1_READER_SEARCH")
            return new ReaderSearchResult(await service.SearchAsync(token, TV1Controller.Body<ReaderQuery>(request).Query, ct));
        var command = TV1Controller.Body<BorrowPermissionCommand>(request);
        return await service.SetBorrowPermissionAsync(command.ReaderId, new BorrowPermissionRequest(token, command.CanBorrow, command.Reason), ct);
    }
}
