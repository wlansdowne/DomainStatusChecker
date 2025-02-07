namespace DomainStatusChecker.Models;

public class SettingsViewModel
{
    public List<string> Subnets { get; set; } = new();
    public List<string> CdnOrganizations { get; set; } = new();
}