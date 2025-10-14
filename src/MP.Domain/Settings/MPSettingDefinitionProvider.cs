using Volo.Abp.Settings;

namespace MP.Domain.Settings;

public class MPSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        // Przelewy24 Settings - Tenant level
        context.Add(new SettingDefinition(
            MPSettings.PaymentProviders.Przelewy24Enabled,
            "false"
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        context.Add(new SettingDefinition(
            MPSettings.PaymentProviders.Przelewy24MerchantId,
            ""
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        context.Add(new SettingDefinition(
            MPSettings.PaymentProviders.Przelewy24PosId,
            ""
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        context.Add(new SettingDefinition(
            MPSettings.PaymentProviders.Przelewy24ApiKey,
            ""
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        context.Add(new SettingDefinition(
            MPSettings.PaymentProviders.Przelewy24CrcKey,
            ""
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        // PayPal Settings - Tenant level
        context.Add(new SettingDefinition(
            MPSettings.PaymentProviders.PayPalEnabled,
            "false"
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        context.Add(new SettingDefinition(
            MPSettings.PaymentProviders.PayPalClientId,
            ""
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        context.Add(new SettingDefinition(
            MPSettings.PaymentProviders.PayPalClientSecret,
            ""
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        // Stripe Settings - Tenant level
        context.Add(new SettingDefinition(
            MPSettings.PaymentProviders.StripeEnabled,
            "false"
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        context.Add(new SettingDefinition(
            MPSettings.PaymentProviders.StripePublishableKey,
            ""
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        context.Add(new SettingDefinition(
            MPSettings.PaymentProviders.StripeSecretKey,
            ""
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        context.Add(new SettingDefinition(
            MPSettings.PaymentProviders.StripeWebhookSecret,
            ""
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        // Booth Settings - Tenant level
        context.Add(new SettingDefinition(
            MPSettings.Booths.MinimumGapDays,
            "7",
            isVisibleToClients: true
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        // Tenant Settings - Tenant level
        context.Add(new SettingDefinition(
            MPSettings.Tenant.Currency,
            "1", // Default: PLN
            isVisibleToClients: true
        ).WithProviders(TenantSettingValueProvider.ProviderName));

        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(MPSettings.MySetting1));
    }
}
