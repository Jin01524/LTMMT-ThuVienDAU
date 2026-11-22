using System.Buffers.Binary;
using System.Text.Json;
namespace ThuVienDAU.Protocol;
public sealed record TcpRequest(int Version, string RequestId, string Command, JsonElement? Payload = null, string? SessionToken = null);
public sealed record TcpResponse(int Version, string RequestId, bool Success, JsonElement? Data = null, string? Error = null, int? ErrorNumber = null);
public static class TcpProtocol
{
    public const int Version = 1;
    public const int MaxFrameBytes = 1024 * 1024;
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static async Task WriteAsync<T>(Stream stream, T value, CancellationToken token)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, Json);
        if (bytes.Length is <= 0 or > MaxFrameBytes) throw new InvalidDataException("Ban tin TCP vuot gioi han.");
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, bytes.Length);
        await stream.WriteAsync(header, token);
        await stream.WriteAsync(bytes, token);
    }
    public static async Task<T> ReadAsync<T>(Stream stream, CancellationToken token)
    {
        var header = new byte[4];
        await stream.ReadExactlyAsync(header, token);
        var length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length is <= 0 or > MaxFrameBytes) throw new InvalidDataException("Do dai ban tin TCP khong hop le.");
        var bytes = new byte[length];
        await stream.ReadExactlyAsync(bytes, token);
        return JsonSerializer.Deserialize<T>(bytes, Json) ?? throw new JsonException("Ban tin TCP rong.");
    }
}
