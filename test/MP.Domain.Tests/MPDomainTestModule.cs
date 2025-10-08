using MP.Domain;
using Volo.Abp.Modularity;

namespace MP;

[DependsOn(
    typeof(MPDomainModule),
    typeof(MPTestBaseModule)
)]
public class MPDomainTestModule : AbpModule
{

}
