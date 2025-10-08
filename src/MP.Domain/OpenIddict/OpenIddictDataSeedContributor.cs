using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenIddict.Abstractions;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.OpenIddict.Scopes;
using Volo.Abp.PermissionManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;

namespace MP.Domain.OpenIddict;

/* Creates initial data that is needed to property run the application
 * and make client-to-server communication possible.
 */
public class OpenIddictDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly IOpenIddictApplicationRepository _openIddictApplicationRepository;
    private readonly IAbpApplicationManager _applicationManager;
    private readonly IOpenIddictScopeRepository _openIddictScopeRepository;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IPermissionDataSeeder _permissionDataSeeder;
    private readonly IStringLocalizer<OpenIddictResponse> L;
    private readonly ITenantRepository _tenantRepository;

    public OpenIddictDataSeedContributor(
        IConfiguration configuration,
        IOpenIddictApplicationRepository openIddictApplicationRepository,
        IAbpApplicationManager applicationManager,
        IOpenIddictScopeRepository openIddictScopeRepository,
        IOpenIddictScopeManager scopeManager,
        IPermissionDataSeeder permissionDataSeeder,
        IStringLocalizer<OpenIddictResponse> l,
        ITenantRepository tenantRepository)
    {
        _configuration = configuration;
        _openIddictApplicationRepository = openIddictApplicationRepository;
        _applicationManager = applicationManager;
        _openIddictScopeRepository = openIddictScopeRepository;
        _scopeManager = scopeManager;
        _permissionDataSeeder = permissionDataSeeder;
        L = l;
        _tenantRepository = tenantRepository;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await CreateScopesAsync();
        //await CreateApplicationsAsync();
        await CreateTenantApplicationsAsync(context);
    }

    private async Task CreateScopesAsync()
    {
        if (await _openIddictScopeRepository.FindByNameAsync("MP") == null)
        {
            await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "MP",
                DisplayName = "MP API",
                Resources = { "MP" }
            });
        }
    }
    private async Task CreateTenantApplicationsAsync(DataSeedContext context)
    {
        if (!context.TenantId.HasValue)
        {
            return; // nie seedujemy jeśli brak tenanta
        }

        var tenant = await _tenantRepository.GetAsync(context.TenantId.Value);

        var commonScopes = new List<string>
    {
        OpenIddictConstants.Permissions.Scopes.Address,
        OpenIddictConstants.Permissions.Scopes.Email,
        OpenIddictConstants.Permissions.Scopes.Phone,
        OpenIddictConstants.Permissions.Scopes.Profile,
        OpenIddictConstants.Permissions.Scopes.Roles,
        "MP"
    };

        // Konfiguracja adresów (z placeholderem {0})
        var clientUrlTemplate = _configuration["App:ClientUrl"] ?? "http://{0}.localhost:4200";

        var baseUri = string.Format(clientUrlTemplate, tenant.Name);

        await CreateApplicationAsync(
            applicationType: OpenIddictConstants.ApplicationTypes.Web,
            name: $"MP_App_{tenant.Name}",
            type: OpenIddictConstants.ClientTypes.Public,
            consentType: OpenIddictConstants.ConsentTypes.Implicit,
            displayName: $"Angular Application ({tenant.Name})",
            secret: null,
            grantTypes: new List<string>
            {
            OpenIddictConstants.GrantTypes.AuthorizationCode,
            OpenIddictConstants.GrantTypes.RefreshToken
            },
            scopes: commonScopes,
            redirectUris: new List<string> { baseUri, $"{baseUri}/auth-callback" }, // callback Angulara
            postLogoutRedirectUris: new List<string> { baseUri },    // strona główna tenanta
            clientUri: baseUri,
            logoUri: "/images/clients/angular.svg"
        );
    }

    private async Task CreateApplicationsAsync()
    {
        var tenants = await _tenantRepository.GetListAsync();

        var commonScopes = new List<string>
            {
                OpenIddictConstants.Permissions.Scopes.Address,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Phone,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles,
                "MP"
            };

        // Pobieramy template z appsettings.json
        var clientUrlTemplate = _configuration["App:ClientUrl"] ?? "http://{0}.localhost:4200";

        var redirectUris = tenants
            .Select(t => clientUrlTemplate.Replace("{0}", t.Name))
            .ToList();

        // Tworzymy aplikację OpenIddict dla Angular / MP_App
        var clientId = _configuration["OpenIddict:Applications:MP_App:ClientId"] ?? "MP_App";
        var clientName = "Console Test / Angular Application";

        await CreateApplicationAsync(
            applicationType: OpenIddictConstants.ApplicationTypes.Web,
            name: clientId,
            type: OpenIddictConstants.ClientTypes.Public,
            consentType: OpenIddictConstants.ConsentTypes.Implicit,
            displayName: clientName,
            secret: null,
            grantTypes: new List<string>
            {
                    OpenIddictConstants.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.GrantTypes.RefreshToken
            },
            scopes: commonScopes,
            redirectUris: redirectUris,
            postLogoutRedirectUris: redirectUris,
            clientUri: null,
            logoUri: "/images/clients/angular.svg"
        );

        // Opcjonalnie dodaj Swagger Client
        var swaggerClientId = _configuration["OpenIddict:Applications:MP_Swagger:ClientId"];
        if (!swaggerClientId.IsNullOrWhiteSpace())
        {
            var swaggerRootUrl = _configuration["OpenIddict:Applications:MP_Swagger:RootUrl"]?.TrimEnd('/');
            await CreateApplicationAsync(
                applicationType: OpenIddictConstants.ApplicationTypes.Web,
                name: swaggerClientId!,
                type: OpenIddictConstants.ClientTypes.Public,
                consentType: OpenIddictConstants.ConsentTypes.Implicit,
                displayName: "Swagger Application",
                secret: null,
                grantTypes: new List<string> { OpenIddictConstants.GrantTypes.AuthorizationCode },
                scopes: commonScopes,
                redirectUris: new List<string> { $"{swaggerRootUrl}/swagger/oauth2-redirect.html" },
                postLogoutRedirectUris: new List<string> { $"{swaggerRootUrl}/swagger/oauth2-redirect.html" },
                clientUri: swaggerRootUrl.EnsureEndsWith('/') + "swagger",
                logoUri: "/images/clients/swagger.svg"
            );
        }
    }

    private async Task CreateApplicationAsync(
        [NotNull] string applicationType,
        [NotNull] string name,
        [NotNull] string type,
        [NotNull] string consentType,
        string displayName,
        string? secret,
        List<string> grantTypes,
        List<string> scopes,
        List<string>? redirectUris = null,
        List<string>? postLogoutRedirectUris = null,
        List<string>? permissions = null,
        string? clientUri = null,
        string? logoUri = null)
    {
        if (!string.IsNullOrEmpty(secret) && string.Equals(type, OpenIddictConstants.ClientTypes.Public,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(L["NoClientSecretCanBeSetForPublicApplications"]);
        }

        if (string.IsNullOrEmpty(secret) && string.Equals(type, OpenIddictConstants.ClientTypes.Confidential,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(L["TheClientSecretIsRequiredForConfidentialApplications"]);
        }

        var client = await _openIddictApplicationRepository.FindByClientIdAsync(name);

        var application = new AbpApplicationDescriptor
        {
            ApplicationType = applicationType,
            ClientId = name,
            ClientType = type,
            ClientSecret = secret,
            ConsentType = consentType,
            DisplayName = displayName,
            ClientUri = clientUri,
            LogoUri = logoUri,
        };

        Check.NotNullOrEmpty(grantTypes, nameof(grantTypes));
        Check.NotNullOrEmpty(scopes, nameof(scopes));

        if (new[] { OpenIddictConstants.GrantTypes.AuthorizationCode, OpenIddictConstants.GrantTypes.Implicit }.All(
                grantTypes.Contains))
        {
            application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdToken);

            if (string.Equals(type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeIdTokenToken);
                application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.CodeToken);
            }
        }

        if (!redirectUris.IsNullOrEmpty() || !postLogoutRedirectUris.IsNullOrEmpty())
        {
            application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);
        }

        var buildInGrantTypes = new[] {
            OpenIddictConstants.GrantTypes.Implicit, OpenIddictConstants.GrantTypes.Password,
            OpenIddictConstants.GrantTypes.AuthorizationCode, OpenIddictConstants.GrantTypes.ClientCredentials,
            OpenIddictConstants.GrantTypes.DeviceCode, OpenIddictConstants.GrantTypes.RefreshToken
        };

        foreach (var grantType in grantTypes)
        {
            if (grantType == OpenIddictConstants.GrantTypes.AuthorizationCode)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
                application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            }

            if (grantType == OpenIddictConstants.GrantTypes.AuthorizationCode ||
                grantType == OpenIddictConstants.GrantTypes.Implicit)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            }

            if (grantType == OpenIddictConstants.GrantTypes.AuthorizationCode ||
                grantType == OpenIddictConstants.GrantTypes.ClientCredentials ||
                grantType == OpenIddictConstants.GrantTypes.Password ||
                grantType == OpenIddictConstants.GrantTypes.RefreshToken ||
                grantType == OpenIddictConstants.GrantTypes.DeviceCode)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Revocation);
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
            }

            if (grantType == OpenIddictConstants.GrantTypes.ClientCredentials)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
            }

            if (grantType == OpenIddictConstants.GrantTypes.Implicit)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Implicit);
            }

            if (grantType == OpenIddictConstants.GrantTypes.Password)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.Password);
            }

            if (grantType == OpenIddictConstants.GrantTypes.RefreshToken)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            }

            if (grantType == OpenIddictConstants.GrantTypes.DeviceCode)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.DeviceCode);
                application.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.DeviceAuthorization);
            }

            if (grantType == OpenIddictConstants.GrantTypes.Implicit)
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdToken);
                if (string.Equals(type, OpenIddictConstants.ClientTypes.Public, StringComparison.OrdinalIgnoreCase))
                {
                    application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.IdTokenToken);
                    application.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Token);
                }
            }

            if (!buildInGrantTypes.Contains(grantType))
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.GrantType + grantType);
            }
        }

        var buildInScopes = new[] {
            OpenIddictConstants.Permissions.Scopes.Address, OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Phone, OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles
        };

        foreach (var scope in scopes)
        {
            if (buildInScopes.Contains(scope))
            {
                application.Permissions.Add(scope);
            }
            else
            {
                application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
            }
        }

        if (!redirectUris.IsNullOrEmpty())
        {
            foreach (var redirectUri in redirectUris!.Where(redirectUri => !redirectUri.IsNullOrWhiteSpace()))
            {
                if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) || !uri.IsWellFormedOriginalString())
                {
                    throw new BusinessException(L["InvalidRedirectUri", redirectUri]);
                }

                if (application.RedirectUris.All(x => x != uri))
                {
                    application.RedirectUris.Add(uri);
                }
            }

        }

        if (!postLogoutRedirectUris.IsNullOrEmpty())
        {
            foreach (var postLogoutRedirectUri in postLogoutRedirectUris!.Where(postLogoutRedirectUri => !postLogoutRedirectUri.IsNullOrWhiteSpace()))
            {
                if (!Uri.TryCreate(postLogoutRedirectUri, UriKind.Absolute, out var uri) ||
                    !uri.IsWellFormedOriginalString())
                {
                    throw new BusinessException(L["InvalidPostLogoutRedirectUri", postLogoutRedirectUri]);
                }

                if (application.PostLogoutRedirectUris.All(x => x != uri))
                {
                    application.PostLogoutRedirectUris.Add(uri);
                }
            }
        }

        if (permissions != null)
        {
            await _permissionDataSeeder.SeedAsync(
                ClientPermissionValueProvider.ProviderName,
                name,
                permissions,
                null
            );
        }

        if (client == null)
        {
            await _applicationManager.CreateAsync(application);
            return;
        }

        if (!HasSameRedirectUris(client, application))
        {
            client.RedirectUris = JsonSerializer.Serialize(application.RedirectUris.Select(q => q.ToString().RemovePostFix("/")));
            client.PostLogoutRedirectUris = JsonSerializer.Serialize(application.PostLogoutRedirectUris.Select(q => q.ToString().RemovePostFix("/")));

            await _applicationManager.UpdateAsync(client.ToModel());
        }

        if (!HasSameScopes(client, application))
        {
            client.Permissions = JsonSerializer.Serialize(application.Permissions.Select(q => q.ToString()));
            await _applicationManager.UpdateAsync(client.ToModel());
        }
    }

    private bool HasSameRedirectUris(OpenIddictApplication existingClient, AbpApplicationDescriptor application)
    {
        return existingClient.RedirectUris == JsonSerializer.Serialize(application.RedirectUris.Select(q => q.ToString().RemovePostFix("/")));
    }

    private bool HasSameScopes(OpenIddictApplication existingClient, AbpApplicationDescriptor application)
    {
        return existingClient.Permissions == JsonSerializer.Serialize(application.Permissions.Select(q => q.ToString().TrimEnd('/')));
    }
}
