using DomainStatusChecker.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IDomainStatusService, DomainStatusService>();
builder.Services.AddScoped<IWebsiteParserService, WebsiteParserService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// Configure Kestrel with timeouts and limits
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Set the maximum request body size to 50MB
    serverOptions.Limits.MaxRequestBodySize = 52_428_800; // 50MB in bytes

    // Increase request timeouts
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);

    // Increase header size limits
    serverOptions.Limits.MaxRequestHeadersTotalSize = 64 * 1024; // 64KB
    serverOptions.Limits.MaxRequestLineSize = 32 * 1024; // 32KB

    // Configure endpoints
    serverOptions.ListenAnyIP(5000, options =>
    {
        options.Protocols = HttpProtocols.Http1AndHttp2;
        options.UseConnectionLogging();
    });
});

// Configure general HTTP limits
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true; // Enable if needed for large file uploads
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Configure static files with explicit paths
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "wwwroot")),
    RequestPath = ""
});

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add health check endpoint
app.MapGet("/health", () => "Healthy");

app.Run();
