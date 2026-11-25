namespace ThuVienDAU.Models;
public sealed record ReaderQuery(string Query);
public sealed record BorrowPermissionCommand(int ReaderId, bool CanBorrow, string Reason);
public sealed record AuditQuery(DateTime From, DateTime To);
public sealed record ReaderReportQuery(string? ReaderType, string? Faculty, string? Major, DateTime From, DateTime To);
