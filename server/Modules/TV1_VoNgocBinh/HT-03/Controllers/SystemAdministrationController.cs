using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ThuVienDAU.Models;
using ThuVienDAU.Server.Modules.TV1_VoNgocBinh.HT;

namespace ThuVienDAU.Server.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemAdministrationController(SystemAdministrationService service) : ControllerBase
{
    [HttpGet("configuration")]
    public ActionResult<SystemConfigurationInfo> GetConfiguration([FromQuery] string sessionId)
    {
        try
        {
            return Ok(service.GetConfiguration(sessionId));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new AuthError(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new AuthError(exception.Message));
        }
        catch (IOException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new AuthError("Không đọc được file cấu hình hệ thống."));
        }
    }

    [HttpGet("audit")]
    public async Task<ActionResult<IReadOnlyList<AuditLogEntry>>> GetAuditLog(
        [FromQuery] string sessionId, [FromQuery] DateTime from, [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.GetAuditLogAsync(sessionId, from, to, cancellationToken));
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new AuthError(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new AuthError(exception.Message));
        }
        catch (MySqlException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new AuthError($"Không tra cứu được nhật ký. Mã MySQL: {exception.Number}"));
        }
    }
}
