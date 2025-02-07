namespace DomainStatusChecker.Models;

public class SettingsViewModel
{
    public List<string> Subnets { get; set; } = new();
    public string? NewSubnet { get; set; }
    public List<string> CdnProviders { get; set; } = new();
    public string? NewCdnProvider { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}

public class SubnetViewModel
{
    public string Cidr { get; set; } = string.Empty;
}

public class CdnProviderViewModel
{
    public string Name { get; set; } = string.Empty;
}