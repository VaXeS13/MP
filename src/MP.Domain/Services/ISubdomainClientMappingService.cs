using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MP.Domain.Services
{
    public interface ISubdomainClientMappingService
    {
        Task<string?> GetClientIdForSubdomainAsync(string subdomain);
        Task<SubdomainClientInfo?> GetClientInfoForSubdomainAsync(string subdomain);
        string? ExtractSubdomainFromOrigin(string origin);
        bool IsValidSubdomain(string subdomain);
    }

    public class SubdomainClientInfo
    {
        public required string ClientId { get; set; }
        public string? DisplayName { get; set; }
        public string? RedirectUri { get; set; }
        public string? PostLogoutRedirectUri { get; set; }
        public bool IsActive { get; set; }
    }
}
