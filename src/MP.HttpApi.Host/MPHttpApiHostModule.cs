
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Server.AspNetCore;
using MP.MultiTenancy;
using MP.HealthChecks;
using Microsoft.OpenApi.Models;
using Volo.Abp;
using Volo.Abp.Studio;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic.Bundling;
using Microsoft.AspNetCore.Hosting;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Studio.Client.AspNetCore;
using Volo.Abp.Security.Claims;
using MP.EntityFrameworkCore;
using MP.Domain;
using MP.Domain.OrganizationalUnits;
using Volo.Abp.MultiTenancy;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using MP.Middleware;
using MP.HttpApi.Hubs;
using MP.HttpApi.Middleware;
using MP.HttpApi.Devices;
using MP.HttpApi.Host.Middleware;
using MP.Services;
using MP.Application.Contracts.Devices;
// DODAJ TE IMPORTY:
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Extensions;
using MP.Domain.MultiTenancy;
using MP.Domain.Payments;
using MP.Application.Payments;
using Hangfire;
using Hangfire.SqlServer;

namespace MP;

[DependsOn(
    typeof(MPHttpApiModule),
    typeof(AbpStudioClientAspNetCoreModule),
    typeof(AbpAspNetCoreMvcUiBasicThemeModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMultiTenancyModule),
    typeof(MPApplicationModule),
    typeof(MPEntityFrameworkCoreModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAspNetCoreSerilogModule)
    )]
