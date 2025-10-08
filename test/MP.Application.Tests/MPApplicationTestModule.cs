using Volo.Abp.Modularity;

namespace MP;

[DependsOn(
    typeof(MPApplicationModule),
    typeof(MPDomainTestModule)
)]
public class MPApplicationTestModule : AbpModule
{

}
