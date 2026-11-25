using ThuVienDAU.Models;
namespace ThuVienDAU.Services;
public sealed class ReaderDirectoryService(TcpCommandClient transport)
{
    private readonly FeatureCommandClient client = new(transport);
    public async Task<IReadOnlyList<ReaderProfile>> SearchAsync(string sessionId, string query, CancellationToken ct = default)
        => (await client.CallAsync<ReaderSearchResult>("TV1_READER_SEARCH", new ReaderQuery(query), sessionId, ct)).Readers;
    public Task<BorrowPermissionResult> SetBorrowPermissionAsync(int readerId, BorrowPermissionRequest request, CancellationToken ct = default)
        => client.CallAsync<BorrowPermissionResult>("TV1_BORROW_PERMISSION", new BorrowPermissionCommand(readerId, request.CanBorrow, request.Reason), request.SessionId, ct);
}
