namespace ThuVienDAU.Models;

public sealed class DatabaseHealthCheckException(string message, int? errorNumber) : Exception(message)
{
    public int? ErrorNumber { get; } = errorNumber;
}
