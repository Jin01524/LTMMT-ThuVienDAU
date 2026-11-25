using ThuVienDAU.Models;
namespace ThuVienDAU.Services;
public sealed class ReaderReportClientService(TcpCommandClient transport)
{
    private readonly FeatureCommandClient client = new(transport);
    public Task<ReaderReportResult> GetAsync(string sessionId, string? readerType, string? faculty, string? major,
        DateTime from, DateTime to, CancellationToken ct = default)
        => client.CallAsync<ReaderReportResult>("TV1_READER_REPORT", new ReaderReportQuery(readerType, faculty, major, from, to), sessionId, ct);
}
