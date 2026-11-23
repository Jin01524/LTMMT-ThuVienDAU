using ThuVienDAU.Protocol;
namespace ThuVienDAU.Catalog;
public sealed class CatalogController(ICatalogSessionResolver? sessions)
{
    private readonly CatalogService service=new(sessions);
    public Task<TcpResponse> HandleAsync(TcpRequest request,CancellationToken ct) => service.HandleAsync(request,ct);
}
