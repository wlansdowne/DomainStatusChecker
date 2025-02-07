using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace DomainStatusChecker.Services;

public interface IConfigurationService
{
    List<string> GetSubnets();
    void SaveSubnets(List<string> subnets);
    List<string> GetCdnProviders();
    void SaveCdnProviders(List<string> providers);
    bool ValidateCidr(string cidr);
}

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly string _appSettingsPath;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IWebHostEnvironment _env;
    private static readonly object _lock = new object();

    public ConfigurationService(
        IConfiguration configuration, 
        IWebHostEnvironment env,
        ILogger<ConfigurationService> logger)
    {
        _configuration = configuration;
        _env = env;
        _appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
        _logger = logger;
    }

    public List<string> GetSubnets()
    {
        try
        {
            var subnets = _configuration.GetSection("AppSettings:Subnets").Get<List<string>>();
            return subnets ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subnets from configuration");
            return new List<string>();
        }
    }

    public List<string> GetCdnProviders()
    {
        try
        {
            var providers = _configuration.GetSection("AppSettings:CdnOrganizations").Get<List<string>>();
            return providers ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CDN providers from configuration");
            return new List<string>();
        }
    }

    public void SaveSubnets(List<string> subnets)
    {
        if (!subnets.Any())
        {
            throw new ArgumentException("At least one subnet must be specified.");
        }

        if (subnets.Any(s => !ValidateCidr(s)))
        {
            throw new ArgumentException("One or more subnets are not in valid CIDR format.");
        }

        SaveConfiguration("Subnets", subnets);
    }

    public void SaveCdnProviders(List<string> providers)
    {
        if (!providers.Any())
        {
            throw new ArgumentException("At least one CDN provider must be specified.");
        }

        if (providers.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("CDN provider names cannot be empty.");
        }

        SaveConfiguration("CdnOrganizations", providers);
    }

    private void SaveConfiguration(string key, List<string> values)
    {
        lock (_lock)
        {
            try
            {
                // Read the current JSON file
                string jsonString;
                try
                {
                    jsonString = File.ReadAllText(_appSettingsPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading configuration file");
                    throw new Exception("Could not read configuration file.", ex);
                }

                // Parse the JSON
                JsonDocument jsonDoc;
                try
                {
                    jsonDoc = JsonDocument.Parse(jsonString);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error parsing configuration JSON");
                    throw new Exception("Invalid JSON in configuration file.", ex);
                }

                // Create a new configuration object
                var configObj = new Dictionary<string, object>();

                // Copy all existing properties
                foreach (var element in jsonDoc.RootElement.EnumerateObject())
                {
                    if (element.Name == "AppSettings")
                    {
                        var appSettings = new Dictionary<string, object>();
                        foreach (var setting in element.Value.EnumerateObject())
                        {
                            if (setting.Name == key)
                            {
                                appSettings[key] = values;
                            }
                            else
                            {
                                appSettings[setting.Name] = JsonSerializer.Deserialize<object>(setting.Value.GetRawText());
                            }
                        }
                        configObj["AppSettings"] = appSettings;
                    }
                    else
                    {
                        configObj[element.Name] = JsonSerializer.Deserialize<object>(element.Value.GetRawText());
                    }
                }

                // Serialize the updated configuration
                var options = new JsonSerializerOptions { WriteIndented = true };
                var updatedJson = JsonSerializer.Serialize(configObj, options);

                try
                {
                    // Write to a temporary file first
                    var tempPath = Path.GetTempFileName();
                    File.WriteAllText(tempPath, updatedJson);

                    // Replace the original file
                    File.Copy(tempPath, _appSettingsPath, true);
                    File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error writing configuration file");
                    throw new Exception("Could not write configuration file.", ex);
                }

                // Reload configuration
                try
                {
                    if (_configuration is IConfigurationRoot configRoot)
                    {
                        configRoot.Reload();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reloading configuration");
                    throw new Exception("Could not reload configuration.", ex);
                }

                _logger.LogInformation("Successfully saved {Count} values to configuration key {Key}", values.Count, key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving configuration for key {Key}", key);
                throw new Exception($"Failed to save configuration: {ex.Message}", ex);
            }
        }
    }

    public bool ValidateCidr(string cidr)
    {
        if (string.IsNullOrWhiteSpace(cidr))
            return false;

        var parts = cidr.Split('/');
        if (parts.Length != 2)
            return false;

        // Validate IP address part
        if (!IPAddress.TryParse(parts[0], out _))
            return false;

        // Validate subnet mask part
        if (!int.TryParse(parts[1], out int maskBits))
            return false;

        return maskBits >= 0 && maskBits <= 32;
    }
}