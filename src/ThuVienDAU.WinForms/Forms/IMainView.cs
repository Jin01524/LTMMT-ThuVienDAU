using ThuVienDAU.Models;

namespace ThuVienDAU.Forms;

internal interface IMainView
{
    event EventHandler? ConnectionCheckRequested;

    void ShowConnectionCheckStarted();
    void ShowConnectionCheckSucceeded(DatabaseConnectionInfo connectionInfo);
    void ShowConnectionCheckFailed(int errorNumber);
    void ShowConnectionConfigurationError(string message);
    void EnableConnectionCheck();
}
