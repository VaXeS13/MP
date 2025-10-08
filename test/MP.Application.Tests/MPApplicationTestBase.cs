using Volo.Abp.Modularity;

namespace MP;

public abstract class MPApplicationTestBase<TStartupModule> : MPTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
