namespace DomainStatusChecker.Models;

public class Website
{
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? IP { get; set; }
    public int? Port { get; set; }
    public string? Host { get; set; }
    public string? DomainStatus { get; set; }
    public List<string> Nameservers { get; set; } = new();
}

public class WebsiteConfiguration
{
    public List<string> Subnets { get; set; } = new();
    public List<string> CdnOrganizations { get; set; } = new();
}