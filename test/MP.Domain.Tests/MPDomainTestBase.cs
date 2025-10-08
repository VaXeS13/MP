using Volo.Abp.Modularity;

namespace MP;

/* Inherit from this class for your domain layer tests. */
public abstract class MPDomainTestBase<TStartupModule> : MPTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
