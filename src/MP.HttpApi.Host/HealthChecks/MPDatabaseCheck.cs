using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace MP.HealthChecks;

public class MPDatabaseCheck : IHealthCheck, ITransientDependency
{
    protected readonly IIdentityRoleRepository IdentityRoleRepository;

    public MPDatabaseCheck(IIdentityRoleRepository identityRoleRepository)
    {
        IdentityRoleRepository = identityRoleRepository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await IdentityRoleRepository.GetListAsync(sorting: nameof(IdentityRole.Id), maxResultCount: 1, cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy($"Could connect to database and get record.");
        }
        catch (ReflectionTypeLoadException ex)
        {
            var errors = new List<string>();
            foreach (var loaderException in ex.LoaderExceptions)
            {
                if (loaderException != null)
                    errors.Add(loaderException.Message);
            }
            return HealthCheckResult.Unhealthy($"ReflectionTypeLoadException: {string.Join("; ", errors)}", ex);
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy($"Error when trying to get database record: {e.Message}", e);
        }
    }
}
