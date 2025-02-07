using DomainStatusChecker.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IDomainStatusService, DomainStatusService>();
builder.Services.AddScoped<IWebsiteParserService, WebsiteParserService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// Configure Kestrel with timeouts
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Set the maximum request body size to 50MB
    serverOptions.Limits.MaxRequestBodySize = 52_428_800; // 50MB in bytes

    // Increase request timeouts
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(1);

    // Configure endpoints
    serverOptions.ListenAnyIP(5000, options =>
    {
        options.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
        options.UseConnectionLogging();
    });
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
