using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using website.updater.Models;
using website.updater.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// 設定請求體大小限制（預設 30MB，設定為 500MB）
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524288000; // 500MB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<KestrelServerOptions>(options =>
    {
        options.Limits.MaxRequestBodySize = 524288000; // 500MB
    });
}

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppInfo"));

var app = builder.Build();

var settings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;
PM2Utils.Initialize(settings.Pm2Path);

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
