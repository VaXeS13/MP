using MP.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace MP.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(MPEntityFrameworkCoreModule),
    typeof(MPApplicationContractsModule)
)]
public class MPDbMigratorModule : AbpModule
{
}
