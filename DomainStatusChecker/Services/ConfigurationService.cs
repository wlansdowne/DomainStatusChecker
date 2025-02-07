using System.Text.Json;
using DomainStatusChecker.Models;

namespace DomainStatusChecker.Services;

public interface IConfigurationService
{
    List<string> GetSubnets();
    void SaveSubnets(List<string> subnets);
    List<string> GetCdnOrganizations();
    void SaveCdnOrganizations(List<string> organizations);
}

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ConfigurationService> _logger;
    private const string SubnetsSection = "AppSettings:Subnets";
    private const string CdnOrganizationsSection = "AppSettings:CdnOrganizations";
    private const string ConfigFileName = "appsettings.json";

    public ConfigurationService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<ConfigurationService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public List<string> GetSubnets()
    {
        try
        {
            var subnets = _configuration.GetSection(SubnetsSection).Get<List<string>>();
            return subnets ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subnets from configuration");
            return new List<string>();
        }
    }

    public List<string> GetCdnOrganizations()
    {
        try
        {
            var organizations = _configuration.GetSection(CdnOrganizationsSection).Get<List<string>>();
            return organizations ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CDN organizations from configuration");
            return new List<string>();
        }
    }

    public void SaveSubnets(List<string> subnets)
    {
        SaveConfiguration(SubnetsSection, subnets);
    }

    public void SaveCdnOrganizations(List<string> organizations)
    {
        SaveConfiguration(CdnOrganizationsSection, organizations);
    }

    private void SaveConfiguration<T>(string section, T value)
    {
        try
        {
            var configPath = Path.Combine(_environment.ContentRootPath, ConfigFileName);
            var jsonString = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize configuration file");
            }

            var sectionParts = section.Split(':');
            var currentSection = config;

            // Navigate through nested sections
            for (int i = 0; i < sectionParts.Length - 1; i++)
            {
                var part = sectionParts[i];
                if (!currentSection.ContainsKey(part))
                {
                    currentSection[part] = new Dictionary<string, object>();
                }
                currentSection = (Dictionary<string, object>)currentSection[part];
            }

            // Update the value
            var lastPart = sectionParts[^1];
            currentSection[lastPart] = value;

            // Save back to file
            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = JsonSerializer.Serialize(config, options);
            File.WriteAllText(configPath, updatedJson);

            _logger.LogInformation("Configuration saved successfully: {Section}", section);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration: {Section}", section);
            throw;
        }
    }
}