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
    private static readonly SemaphoreSlim _processingLimiter = new SemaphoreSlim(20);
    private const int BatchSize = 25; // Reduced batch size
    private const int ChunkSize = 100; // Number of lines to read at once

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
            var currentChunk = new List<string>(ChunkSize);
            string? line;

            while (!reader.EndOfStream)
            {
                // Process CSV in chunks to avoid memory issues
                currentChunk.Clear();
                var chunkCount = 0;

                // Read a chunk of lines
                while (chunkCount < ChunkSize && (line = await reader.ReadLineAsync()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line) && 
                        !line.StartsWith("Site Name", StringComparison.OrdinalIgnoreCase) &&
                        !line.StartsWith("="))
                    {
                        currentChunk.Add(line);
                        totalLines++;
                    }
                    chunkCount++;
                }

                // Process the current chunk in batches
                for (int i = 0; i < currentChunk.Count; i += BatchSize)
                {
                    var batch = currentChunk.Skip(i).Take(BatchSize);
                    var batchTasks = batch.Select(websiteLine =>
                        ProcessWebsiteLineAsync(websiteLine, websites, () =>
                        {
                            processedCount++;
                            var progress = (double)processedCount / totalLines * 100;
                            _logger.LogInformation("Processing progress: {Progress:F1}% ({Current}/{Total})",
                                progress, processedCount, totalLines);
                        }));

                    try
                    {
                        // Process batch with timeout
                        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                        await Task.WhenAll(batchTasks).WaitAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Batch processing timeout. Moving to next batch.");
                        continue;
                    }

                    // Add small delay between batches to prevent overwhelming
                    await Task.Delay(100);
                }
            }
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
                    
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    try
                    {
                        website.DomainStatus = await _domainStatusService.CheckDomainStatusAsync(website.Host)
                            .WaitAsync(TimeSpan.FromSeconds(30), cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        website.DomainStatus = "Timeout";
                        _logger.LogWarning("Domain check timeout for: {Domain}", website.Host);
                    }
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