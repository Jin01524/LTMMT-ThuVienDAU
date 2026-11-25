using System.Security.Cryptography;
using System.Text;
namespace ThuVienDAU.Catalog;
// HT-02 can replace this resolver with its authenticated, expiring server-side sessions.
public interface ICatalogSessionResolver { int? Resolve(string? sessionToken); }
public sealed class DevelopmentCatalogSession(int accountId) : ICatalogSessionResolver
{
    public string Token { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public int? Resolve(string? sessionToken)
    {
        if (sessionToken is null || sessionToken.Length != Token.Length) return null;
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(sessionToken), Encoding.UTF8.GetBytes(Token)) ? accountId : null;
    }
}

public sealed class CombinedCatalogSession(ICatalogSessionResolver real,ICatalogSessionResolver demo):ICatalogSessionResolver { public int? Resolve(string? token)=>real.Resolve(token)??demo.Resolve(token); }
