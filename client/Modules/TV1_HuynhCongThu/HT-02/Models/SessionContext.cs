using ThuVienDAU.Models;

namespace ThuVienDAU.Models;

public sealed class SessionContext
{
    public SessionInfo? Current { get; private set; }

    public void Set(SessionInfo session) => Current = session;

    public void Clear() => Current = null;
}
