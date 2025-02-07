namespace DomainStatusChecker.Models;

public class SettingsViewModel
{
    public List<string> Subnets { get; set; } = new();
    public List<string> CdnOrganizations { get; set; } = new();
    public List<string> CdnProviders { get; set; } = new();  // Alias for CdnOrganizations
    public string? NewSubnet { get; set; }
    public string? NewCdnProvider { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}