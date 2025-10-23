using System;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Volo.Abp.TenantManagement;
using MP.Application.Devices;
using MP.Booths;
using MP.Domain;
using MP.Application.Contracts.Payments;
using MP.Application.Payments;
using MP.Application.Contracts.Rentals;
using MP.Application.Rentals;
using MP.Application.Terminals;
using MP.Carts;
using MP.Items;

namespace MP;

[DependsOn(
    typeof(MPDomainModule),
    typeof(MPApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class MPApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<MPApplicationModule>();
        });

        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<MPApplicationModule>();
            // Dodaj nasze profile AutoMapper
            options.AddProfile<BoothProfile>();
        });

        // Rejestruj serwisy płatności
        services.AddTransient<IPaymentProviderAppService, PaymentProviderAppServiceNew>();

        // Rejestruj dostawców płatności
        services.AddTransient<Przelewy24Provider>();
        services.AddTransient<StripeProvider>();
        services.AddTransient<PayPalProvider>();

        // Rejestruj factory dostawców płatności
        services.AddTransient<IPaymentProviderFactory, PaymentProviderFactory>();

        // Rejestruj serwis generowania etykiet
        services.AddTransient<ILabelGeneratorService, LabelGeneratorService>();

        // Rejestruj dostawców terminali płatniczych
        services.AddTransient<MockTerminalProvider>();
        services.AddTransient<Application.Terminals.Providers.IngenicoLane5000Provider>();
        services.AddTransient<Application.Terminals.Providers.VerifoneVX520Provider>();
        services.AddTransient<Application.Terminals.Providers.NetsTerminalProvider>();
        services.AddTransient<Application.Terminals.Providers.SquareTerminalProvider>();
        services.AddTransient<Application.Terminals.Providers.StripeTerminalProvider>();
        services.AddTransient<Application.Terminals.Providers.SumUpProvider>();
        services.AddTransient<Application.Terminals.Providers.AdyenProvider>();
        // TODO: Add more providers when implemented:
        // services.AddTransient<Application.Terminals.Providers.PAXA920Provider>();

        // Rejestruj factory terminali płatniczych
        services.AddTransient<ITerminalPaymentProviderFactory, TerminalPaymentProviderFactory>();

        // Rejestruj communication layer dla terminali
        services.AddTransient<Application.Terminals.Communication.TcpIpTerminalCommunication>();
        services.AddTransient<Application.Terminals.Communication.SerialPortCommunication>();
        services.AddTransient<Application.Terminals.Communication.RestApiCommunication>();
        services.AddTransient<Application.Terminals.Communication.UsbCommunication>();
        services.AddTransient<Application.Terminals.Communication.BluetoothCommunication>();

        // Rejestruj dostawców kas fiskalnych
        services.AddTransient<Application.FiscalPrinters.Providers.PosnetThermalProvider>();
        services.AddTransient<Application.FiscalPrinters.Providers.ElzabProvider>();
        services.AddTransient<Application.FiscalPrinters.Providers.NovitusProvider>();
        // TODO: Add more fiscal printers when implemented for other countries:
        // services.AddTransient<Application.FiscalPrinters.Providers.EpsonFiscalProvider>(); // Czech Republic
        // services.AddTransient<Application.FiscalPrinters.Providers.DatecsFiscalProvider>(); // Bulgaria, Romania

        // Rejestruj factory kas fiskalnych
        services.AddTransient<Application.FiscalPrinters.IFiscalPrinterProviderFactory, Application.FiscalPrinters.FiscalPrinterProviderFactory>();

        // Rejestruj Background Workers jako Hosted Services
        services.AddHostedService<ExpiredCartCleanupWorker>();
        services.AddHostedService<ExpiredRentalItemCleanupWorker>();
        services.AddHostedService<MP.Application.Notifications.NotificationReminderWorker>();

        // Rejestruj Remote Device Proxy dla komunikacji z lokalnymi agentami
        services.AddTransient<IRemoteDeviceProxy, SignalRDeviceProxy>();
        services.Configure<RemoteDeviceProxyOptions>(options =>
        {
            options.CommandTimeout = TimeSpan.FromSeconds(30);
            options.MaxRetries = 3;
            options.RetryDelay = TimeSpan.FromSeconds(2);
            options.EnableOfflineQueue = true;
            options.MaxQueuedCommands = 1000;
        });
    }
}
