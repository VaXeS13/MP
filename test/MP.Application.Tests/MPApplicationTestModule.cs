using System;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Volo.Abp.Modularity;
using Volo.Abp.Users;

namespace MP;

[DependsOn(
    typeof(MPApplicationModule),
    typeof(MPDomainTestModule)
)]
public class MPApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Set up mock current user for application service tests
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.Id.Returns(new Guid("00000000-0000-0000-0000-000000000001"));
        currentUser.GetId().Returns(new Guid("00000000-0000-0000-0000-000000000001"));

        context.Services.AddSingleton(currentUser);
    }
}
