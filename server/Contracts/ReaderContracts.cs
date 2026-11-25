namespace ThuVienDAU.Models;

public sealed record ReaderProfile(
    int ReaderId,
    string SchoolId,
    string Name,
    string ReaderType,
    string? Faculty,
    string? Major,
    string ProfileStatus,
    bool CanBorrow,
    DateTime? BorrowBlockedUntil,
    string? BorrowBlockReason,
    int ActiveLoans,
    int UnresolvedFines);

public sealed record ReaderSearchResult(IReadOnlyList<ReaderProfile> Readers);

public sealed record BorrowPermissionRequest(
    string SessionId,
    bool CanBorrow,
    string Reason);

public sealed record BorrowPermissionResult(
    int ReaderId,
    bool CanBorrow,
    DateTime? BorrowBlockedUntil,
    string? BorrowBlockReason,
    int ActiveLoans,
    int UnresolvedFines);
