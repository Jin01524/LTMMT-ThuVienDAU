using System.Text.Json;
using MySql.Data.MySqlClient;
using ThuVienDAU.Configuration;
using ThuVienDAU.Protocol;
using ThuVienDAU.Server.Services;
using ThuVienDAU.Server.Modules.TV1_HuynhCongThu.DG;
using ThuVienDAU.Server.Modules.TV1_HuynhCongThu.HT;
using ThuVienDAU.Server.Modules.TV1_HuynhCongThu.BC;
namespace ThuVienDAU.Server.Controllers;
public sealed class TV1Controller(AuthenticationService authentication)
{
    public async Task<TcpResponse> HandleAsync(TcpRequest request, CancellationToken ct)
    {
        try
        {
            if (request.Version != 1 || string.IsNullOrWhiteSpace(request.RequestId) || request.RequestId.Length > 64)
                throw new ArgumentException("Phiên bản/mã yêu cầu không hợp lệ.");
            var token = request.SessionToken ?? "";
            var options = DatabaseOptions.Load();
            object result = request.Command switch
            {
                "TV1_READER_SEARCH" or "TV1_BORROW_PERMISSION" => await new ReaderManagementController(new ReaderManagementService(options, authentication)).HandleAsync(request, token, ct),
                "TV1_CONFIGURATION" or "TV1_AUDIT" => await new SystemAdministrationController(new SystemAdministrationService(options, authentication)).HandleAsync(request, token, ct),
                "TV1_READER_REPORT" => await new ReaderReportController(new ReaderReportService(options, authentication)).HandleAsync(request, token, ct),
                _ => throw new ArgumentException("Lệnh TV1 không được hỗ trợ.")
            };
            return Success(request,result);
        }
        catch (MySqlException ex) { return new(1, request.RequestId, false, Error: ex.Number == 1644 ? ex.Message : "Không truy cập được CSDL; kiểm tra cấu hình hoặc thử tải lại.", ErrorNumber: ex.Number); }
        catch (Exception ex) when (ex is UnauthorizedAccessException or ArgumentException or InvalidOperationException or KeyNotFoundException or IOException or JsonException)
        { return new(1, request.RequestId, false, Error: ex is JsonException ? "Dữ liệu yêu cầu không hợp lệ." : ex.Message); }
    }
    internal static TcpResponse Success(TcpRequest request,object result)
    {
        var response=new TcpResponse(1,request.RequestId,true,JsonSerializer.SerializeToElement(result,TcpProtocol.Json));
        return JsonSerializer.SerializeToUtf8Bytes(response,TcpProtocol.Json).Length<=TcpProtocol.MaxFrameBytes
            ? response : new(1,request.RequestId,false,Error:"Kết quả vượt giới hạn 1 MiB. Thu hẹp bộ lọc rồi tải lại.");
    }
    internal static T Body<T>(TcpRequest request) where T : class => request.Payload?.Deserialize<T>(TcpProtocol.Json) ?? throw new ArgumentException("Thiếu dữ liệu yêu cầu.");
}
