using System.Text.Json;
using ThuVienDAU.Models;
using ThuVienDAU.Protocol;

namespace ThuVienDAU.Services;

public sealed class ReaderImportClientService(TcpCommandClient transport)
{
    private const int MaximumRequestBytes = 900 * 1024;
    private readonly FeatureCommandClient client = new(transport);

    public Task<ReaderImportResult> PreviewAsync(string sessionId, ReaderImportCommand request, CancellationToken ct = default)
    {
        ValidateRequestSize(request);
        return client.CallAsync<ReaderImportResult>("TV1_READER_IMPORT_PREVIEW", request, sessionId, ct);
    }

    public Task<ReaderImportResult> CommitAsync(string sessionId, ReaderImportCommand request, CancellationToken ct = default)
    {
        ValidateRequestSize(request);
        return client.CallAsync<ReaderImportResult>("TV1_READER_IMPORT_COMMIT", request, sessionId, ct);
    }

    public IReadOnlyList<ReaderImportRow> ReadFile(string path) => ReaderImportFileReader.Read(path);

    private static void ValidateRequestSize(ReaderImportCommand request)
    {
        var size = JsonSerializer.SerializeToUtf8Bytes(request, TcpProtocol.Json).Length;
        if (size > MaximumRequestBytes)
            throw new InvalidDataException("Danh sách vượt giới hạn bản tin TCP; chia thành các file nhỏ hơn 900 KiB dữ liệu.");
    }
}
