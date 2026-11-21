using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU.Server.Controllers;

[ApiController]
[Route("api/database")]
public sealed class DatabaseHealthController(DatabaseHealthService service) : ControllerBase
{
    [HttpGet("health")]
    public async Task<ActionResult<DatabaseConnectionInfo>> Check(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.CheckAsync(cancellationToken));
        }
        catch (MySqlException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new DatabaseHealthError(
                    "Server chưa kết nối được MySQL. Kiểm tra máy chủ, cổng, tài khoản và quyền truy cập.",
                    exception.Number));
        }
        catch (Exception exception) when (exception is InvalidOperationException or
            ArgumentException or JsonException or IOException or UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new DatabaseHealthError(
                    "Server cần cấu hình MySQL. Kiểm tra appsettings.local.json trong thư mục server và build lại."));
        }
    }
}
