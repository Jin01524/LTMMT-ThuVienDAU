using ThuVienDAU.Configuration;
using ThuVienDAU.Services;
using ThuVienDAU.Server.Modules.TV1_VoNgocBinh.BC;
using ThuVienDAU.Server.Modules.TV1_VoNgocBinh.DG;
using ThuVienDAU.Server.Modules.TV1_VoNgocBinh.HT;
using ThuVienDAU.Server.Services;

var builder = WebApplication.CreateBuilder(args);
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
{
    builder.WebHost.UseUrls("http://localhost:5080");
}

builder.Services.AddControllers();
builder.Services.AddScoped<DatabaseHealthService>();
var databaseOptions = DatabaseOptions.Load();
builder.Services.AddSingleton(databaseOptions);
var authenticationService = new AuthenticationService(databaseOptions);
builder.Services.AddSingleton(authenticationService);
builder.Services.AddScoped<ReaderManagementService>();
builder.Services.AddScoped<SystemAdministrationService>();
builder.Services.AddScoped<ReaderReportService>();

var app = builder.Build();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", application = "ThuVienDAU.Server" }));
app.MapControllers();
app.Run();
