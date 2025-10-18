using System;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;
using MP.Application.Contracts.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        // Set up mock current tenant with TenantId for multi-tenancy tests
        var currentTenant = Substitute.For<ICurrentTenant>();
        currentTenant.Id.Returns(new Guid("7E4E5B7F-55C0-BF0B-BBE8-3A1BD8B36A6D"));
        currentTenant.Name.Returns("TestTenant");
        currentTenant.IsAvailable.Returns(true);

        context.Services.Replace(ServiceDescriptor.Singleton(typeof(ICurrentTenant), currentTenant));

        // Mock SignalRNotificationService for tests
        var signalRNotificationService = Substitute.For<ISignalRNotificationService>();
        context.Services.AddSingleton(signalRNotificationService);
    }
}
