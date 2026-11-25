using ThuVienDAU.Models;
using ThuVienDAU.Services;

namespace ThuVienDAU.Controllers;

internal sealed class AuthenticationController
{
    private readonly AuthenticationService authenticationService;
    private readonly SessionContext sessionContext;

    public AuthenticationController(AuthenticationService authenticationService, SessionContext sessionContext)
    {
        this.authenticationService = authenticationService;
        this.sessionContext = sessionContext;
    }

    public SessionContext SessionContext => sessionContext;

    public async Task LogoutAsync()
    {
        if (sessionContext.Current is null) return;
        await authenticationService.LogoutAsync(sessionContext.Current.SessionId);
        sessionContext.Clear();
    }
}