public class MPHttpApiHostModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("MP");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        // USU� b��dne w�a�ciwo�ci - zostaw standardow� konfiguracj� ABP
        if (!hostingEnvironment.IsDevelopment())
        {
            PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
            {
                options.AddDevelopmentEncryptionAndSigningCertificate = false;
            });

            PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
            {
                serverBuilder.AddProductionEncryptionAndSigningCertificate("openiddict.pfx", configuration["AuthServer:CertificatePassPhrase"]!);
                serverBuilder.SetIssuer(new Uri(configuration["AuthServer:Authority"]!));
            });
        }
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (!configuration.GetValue<bool>("App:DisablePII"))
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        }

        if (!configuration.GetValue<bool>("AuthServer:RequireHttpsMetadata"))
        {
            Configure<OpenIddictServerAspNetCoreOptions>(options =>
            {
                options.DisableTransportSecurityRequirement = true;
            });

            Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
            });
        }

        ConfigureMultiTenancy(); 
        ConfigureAuthentication(context);
        ConfigureUrls(configuration); // DODAJ T� LINI�
        ConfigureBundles();
        ConfigureConventionalControllers();
        ConfigureHealthChecks(context);
        ConfigureSwagger(context, configuration);

        // POPRAWIONA konfiguracja tenant resolvers
       

        ConfigureVirtualFileSystem(context);
        ConfigureCors(context, configuration);
        ConfigurePaymentServices(context, configuration);
        ConfigureHangfire(context, configuration);
        ConfigureSignalR(context);
        ConfigureRemoteDeviceProxy(context);

        // POPRAWIONA konfiguracja cookies z subdomain-aware authentication
        // ConfigureSubdomainAwareAuthentication(context);

        // Register organizational unit context
        context.Services.AddScoped<ICurrentOrganizationalUnit, CurrentOrganizationalUnit>();

        // Session
        context.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });
    }

    // Helper method dla nazw cookies
    private static string GetAuthCookieName(string subdomain)
    {
        return string.IsNullOrEmpty(subdomain)
            ? ".AspNetCore.Cookies"
            : $".AspNetCore.Cookies.{subdomain.ToUpperInvariant()}";
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                BasicThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );

            options.ScriptBundles.Configure(
                BasicThemeBundles.Scripts.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-scripts.js");
                }
            );
        });
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<MPDomainSharedModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}MP.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<MPDomainModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}MP.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<MPApplicationContractsModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}MP.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<MPApplicationModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}MP.Application"));
            });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(MPApplicationModule).Assembly);
        });
    }

    private static void ConfigureSwagger(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGenWithOidc(
            configuration["AuthServer:Authority"]!,
            ["MP"],
            [AbpSwaggerOidcFlows.AuthorizationCode],
            null,
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "MP API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.RemovePostFix("/"))
                            .ToArray()
                    )
                    .SetIsOriginAllowedToAllowWildcardSubdomains() // Wa�ne dla subdomen
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowed((string _) => true) // Dla developmentu
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }
    private void ConfigureMultiTenancy()
    {
        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = true;
        });

        Configure<AbpTenantResolveOptions>(options =>
        {
            // Wyczy�� wszystkie domy�lne resolvery
            options.TenantResolvers.Clear();

            // Dodaj TYLKO nasz resolver
            options.TenantResolvers.Add(new SubdomainTenantResolveContributor());
        });
    }

    private void ConfigureHealthChecks(ServiceConfigurationContext context)
    {
        context.Services.AddMPHealthChecks();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        app.UseForwardedHeaders();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseRouting();
        app.MapAbpStaticAssets();
        app.UseAbpStudioLink();
        // Enhanced XSS protection - configure ABP security headers instead of custom ones
        app.UseAbpSecurityHeaders();
        app.UseCors();

        // KRYTYCZNA KOLEJNO�� MIDDLEWARE - subdomen detection PRZED authentication
        app.UseMiddleware<SubdomainDetectionMiddleware>(); // Wykrywanie subdomeny

        // Agent API Key authentication for SignalR connections
        app.UseMiddleware<AgentAuthenticationMiddleware>();

        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }
        app.UseMiddleware<TenantMiddleware>(); // Izolacja cookies
        app.UseMiddleware<OrganizationalUnitMiddleware>(); // Organizational unit context resolution

        // Twoje zakomentowane middleware - pozostaw jak s�
        //app.UseMiddleware<ClientIdToTenantMiddleware>();
        //app.UseMiddleware<DynamicTenantUrlMiddleware>();

        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAuthorization();

        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "MP API");

            var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            options.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
        });

        app.UseAuditing();
        app.UseAbpSerilogEnrichers();

        // Configure Hangfire Dashboard BEFORE UseConfiguredEndpoints
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
        });

        app.UseConfiguredEndpoints(endpoints =>
        {
            endpoints.MapHangfireDashboard();

            // Map SignalR hubs
            endpoints.MapHub<MP.Hubs.NotificationHub>("/signalr-hubs/notifications");
            endpoints.MapHub<MP.Hubs.DashboardHub>("/signalr-hubs/dashboard");
            endpoints.MapHub<MP.Hubs.BoothHub>("/signalr-hubs/booths");
            endpoints.MapHub<MP.Hubs.SalesHub>("/signalr-hubs/sales");
            endpoints.MapHub<MP.Hubs.ChatHub>("/signalr-hubs/chat");
            endpoints.MapHub<LocalAgentHub>("/hubs/localAgent");
        });

        // Register recurring job for P24 payment status checks
        // Runs every 15 minutes
        RecurringJob.AddOrUpdate<P24StatusCheckRecurringJob>(
            "p24-status-check",
            job => job.ExecuteAsync(),
            "*/15 * * * *"); // Every 15 minutes

        // Register recurring job for PayPal payment status checks
        // Runs every 15 minutes
        RecurringJob.AddOrUpdate<PayPalStatusCheckRecurringJob>(
            "paypal-status-check",
            job => job.ExecuteAsync(),
            "*/15 * * * *"); // Every 15 minutes

        // Register recurring job for Stripe payment status checks
        // Runs every 15 minutes
        RecurringJob.AddOrUpdate<StripeStatusCheckRecurringJob>(
            "stripe-status-check",
            job => job.ExecuteAsync(),
            "*/15 * * * *"); // Every 15 minutes

        // Register daily booth status synchronization job
        // Runs every day at 00:05 AM to sync booth statuses with rental periods
        RecurringJob.AddOrUpdate<DailyBoothStatusSyncJob>(
            "daily-booth-status-sync",
            job => job.ExecuteAsync(),
            "5 0 * * *"); // Every day at 00:05 AM

        // Register daily rental status synchronization job
        // Runs every day at 00:01 AM to automatically expire rentals that have passed their end date
        RecurringJob.AddOrUpdate<DailyRentalStatusSyncJob>(
            "daily-rental-status-sync",
            job => job.ExecuteAsync(),
            "1 0 * * *"); // Every day at 00:01 AM
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.Applications["Angular"].RootUrl = configuration["App:AngularUrl"] ?? "http://localhost:4200";

            // Password reset URL
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";

            // RedirectAllowedUrls - dla subdomen
            options.RedirectAllowedUrls.Clear();

            // Development URLs
            options.RedirectAllowedUrls.Add("http://localhost:4200");
            options.RedirectAllowedUrls.Add("https://localhost:4200");
            options.RedirectAllowedUrls.Add("http://cto.localhost:4200");
            options.RedirectAllowedUrls.Add("http://kiss.localhost:4200");
            options.RedirectAllowedUrls.Add("https://cto.localhost:4200");
            options.RedirectAllowedUrls.Add("https://kiss.localhost:4200");

            // Dodaj z konfiguracji
            var corsOrigins = configuration["App:CorsOrigins"];
            if (!string.IsNullOrEmpty(corsOrigins))
            {
                var origins = corsOrigins
                    .Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(o => o.Trim().RemovePostFix("/"))
                    .ToArray();

                foreach (var origin in origins)
                {
                    options.RedirectAllowedUrls.Add(origin);
                }
            }
        });
    }
    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });

        context.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;

            // DODAJ EXPLICIT LOGIN PATH
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";

            // Enhanced login redirect handling
            options.Events.OnRedirectToLogin = context =>
            {
                var httpContext = context.HttpContext;
                var subdomain = httpContext.Items["ClientSubdomain"] as string;

                // Try to get tenant from query parameter if subdomain not detected
                if (string.IsNullOrEmpty(subdomain))
                {
                    subdomain = httpContext.Request.Query["__tenant"].FirstOrDefault();
                }

                // Extract tenant from ReturnUrl if still not found
                if (string.IsNullOrEmpty(subdomain))
                {
                    var returnUrl = httpContext.Request.Query["ReturnUrl"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        var decodedReturnUrl = System.Net.WebUtility.UrlDecode(returnUrl);
                        var match = System.Text.RegularExpressions.Regex.Match(decodedReturnUrl, @"redirect_uri=([^&]+)");
                        if (match.Success)
                        {
                            var redirectUri = System.Net.WebUtility.UrlDecode(match.Groups[1].Value);
                            subdomain = ExtractSubdomainForLogin(redirectUri);
                        }
                    }
                }

                // Get the original return URL from the request or use the current URL
                var originalReturnUrl = httpContext.Request.Query["ReturnUrl"].FirstOrDefault() ??
                                      httpContext.Request.GetEncodedUrl();

                var loginPath = "/Account/Login";

                // DON'T add __tenant to login URL - it causes GUID parsing issues
                // The tenant context is already resolved by middleware and tenant resolver

                if (!string.IsNullOrEmpty(originalReturnUrl))
                {
                    var separator = loginPath.Contains("?") ? "&" : "?";
                    loginPath += $"{separator}ReturnUrl={System.Net.WebUtility.UrlEncode(originalReturnUrl)}";
                }

                Console.WriteLine($"Redirecting to login without __tenant parameter: {loginPath}");
                Console.WriteLine($"Tenant context maintained through middleware: {subdomain}");
                context.Response.Redirect(loginPath);
                return Task.CompletedTask;
            };

            options.Events.OnSignedIn = context =>
            {
                Console.WriteLine($"User signed in successfully in tenant context");
                return Task.CompletedTask;
            };
        });

        // Configure External Authentication Providers
        ConfigureExternalAuthenticationProviders(context, configuration);
    }

    private void ConfigureExternalAuthenticationProviders(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var services = context.Services;

        // Google Authentication
        var googleClientId = configuration["Authentication:Google:ClientId"];
        var googleClientSecret = configuration["Authentication:Google:ClientSecret"];

        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            services.AddAuthentication()
                .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                });
        }

        // Facebook Authentication
        var facebookAppId = configuration["Authentication:Facebook:AppId"];
        var facebookAppSecret = configuration["Authentication:Facebook:AppSecret"];

        if (!string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
        {
            services.AddAuthentication()
                .AddFacebook(FacebookDefaults.AuthenticationScheme, options =>
                {
                    options.AppId = facebookAppId;
                    options.AppSecret = facebookAppSecret;
                });
        }
    }

    // Helper method for extracting subdomain during login flow
    private static string ExtractSubdomainForLogin(string url)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var host = uri.Host;

                // Development: *.localhost
                if (host.Contains(".localhost"))
                {
                    var parts = host.Split('.');
                    if (parts.Length >= 2 && parts[0] != "www")
                    {
                        return parts[0];
                    }
                }
                // Production: *.mp.com
                else if (host.Contains(".mp.com") && host != "mp.com" && host != "www.mp.com")
                {
                    var parts = host.Split('.');
                    if (parts.Length >= 3 && parts[0] != "www")
                    {
                        return parts[0];
                    }
                }
            }
        }
        catch (Exception)
        {
            // Ignore parsing errors
        }
        return null;
    }

    private void ConfigurePaymentServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var services = context.Services;

        // HttpClient dla Przelewy24
        services.AddHttpClient<IPrzelewy24Service, Przelewy24Service>();

        // Rejestracja serwisu Przelewy24
        services.AddTransient<IPrzelewy24Service, Przelewy24Service>();
    }

    private void ConfigureHangfire(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var services = context.Services;

        // Add Hangfire services using SQL Server storage
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("Default"), new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                // Explicitly use Microsoft.Data.SqlClient provider
                PrepareSchemaIfNecessary = true,
                SchemaName = "HangFire"
            }));

        // Add the processing server as IHostedService
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
            options.ServerName = "MP-Payment-Server";
        });
    }

    private void ConfigureSignalR(ServiceConfigurationContext context)
    {
        context.Services.AddSignalR(options =>
        {
            // Enable detailed errors in development
            options.EnableDetailedErrors = context.Services.GetHostingEnvironment().IsDevelopment();

            // Keep alive settings for better connection management
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);

            // Max message size (1MB)
            options.MaximumReceiveMessageSize = 1024 * 1024;
        });

        // Register agent management services
        context.Services.AddTransient<MP.HttpApi.Hubs.IAgentConnectionManager, MP.Services.AgentConnectionManager>();
        context.Services.AddTransient<MP.HttpApi.Hubs.IAgentCommandProcessor, MP.Services.AgentCommandProcessor>();
    }

    private void ConfigureRemoteDeviceProxy(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // Register Remote Device Proxy for communication with local agents via SignalR
        services.AddTransient<IRemoteDeviceProxy, SignalRDeviceProxy>();

        // Configure Remote Device Proxy options
        services.Configure<RemoteDeviceProxyOptions>(options =>
        {
            // Command timeout for remote device operations
            options.CommandTimeout = TimeSpan.FromSeconds(30);

            // Retry configuration
            options.MaxRetries = 3;
            options.RetryDelay = TimeSpan.FromSeconds(2);

            // Offline queue configuration for critical operations
            options.EnableOfflineQueue = true;
            options.MaxQueuedCommands = 1000;

            // Circuit breaker configuration
            options.CircuitBreakerFailureThreshold = 5;
            options.CircuitBreakerResetTimeSeconds = 60;
        });
    }
}

