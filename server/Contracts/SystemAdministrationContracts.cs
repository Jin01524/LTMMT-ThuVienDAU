namespace ThuVienDAU.Models;

public sealed record SystemConfigurationInfo(string Json, IReadOnlyList<string> PendingDecisions);

public sealed record AuditLogEntry(
    long LogId,
    DateTime Timestamp,
    int? AccountId,
    string? Username,
    string Action,
    string TableName,
    string RecordKey,
    string DetailsJson);
