using System.Text.Json;
using MySql.Data.MySqlClient;
using ThuVienDAU.Models;
using ThuVienDAU.Protocol;
using ThuVienDAU.Server.Services;
namespace ThuVienDAU.Server.Controllers;
public sealed class AuthenticationController(AuthenticationService service)
{
    public async Task<TcpResponse> HandleAsync(TcpRequest request,CancellationToken ct)
    {
        try
        {
            if(request.Version!=1||string.IsNullOrWhiteSpace(request.RequestId)||request.RequestId.Length>64)throw new ArgumentException("Phiên bản/mã yêu cầu không hợp lệ.");
            var token=request.SessionToken??"";object result;
            switch(request.Command)
            {
                case "AUTH_LOGIN":result=await service.LoginAsync(Body<LoginRequest>(request),ct);break;
                case "AUTH_LOGOUT":service.Logout(token);result=new { loggedOut=true };break;
                case "AUTH_ACCOUNTS":result=await service.GetAccountsAsync(token,ct);break;
                case "AUTH_READER_ACCOUNTS":result=await service.GetReaderAccountsAsync(token,ct);break;
                case "AUTH_PROVISION":result=await service.ProvisionAccountAsync(Body<ProvisionAccountRequest>(request) with { SessionId=token },ct);break;
                case "AUTH_READER_STATUS":await service.SetReaderAccountStatusAsync(Body<ReaderAccountStatusRequest>(request) with { SessionId=token },ct);result=new { updated=true };break;
                default:throw new ArgumentException("Lệnh đăng nhập không được hỗ trợ.");
            }
            return TV1Controller.Success(request,result);
        }
        catch(UnauthorizedAccessException ex){return new(1,request.RequestId,false,Error:ex.Message);}
        catch(MySqlException ex){return new(1,request.RequestId,false,Error:ex.Number==1062?"Tên đăng nhập hoặc hồ sơ đã có tài khoản.":ex.Number==1452?"Hồ sơ độc giả không tồn tại.":ex.Number==1644?"CSDL từ chối thao tác: "+ex.Message:"Không truy cập được MySQL. Kiểm tra cấu hình server.",ErrorNumber:ex.Number);}
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException or IOException or JsonException){return new(1,request.RequestId,false,Error:ex is JsonException?"Dữ liệu đăng nhập không hợp lệ.":ex.Message);}
    }
    private static T Body<T>(TcpRequest r) where T:class => r.Payload?.Deserialize<T>(TcpProtocol.Json)??throw new ArgumentException("Thiếu dữ liệu yêu cầu.");
}
