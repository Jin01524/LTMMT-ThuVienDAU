namespace ThuVienDAU.Models;

public sealed record LoginRequest(string Username, string Password);

public sealed record SessionInfo(string SessionId, int AccountId, int? ReaderId, string Username, string Role, string DisplayName);

public sealed record ReaderAccountItem(int AccountId, int ReaderId, string ReaderCode, string ReaderName, string Username, bool IsActive);

public sealed record ReaderAccountStatusRequest(string SessionId, int AccountId, bool IsActive, string Reason);

public sealed record ProvisionAccountRequest(string SessionId, string Username, string Password, string Role, int? ReaderId);

public sealed record AccountItem(int AccountId, string Username, string Role, int? ReaderId, bool IsActive);

public sealed record AuthError(string Message);
