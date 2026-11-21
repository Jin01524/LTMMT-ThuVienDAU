namespace ThuVienDAU.Models;

public sealed record DatabaseHealthError(string Message, int? ErrorNumber = null);
