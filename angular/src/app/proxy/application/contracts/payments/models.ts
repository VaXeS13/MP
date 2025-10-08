
export interface CreatePaymentRequestDto {
  amount: number;
  currency?: string;
  description?: string;
  providerId?: string;
  methodId?: string;
  metadata: Record<string, object>;
}

export interface PaymentCreationResultDto {
  success: boolean;
  paymentUrl?: string;
  transactionId?: string;
  errorMessage?: string;
}

export interface PaymentMethodDto {
  id?: string;
  name?: string;
  displayName?: string;
  description?: string;
  iconUrl?: string;
  processingTime?: string;
  fees: PaymentMethodFeesDto;
  isActive: boolean;
}

export interface PaymentMethodFeesDto {
  fixedAmount?: number;
  percentageAmount?: number;
  description?: string;
}

export interface PaymentProviderDto {
  id?: string;
  name?: string;
  displayName?: string;
  description?: string;
  logoUrl?: string;
  supportedCurrencies: string[];
  isActive: boolean;
}

export interface P24NotificationDto {
  p24_merchant_id: string;
  p24_pos_id: string;
  p24_session_id: string;
  p24_amount: number;
  p24_currency: string;
  p24_order_id: string;
  p24_method: number;
  p24_statement?: string;
  p24_sign?: string;
}
