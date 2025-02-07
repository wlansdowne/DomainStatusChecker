using DomainStatusChecker.Models;
using DomainStatusChecker.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50MB
    options.Limits.MaxRequestHeadersTotalSize = 1048576; // 1MB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);
});

// Configure services
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50MB
    options.Limits.MaxRequestHeadersTotalSize = 1048576; // 1MB
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 52428800; // 50MB
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// Register services
builder.Services.AddScoped<IDomainStatusService, DomainStatusService>();
builder.Services.AddScoped<IWebsiteParserService, WebsiteParserService>();
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
else
{
    app.UseDeveloperExceptionPage();
}

// Use CORS
app.UseCors();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
