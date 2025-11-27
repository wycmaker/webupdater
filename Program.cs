using website.updater.Models;
using website.updater.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddScoped<ZipUtils>();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppInfo"));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
