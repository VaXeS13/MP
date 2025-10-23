using System;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Uow;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using Volo.Abp.Studio;
using MP.Booths;
using MP.Domain.Booths;
using MP.Domain.BoothTypes;
using MP.EntityFrameworkCore.BoothTypes;
using MP.Domain;
using MP.Domain.Payments;
using MP.EntityFrameworkCore.Payments;
using MP.Domain.Carts;
using MP.EntityFrameworkCore.Carts;
using MP.Domain.Items;
using MP.EntityFrameworkCore.Items;
using MP.Domain.LocalAgent;
using MP.EntityFrameworkCore.LocalAgent;

namespace MP.EntityFrameworkCore;

[DependsOn(
    typeof(MPDomainModule),
    typeof(AbpPermissionManagementEntityFrameworkCoreModule),
    typeof(AbpSettingManagementEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCoreSqlServerModule),
    typeof(AbpBackgroundJobsEntityFrameworkCoreModule),
    typeof(AbpAuditLoggingEntityFrameworkCoreModule),
    typeof(AbpFeatureManagementEntityFrameworkCoreModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpOpenIddictEntityFrameworkCoreModule),
    typeof(AbpTenantManagementEntityFrameworkCoreModule),
    typeof(BlobStoringDatabaseEntityFrameworkCoreModule)
    )]
public class MPEntityFrameworkCoreModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {

        MPEfCoreEntityExtensionMappings.Configure();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<MPDbContext>(options =>
        {
            /* Remove "includeAllEntities: true" to create
             * default repositories only for aggregate roots */
            options.AddDefaultRepositories(includeAllEntities: true);
        });

        if (AbpStudioAnalyzeHelper.IsInAnalyzeMode)
        {
            return;
        }

        Configure<AbpDbContextOptions>(options =>
        {
            /* The main point to change your DBMS.
             * See also MPDbContextFactory for EF Core tooling. */

            options.UseSqlServer();

        });
        context.Services.AddTransient<IBoothRepository, EfCoreBoothRepository>();
        context.Services.AddTransient<IBoothTypeRepository, EfCoreBoothTypeRepository>();
        context.Services.AddTransient<ICartRepository, EfCoreCartRepository>();
        context.Services.AddTransient<IP24TransactionRepository, EfCoreP24TransactionRepository>();
        context.Services.AddTransient<IStripeTransactionRepository, EfCoreStripeTransactionRepository>();
        context.Services.AddTransient<IPayPalTransactionRepository, EfCorePayPalTransactionRepository>();
        context.Services.AddTransient<IItemRepository, EfCoreItemRepository>();
        context.Services.AddTransient<IItemSheetRepository, EfCoreItemSheetRepository>();
        context.Services.AddTransient<IAgentApiKeyRepository, EfCoreAgentApiKeyRepository>();
    }
}
