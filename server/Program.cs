using ThuVienDAU.Configuration;
using ThuVienDAU.Services;
using ThuVienDAU.Server.Services;

var builder = WebApplication.CreateBuilder(args);
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
{
    builder.WebHost.UseUrls("http://localhost:5080");
}

builder.Services.AddControllers();
builder.Services.AddScoped<DatabaseHealthService>();
var authenticationService = new AuthenticationService(DatabaseOptions.Load());
builder.Services.AddSingleton(authenticationService);

var app = builder.Build();
await authenticationService.EnsureDemoAccountsAsync(CancellationToken.None);
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", application = "ThuVienDAU.Server" }));
app.MapControllers();
app.Run();
