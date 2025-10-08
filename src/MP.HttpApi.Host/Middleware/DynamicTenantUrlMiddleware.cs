using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.ApplicationConfigurations;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Threading;
using Volo.Abp.UI.Navigation.Urls;

namespace MP.Middleware
{
    public class DynamicTenantUrlMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private static readonly object _lock = new object();
        private static bool _urlsInitialized = false;
        private readonly ITenantRepository _tenantRepository;

        public DynamicTenantUrlMiddleware(RequestDelegate next, IConfiguration configuration, ITenantRepository tenantRepository)
        {
            _next = next;
            _configuration = configuration;
            _tenantRepository = tenantRepository;
        }

        public async Task InvokeAsync(HttpContext context,
            ITenantRepository tenantRepository,
            IOptionsSnapshot<AppUrlOptions> appUrlOptions,
            ICurrentTenant currentTenant)
        {
            // Inicjalizuj URL-e tylko raz
            if (!_urlsInitialized)
            {
                lock (_lock)
                {
                    if (!_urlsInitialized)
                    {
                        // Użyj AsyncHelper.RunSync zamiast await w lock
                        AsyncHelper.RunSync(() => InitializeTenantUrls(tenantRepository, appUrlOptions.Value));
                        _urlsInitialized = true;
                    }
                }
            }

            // Ustaw dynamiczny Angular URL dla bieżącego tenant'a
            await SetCurrentTenantUrl(context, appUrlOptions.Value, currentTenant);

            await _next(context);
        }

        private async Task InitializeTenantUrls(ITenantRepository tenantRepository, AppUrlOptions appUrlOptions)
        {
            try
            {
                var angularBaseUrl = _configuration["App:AngularUrl"]; // localhost:4200
                var tenants = await tenantRepository.GetListAsync();

                // Dodaj bazowy URL (host tenant)
                var baseUrl = $"http://{angularBaseUrl}";
                if (!appUrlOptions.RedirectAllowedUrls.Contains(baseUrl))
                {
                    appUrlOptions.RedirectAllowedUrls.Add(baseUrl);
                }

                // Dodaj URL dla każdego tenant'a
                foreach (var tenant in tenants)
                {
                    var tenantName = tenant.Name.ToLowerInvariant();
                    var tenantUrl = $"http://{tenantName}.{angularBaseUrl}";

                    if (!appUrlOptions.RedirectAllowedUrls.Contains(tenantUrl))
                    {
                        appUrlOptions.RedirectAllowedUrls.Add(tenantUrl);
                    }

                    // Dla produkcji dodaj HTTPS
                    if (!angularBaseUrl.Contains("localhost"))
                    {
                        var httpsUrl = $"https://{tenantName}.{angularBaseUrl}";
                        if (!appUrlOptions.RedirectAllowedUrls.Contains(httpsUrl))
                        {
                            appUrlOptions.RedirectAllowedUrls.Add(httpsUrl);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log błąd - może baza nie jest jeszcze dostępna
                Console.WriteLine($"Failed to initialize tenant URLs: {ex.Message}");
            }
        }

        private async Task SetCurrentTenantUrl(HttpContext context, AppUrlOptions appUrlOptions, ICurrentTenant currentTenant)
        {
            // Sprawdź, czy to request związany z logowaniem/autoryzacją
            if (context.Request.Path.StartsWithSegments("/connect/authorize") ||
                context.Request.Path.StartsWithSegments("/Account"))
            {
                // Pobierz ReturnUrl z zapytania
                var returnUrl = context.Request.Query["ReturnUrl"].FirstOrDefault();

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    // Upewnij się, że ReturnUrl jest pełnym URL (dodaj protokół i domenę)
                    if (!returnUrl.StartsWith("http://") && !returnUrl.StartsWith("https://"))
                    {
                        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
                        returnUrl = baseUrl + returnUrl;
                    }

                    var angularBaseUrl = _configuration["App:AngularUrl"];
                    try
                    {
                        // Parsowanie ReturnUrl, aby wydobyć client_id
                        var uri = new Uri(returnUrl);
                        var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
                        var clientId = queryParams["client_id"].FirstOrDefault();

                        if (!string.IsNullOrEmpty(clientId) && clientId.StartsWith("MP_App_"))
                        {
                            var tenantName = clientId["MP_App_".Length..];

                            if (!string.IsNullOrEmpty(tenantName))
                            {
                                var tenant = await _tenantRepository.FindByNameAsync(tenantName);
                                if (tenant != null)
                                {
                                    // Ustaw tenant po ID
                                    currentTenant.Change(tenant.Id, tenant.NormalizedName);
                                }
                            }
                            var tenantAngularUrl = $"http://{tenantName}.{angularBaseUrl}";

                            // Ustaw Angular URL dla tego tenant'a
                            appUrlOptions.Applications["Angular"].RootUrl = tenantAngularUrl;
                        }
                        else if (clientId == "MP_App")
                        {
                            // Tenant domyślny (host)
                            appUrlOptions.Applications["Angular"].RootUrl = $"http://{angularBaseUrl}";
                        }
                    }
                    catch (UriFormatException ex)
                    {
                        // Obsługa wyjątku, jeśli URL jest nadal nieprawidłowy

                        Console.WriteLine("Nieprawidłowy format URL w ReturnUrl: " + ex.Message);
                        // Można dodać odpowiednią obsługę błędu (np. zwrócenie jakiejś strony błędu)
                    }
                }
            }
        }

    }
}