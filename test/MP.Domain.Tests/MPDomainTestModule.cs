using MP.Domain;
using MP.EntityFrameworkCore;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;

namespace MP;

[DependsOn(
    typeof(MPDomainModule),
    typeof(MPTestBaseModule),
    typeof(MPEntityFrameworkCoreModule)
)]
public class MPDomainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Disable dynamic permissions and features to avoid DI issues
        Configure<FeatureManagementOptions>(options =>
        {
            options.SaveStaticFeaturesToDatabase = false;
            options.IsDynamicFeatureStoreEnabled = false;
        });
        Configure<PermissionManagementOptions>(options =>
        {
            options.SaveStaticPermissionsToDatabase = false;
            options.IsDynamicPermissionStoreEnabled = false;
        });
    }
}
