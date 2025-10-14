namespace MP.Domain.Settings;

public static class MPSettings
{
    private const string Prefix = "MP";

    public static class PaymentProviders
    {
        private const string PaymentPrefix = Prefix + ".PaymentProviders";

        // Przelewy24 Settings
        public const string Przelewy24Enabled = PaymentPrefix + ".Przelewy24.Enabled";
        public const string Przelewy24MerchantId = PaymentPrefix + ".Przelewy24.MerchantId";
        public const string Przelewy24PosId = PaymentPrefix + ".Przelewy24.PosId";
        public const string Przelewy24ApiKey = PaymentPrefix + ".Przelewy24.ApiKey";
        public const string Przelewy24CrcKey = PaymentPrefix + ".Przelewy24.CrcKey";

        // PayPal Settings
        public const string PayPalEnabled = PaymentPrefix + ".PayPal.Enabled";
        public const string PayPalClientId = PaymentPrefix + ".PayPal.ClientId";
        public const string PayPalClientSecret = PaymentPrefix + ".PayPal.ClientSecret";

        // Stripe Settings
        public const string StripeEnabled = PaymentPrefix + ".Stripe.Enabled";
        public const string StripePublishableKey = PaymentPrefix + ".Stripe.PublishableKey";
        public const string StripeSecretKey = PaymentPrefix + ".Stripe.SecretKey";
        public const string StripeWebhookSecret = PaymentPrefix + ".Stripe.WebhookSecret";
    }

    public static class Booths
    {
        private const string BoothsPrefix = Prefix + ".Booths";

        /// <summary>
        /// Minimum gap in days between rentals.
        /// If a booth is rented until day D, the next rental can start on day D+1
        /// OR day D+1+MinimumGapDays (leaving at least MinimumGapDays free).
        /// Default: 3 days
        /// </summary>
        public const string MinimumGapDays = BoothsPrefix + ".MinimumGapDays";
    }

    public static class Tenant
    {
        private const string TenantPrefix = Prefix + ".Tenant";

        /// <summary>
        /// Default currency for tenant.
        /// All new booths, rentals, and items will use this currency.
        /// Stored as integer corresponding to Currency enum (1 = PLN, 2 = EUR, etc.)
        /// Default: 1 (PLN)
        /// </summary>
        public const string Currency = TenantPrefix + ".Currency";
    }

    //Add your own setting names here. Example:
    //public const string MySetting1 = Prefix + ".MySetting1";
}
