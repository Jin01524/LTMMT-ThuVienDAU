using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ThuVienDAU.Models;
using ThuVienDAU.Server.Modules.TV1_VoNgocBinh.DG;

namespace ThuVienDAU.Server.Controllers;

[ApiController]
[Route("api/readers")]
public sealed class ReaderManagementController(ReaderManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReaderSearchResult>> Search(
        [FromQuery] string sessionId, [FromQuery] string q, CancellationToken cancellationToken)
    {
        try
        {
            var readers = await service.SearchAsync(sessionId, q, cancellationToken);
            return Ok(new ReaderSearchResult(readers));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new AuthError(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new AuthError(exception.Message));
        }
        catch (MySqlException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new AuthError($"Không tra cứu được hồ sơ độc giả. Mã MySQL: {exception.Number}"));
        }
    }

    [HttpPut("{readerId:int}/borrow-permission")]
    public async Task<ActionResult<BorrowPermissionResult>> SetBorrowPermission(
        int readerId, BorrowPermissionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.SetBorrowPermissionAsync(readerId, request, cancellationToken));
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new AuthError(exception.Message));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new AuthError(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new AuthError(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new AuthError(exception.Message));
        }
        catch (MySqlException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new AuthError($"Không cập nhật được quyền mượn. Mã MySQL: {exception.Number}"));
        }
    }
}
