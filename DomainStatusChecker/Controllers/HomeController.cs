using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DomainStatusChecker.Models;
using DomainStatusChecker.Services;

namespace DomainStatusChecker.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebsiteParserService _websiteParser;
    private readonly IConfigurationService _configService;

    public HomeController(
        ILogger<HomeController> logger,
        IWebsiteParserService websiteParser,
        IConfigurationService configService)
    {
        _logger = logger;
        _websiteParser = websiteParser;
        _configService = configService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Settings()
    {
        var model = new SettingsViewModel
        {
            Subnets = _configService.GetSubnets(),
            CdnOrganizations = _configService.GetCdnOrganizations()
        };
        return View(model);
    }

    [HttpPost]
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)] // 50MB
    [RequestSizeLimit(52428800)] // 50MB
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        try
        {
            _logger.LogInformation("Starting file upload: {FileName}, Size: {Size}KB", 
                file.FileName, file.Length / 1024);

            using var stream = file.OpenReadStream();
            // Set a timeout for the entire operation
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
            
            var websites = await Task.Run(async () => 
            {
                try
                {
                    return await _websiteParser.ParseWebsitesFromCsvAsync(stream)
                        .WaitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("CSV processing timeout");
                    throw new TimeoutException("Processing the CSV file took too long. Please try with a smaller file or contact support.");
                }
            }, cts.Token);

            _logger.LogInformation("File processing completed: {Count} websites processed", 
                websites.Count);

            return View("Report", websites);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout processing file: {FileName}", file.FileName);
            return StatusCode(StatusCodes.Status408RequestTimeout, 
                "The request timed out. Please try with a smaller file or contact support.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file: {FileName}", file.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                "An error occurred while processing the file. Please try again or contact support.");
        }
    }

    [HttpPost]
    public IActionResult SaveSettings(SettingsViewModel model)
    {
        try
        {
            _configService.SaveSubnets(model.Subnets ?? new List<string>());
            _configService.SaveCdnOrganizations(model.CdnOrganizations ?? new List<string>());
            TempData["Message"] = "Settings saved successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
            TempData["Error"] = "Error saving settings: " + ex.Message;
        }

        return RedirectToAction(nameof(Settings));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
