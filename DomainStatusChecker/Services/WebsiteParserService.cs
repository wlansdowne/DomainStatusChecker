using System.Text.RegularExpressions;
using DomainStatusChecker.Models;

namespace DomainStatusChecker.Services;

public interface IWebsiteParserService
{
    Task<List<Website>> ParseWebsitesFromCsvAsync(Stream csvStream);
}

public class WebsiteParserService : IWebsiteParserService
{
    private readonly ILogger<WebsiteParserService> _logger;
    private readonly IDomainStatusService _domainStatusService;
    private static readonly SemaphoreSlim _processingLimiter = new SemaphoreSlim(10); // Max 10 concurrent domain checks

    public WebsiteParserService(
        ILogger<WebsiteParserService> logger,
        IDomainStatusService domainStatusService)
    {
        _logger = logger;
        _domainStatusService = domainStatusService;
    }

    public async Task<List<Website>> ParseWebsitesFromCsvAsync(Stream csvStream)
    {
        var websites = new List<Website>();
        var processedCount = 0;
        var totalLines = 0;

        try
        {
            using var reader = new StreamReader(csvStream);
            var lines = new List<string>();

            // First pass: count lines and filter out headers/empty lines
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line) && 
                    !line.StartsWith("Site Name", StringComparison.OrdinalIgnoreCase) &&
                    !line.StartsWith("="))
                {
                    lines.Add(line);
                    totalLines++;
                }
            }

            // Process websites with concurrent limiting
            var tasks = new List<Task>();
            foreach (var websiteLine in lines)
            {
                tasks.Add(ProcessWebsiteLineAsync(websiteLine, websites, () =>
                {
                    processedCount++;
                    var progress = (double)processedCount / totalLines * 100;
                    _logger.LogInformation("Processing progress: {Progress:F1}% ({Current}/{Total})",
                        progress, processedCount, totalLines);
                }));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CSV file");
            throw;
        }

        return websites.OrderBy(w => w.Status == "STOPPED")
                      .ThenBy(w => w.Name)
                      .ToList();
    }

    private async Task ProcessWebsiteLineAsync(string line, List<Website> websites, Action onProcessed)
    {
        try
        {
            _logger.LogInformation("Processing line: {Line}", line);

            var website = ParseWebsiteLine(line);
            if (website == null)
            {
                return;
            }

            await _processingLimiter.WaitAsync();
            try
            {
                if (website.Status?.Equals("STARTED", StringComparison.OrdinalIgnoreCase) == true &&
                    !string.IsNullOrEmpty(website.Host) &&
                    !website.Host.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Checking domain status for: {Domain}", website.Host);
                    website.DomainStatus = await _domainStatusService.CheckDomainStatusAsync(website.Host);
                }
                else
                {
                    website.DomainStatus = "N/A";
                }

                lock (websites)
                {
                    websites.Add(website);
                }
            }
            finally
            {
                _processingLimiter.Release();
                onProcessed();
            }

            _logger.LogInformation("Added website: {Name} with status: {Status}", website.Name, website.DomainStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing line: {Line}", line);
        }
    }

    private Website? ParseWebsiteLine(string line)
    {
        try
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                return null;
            }

            var website = new Website();
            var currentPart = 0;
            var nameParts = new List<string>();

            // Parse name until we hit a status
            while (currentPart < parts.Length &&
                   !parts[currentPart].Equals("STARTED", StringComparison.OrdinalIgnoreCase) &&
                   !parts[currentPart].Equals("STOPPED", StringComparison.OrdinalIgnoreCase))
            {
                nameParts.Add(parts[currentPart]);
                currentPart++;
            }

            if (currentPart >= parts.Length)
            {
                return null;
            }

            website.Name = string.Join(" ", nameParts).Trim();
            website.Status = parts[currentPart++];

            // Parse remaining fields
            if (currentPart < parts.Length)
            {
                website.IP = parts[currentPart++];
            }

            if (currentPart < parts.Length && int.TryParse(parts[currentPart], out var port))
            {
                website.Port = port;
                currentPart++;
            }

            if (currentPart < parts.Length)
            {
                website.Host = parts[currentPart];
            }

            return website;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing line: {Line}", line);
            return null;
        }
    }
}