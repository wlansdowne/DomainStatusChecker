using System.Net;
using System.Net.Sockets;
using DomainStatusChecker.Models;
using System.Text.Json;
using System.Net.Http.Json;
using DnsClient;

namespace DomainStatusChecker.Services;

public interface IDomainStatusService
{
    Task<string> CheckDomainStatusAsync(string domain);
    Task<List<string>> GetNameserversAsync(string domain);
    bool IsIpInSubnets(string ipAddress);
}

public class DomainStatusService : IDomainStatusService
{
    private readonly ILogger<DomainStatusService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfigurationService _configService;
    private readonly IConfiguration _configuration;
    private static readonly TimeSpan _dnsTimeout = TimeSpan.FromSeconds(5);
    private readonly LookupClient _dnsClient;

    public DomainStatusService(
        ILogger<DomainStatusService> logger,
        IHttpClientFactory httpClientFactory,
        IConfigurationService configService,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _configService = configService;
        _configuration = configuration;
        
        // Configure DNS client with timeout
        var options = new LookupClientOptions
        {
            UseCache = true,
            Timeout = _dnsTimeout,
            UseTcpOnly = false,
            UseRandomNameServer = true
        };
        _dnsClient = new LookupClient(options);
    }

    public async Task<string> CheckDomainStatusAsync(string domain)
    {
        try
        {
            _logger.LogInformation("Checking domain status for: {Domain}", domain);
            
            var ipAddresses = await GetHostAddressesWithTimeoutAsync(domain);
            if (ipAddresses == null || !ipAddresses.Any())
            {
                _logger.LogInformation("No IPv4 addresses found for domain: {Domain}", domain);
                return "Not Found";
            }

            _logger.LogInformation("Resolved IPs for {Domain}: {IPs}", domain, string.Join(", ", ipAddresses));

            foreach (var ip in ipAddresses)
            {
                if (IsIpInSubnets(ip))
                {
                    _logger.LogInformation("IP {IP} found in configured subnets", ip);
                    return "Alive";
                }

                var cdnResult = await CheckCdnWithRateLimitAsync(ip);
                if (cdnResult.isCdn)
                {
                    _logger.LogInformation("CDN detected for IP {IP}: {CDN}", ip, cdnResult.cdnName);
                    return $"CDN Protected ({cdnResult.cdnName})";
                }
            }

            return "Resolves Elsewhere";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking domain status for {Domain}", domain);
            if (ex is SocketException || ex.InnerException is SocketException)
            {
                return "Not Found";
            }
            return "DNS Error";
        }
    }

    public async Task<List<string>> GetNameserversAsync(string domain)
    {
        try
        {
            var result = await _dnsClient.QueryAsync(domain, QueryType.NS);
            var nameservers = result.Answers
                .NsRecords()
                .Select(record => record.NSDName.Value.TrimEnd('.'))
                .OrderBy(ns => ns)
                .ToList();

            _logger.LogInformation("Found nameservers for {Domain}: {Nameservers}", 
                domain, string.Join(", ", nameservers));

            return nameservers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting nameservers for {Domain}", domain);
            return new List<string>();
        }
    }

    private async Task<List<string>> GetHostAddressesWithTimeoutAsync(string domain)
    {
        try
        {
            var result = await _dnsClient.QueryAsync(domain, QueryType.A);
            return result.Answers
                .ARecords()
                .Select(record => record.Address.ToString())
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DNS lookup for domain: {Domain}", domain);
            return new List<string>();
        }
    }

    public bool IsIpInSubnets(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out var ip))
        {
            return false;
        }

        var subnets = _configService.GetSubnets();
        foreach (var subnet in subnets)
        {
            try
            {
                var parts = subnet.Split('/');
                if (parts.Length != 2) continue;

                var networkAddress = IPAddress.Parse(parts[0]);
                var prefixLength = int.Parse(parts[1]);
                
                if (IsIpInRange(ip, networkAddress, prefixLength))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subnet {Subnet}", subnet);
            }
        }

        return false;
    }

    private bool IsIpInRange(IPAddress ip, IPAddress networkAddress, int prefixLength)
    {
        var ipBytes = ip.GetAddressBytes();
        var networkBytes = networkAddress.GetAddressBytes();
        
        var wholeBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var i = 0; i < wholeBytes; i++)
        {
            if (ipBytes[i] != networkBytes[i])
            {
                return false;
            }
        }

        if (remainingBits > 0 && wholeBytes < ipBytes.Length)
        {
            var mask = (byte)(0xFF << (8 - remainingBits));
            return (ipBytes[wholeBytes] & mask) == (networkBytes[wholeBytes] & mask);
        }

        return true;
    }

    private async Task<(bool isCdn, string cdnName)> CheckCdnWithRateLimitAsync(string ipAddress)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IpApiResponse>(
                $"http://ip-api.com/json/{ipAddress}?fields=org,status");

            if (response?.Status == "success" && !string.IsNullOrEmpty(response.Org))
            {
                var cdnOrganizations = _configuration.GetSection("AppSettings:CdnOrganizations").Get<List<string>>() ?? new();
                foreach (var cdn in cdnOrganizations)
                {
                    if (response.Org.Contains(cdn, StringComparison.OrdinalIgnoreCase))
                    {
                        return (true, cdn);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking CDN for IP {IP}", ipAddress);
        }

        return (false, string.Empty);
    }

    private class IpApiResponse
    {
        public string Status { get; set; } = string.Empty;
        public string Org { get; set; } = string.Empty;
    }
}