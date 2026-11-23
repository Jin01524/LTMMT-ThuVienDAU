using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ThuVienDAU.Models;
using ThuVienDAU.Server.Services;

namespace ThuVienDAU.Server.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController(AuthenticationService service) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<SessionInfo>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new AuthError("Nhập tên đăng nhập và mật khẩu."));
            }
            return Ok(await service.LoginAsync(request, cancellationToken));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new AuthError(exception.Message));
        }
        catch (MySqlException exception)
        {
            return StatusCode(503, new AuthError($"Không truy cập được cơ sở dữ liệu. Mã MySQL: {exception.Number}"));
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout([FromBody] string sessionId)
    {
        service.Logout(sessionId);
        return Ok();
    }

    [HttpGet("reader-accounts")]
    public async Task<ActionResult<IReadOnlyList<ReaderAccountItem>>> ReaderAccounts(
        [FromQuery] string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.GetReaderAccountsAsync(sessionId, cancellationToken));
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new AuthError(exception.Message));
        }
    }

    [HttpPut("reader-accounts/status")]
    public async Task<IActionResult> SetReaderAccountStatus(
        ReaderAccountStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await service.SetReaderAccountStatusAsync(request, cancellationToken);
            return Ok();
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
            return BadRequest(new AuthError(exception.Message));
        }
    }
}
