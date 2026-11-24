namespace ThuVienDAU.Models;

public sealed record ReaderGroupCount(string Group, string Value, int Count);

public sealed record ReaderReportResult(
    int TotalReaders,
    int ActiveProfiles,
    int BorrowEnabled,
    bool ReaderListTruncated,
    IReadOnlyList<ReaderGroupCount> ByType,
    IReadOnlyList<ReaderGroupCount> ByFaculty,
    IReadOnlyList<ReaderGroupCount> ByMajor,
    IReadOnlyList<ReaderProfile> Readers,
    IReadOnlyList<AuditLogEntry> Activity);
