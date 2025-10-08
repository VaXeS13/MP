
export interface PayPalSettingsDto {
  enabled: boolean;
  clientId?: string;
  clientSecret?: string;
}

export interface PaymentProviderSettingsDto {
  przelewy24: Przelewy24SettingsDto;
  payPal: PayPalSettingsDto;
  stripe: StripeSettingsDto;
}

export interface Przelewy24SettingsDto {
  enabled: boolean;
  merchantId?: string;
  posId?: string;
  apiKey?: string;
  crcKey?: string;
}

export interface StripeSettingsDto {
  enabled: boolean;
  publishableKey?: string;
  secretKey?: string;
  webhookSecret?: string;
}

export interface UpdatePaymentProviderSettingsDto {
  przelewy24: Przelewy24SettingsDto;
  payPal: PayPalSettingsDto;
  stripe: StripeSettingsDto;
}
