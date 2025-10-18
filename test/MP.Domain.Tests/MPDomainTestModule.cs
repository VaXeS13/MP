using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MP.Domain;
using MP.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

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

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // Seed test user for domain tests
        AsyncHelper.RunSync(async () => await SeedTestDataAsync(context.ServiceProvider));
    }

    private async Task SeedTestDataAsync(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IRepository<IdentityUser, Guid>>();

            // Known test user IDs - must match those used in tests
            var testUserId1 = new Guid("00000000-0000-0000-0000-000000000001");
            var testUserId2 = new Guid("00000000-0000-0000-0000-000000000002");

            using (var unitOfWork = uow.Begin())
            {
                try
                {
                    // Check if test users already exist
                    var testUser1 = await userRepository.FirstOrDefaultAsync(u => u.Email == "testuser1@test.com");
                    var testUser2 = await userRepository.FirstOrDefaultAsync(u => u.Email == "testuser2@test.com");

                    if (testUser1 == null)
                    {
                        testUser1 = new IdentityUser(testUserId1, "TestUser1", "testuser1@test.com");
                        await userRepository.InsertAsync(testUser1);
                    }

                    if (testUser2 == null)
                    {
                        testUser2 = new IdentityUser(testUserId2, "TestUser2", "testuser2@test.com");
                        await userRepository.InsertAsync(testUser2);
                    }

                    await unitOfWork.SaveChangesAsync();
                    await unitOfWork.CompleteAsync();
                }
                catch
                {
                    // Silently ignore if users already exist
                }
            }
        }
    }
}
