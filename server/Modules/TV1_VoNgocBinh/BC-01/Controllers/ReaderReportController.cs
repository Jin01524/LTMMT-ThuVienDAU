using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ThuVienDAU.Models;
using ThuVienDAU.Server.Modules.TV1_VoNgocBinh.BC;

namespace ThuVienDAU.Server.Controllers;

[ApiController]
[Route("api/reports/readers")]
public sealed class ReaderReportController(ReaderReportService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReaderReportResult>> Get(
        [FromQuery] string sessionId, [FromQuery] string? readerType,
        [FromQuery] string? faculty, [FromQuery] string? major,
        [FromQuery] DateTime from, [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.GetAsync(sessionId, readerType, faculty, major, from, to, cancellationToken));
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
                new AuthError($"Không tạo được báo cáo độc giả. Mã MySQL: {exception.Number}"));
        }
    }
}
