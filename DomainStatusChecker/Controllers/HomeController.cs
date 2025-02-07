using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DomainStatusChecker.Models;
using DomainStatusChecker.Services;
using System.Text.Json;

namespace DomainStatusChecker.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebsiteParserService _websiteParserService;
    private readonly IConfigurationService _configService;
    private const string WebsitesKey = "ProcessedWebsites";
    private static List<Website>? _lastProcessedWebsites;

    public HomeController(
        ILogger<HomeController> logger,
        IWebsiteParserService websiteParserService,
        IConfigurationService configService)
    {
        _logger = logger;
        _websiteParserService = websiteParserService;
        _configService = configService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Settings()
    {
        var model = new SettingsViewModel
        {
            Subnets = _configService.GetSubnets(),
            CdnProviders = _configService.GetCdnProviders()
        };

        if (TempData["SuccessMessage"] != null)
        {
            model.SuccessMessage = TempData["SuccessMessage"].ToString();
        }
        if (TempData["ErrorMessage"] != null)
        {
            model.ErrorMessage = TempData["ErrorMessage"].ToString();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddSubnet(SettingsViewModel model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.NewSubnet))
            {
                TempData["ErrorMessage"] = "Subnet cannot be empty.";
                return RedirectToAction(nameof(Settings));
            }

            if (!_configService.ValidateCidr(model.NewSubnet))
            {
                TempData["ErrorMessage"] = "Invalid CIDR format. Example: 192.168.1.0/24";
                return RedirectToAction(nameof(Settings));
            }

            var subnets = _configService.GetSubnets();
            if (subnets.Contains(model.NewSubnet))
            {
                TempData["ErrorMessage"] = "This subnet already exists.";
                return RedirectToAction(nameof(Settings));
            }

            subnets.Add(model.NewSubnet);
            _configService.SaveSubnets(subnets);

            TempData["SuccessMessage"] = "Subnet added successfully.";
            return RedirectToAction(nameof(Settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding subnet");
            TempData["ErrorMessage"] = "Error adding subnet: " + ex.Message;
            return RedirectToAction(nameof(Settings));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveSubnet(string cidr)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cidr))
            {
                TempData["ErrorMessage"] = "Invalid subnet specified.";
                return RedirectToAction(nameof(Settings));
            }

            var subnets = _configService.GetSubnets();
            if (subnets.Count <= 1)
            {
                TempData["ErrorMessage"] = "Cannot remove the last subnet. At least one subnet is required.";
                return RedirectToAction(nameof(Settings));
            }

            if (!subnets.Contains(cidr))
            {
                TempData["ErrorMessage"] = "Subnet not found.";
                return RedirectToAction(nameof(Settings));
            }

            subnets.Remove(cidr);
            _configService.SaveSubnets(subnets);

            TempData["SuccessMessage"] = "Subnet removed successfully.";
            return RedirectToAction(nameof(Settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing subnet");
            TempData["ErrorMessage"] = "Error removing subnet: " + ex.Message;
            return RedirectToAction(nameof(Settings));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddCdnProvider(SettingsViewModel model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.NewCdnProvider))
            {
                TempData["ErrorMessage"] = "CDN provider name cannot be empty.";
                return RedirectToAction(nameof(Settings));
            }

            var providers = _configService.GetCdnProviders();
            if (providers.Contains(model.NewCdnProvider, StringComparer.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "This CDN provider already exists.";
                return RedirectToAction(nameof(Settings));
            }

            providers.Add(model.NewCdnProvider);
            _configService.SaveCdnProviders(providers);

            TempData["SuccessMessage"] = "CDN provider added successfully.";
            return RedirectToAction(nameof(Settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding CDN provider");
            TempData["ErrorMessage"] = "Error adding CDN provider: " + ex.Message;
            return RedirectToAction(nameof(Settings));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveCdnProvider(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Invalid CDN provider specified.";
                return RedirectToAction(nameof(Settings));
            }

            var providers = _configService.GetCdnProviders();
            if (providers.Count <= 1)
            {
                TempData["ErrorMessage"] = "Cannot remove the last CDN provider. At least one provider is required.";
                return RedirectToAction(nameof(Settings));
            }

            if (!providers.Contains(name))
            {
                TempData["ErrorMessage"] = "CDN provider not found.";
                return RedirectToAction(nameof(Settings));
            }

            providers.Remove(name);
            _configService.SaveCdnProviders(providers);

            TempData["SuccessMessage"] = "CDN provider removed successfully.";
            return RedirectToAction(nameof(Settings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing CDN provider");
            TempData["ErrorMessage"] = "Error removing CDN provider: " + ex.Message;
            return RedirectToAction(nameof(Settings));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to upload.";
            return RedirectToAction(nameof(Index));
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Please upload a CSV file.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var websites = await ProcessUploadedFile(file);
            _lastProcessedWebsites = websites;
            TempData[WebsitesKey] = JsonSerializer.Serialize(websites);
            return RedirectToAction(nameof(Report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing uploaded file");
            TempData["Error"] = $"Error processing the file: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("api/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ApiUpload([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Please upload a CSV file");
        }

        try
        {
            var websites = await ProcessUploadedFile(file);
            _lastProcessedWebsites = websites;
            return Ok(new { 
                message = "File processed successfully",
                reportUrl = Url.Action(nameof(Report), "Home", null, Request.Scheme),
                websites = websites
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing uploaded file through API");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<List<Website>> ProcessUploadedFile(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        return await _websiteParserService.ParseWebsitesFromCsvAsync(stream);
    }

    public IActionResult Report()
    {
        var websites = _lastProcessedWebsites;
        
        if (websites == null)
        {
            var websitesJson = TempData[WebsitesKey] as string;
            if (string.IsNullOrEmpty(websitesJson))
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                websites = JsonSerializer.Deserialize<List<Website>>(websitesJson);
                _lastProcessedWebsites = websites;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing websites data");
                TempData["Error"] = "Error loading report data";
                return RedirectToAction(nameof(Index));
            }
        }

        // Keep the data available for the next request
        if (TempData.ContainsKey(WebsitesKey))
        {
            TempData.Keep(WebsitesKey);
        }

        return View(websites);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
