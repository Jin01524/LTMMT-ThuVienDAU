using ThuVienDAU.Services;

var builder = WebApplication.CreateBuilder(args);
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
{
    builder.WebHost.UseUrls("http://localhost:5080");
}

builder.Services.AddControllers();
builder.Services.AddScoped<DatabaseHealthService>();

var app = builder.Build();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", application = "ThuVienDAU.Server" }));
app.MapControllers();
app.Run();
