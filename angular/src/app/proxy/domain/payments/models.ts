
export interface Przelewy24PaymentMethod {
  id: number;
  name?: string;
  displayName?: string;
  description?: string;
  iconUrl?: string;
  isActive: boolean;
  isAvailable: boolean;
  supportedCurrencies: string[];
  processingTime?: string;
  minAmount?: number;
  maxAmount?: number;
}

export interface Przelewy24PaymentRequest {
  merchantId?: string;
  posId?: string;
  sessionId?: string;
  amount: number;
  currency?: string;
  description?: string;
  email?: string;
  clientName?: string;
  country?: string;
  language?: string;
  urlReturn?: string;
  urlStatus?: string;
}

export interface Przelewy24PaymentResult {
  transactionId?: string;
  paymentUrl?: string;
  success: boolean;
  errorMessage?: string;
}

export interface Przelewy24PaymentStatus {
  transactionId?: string;
  status?: string;
  amount?: number;
  completedAt?: string;
  errorCode?: string;
  errorMessage?: string;
}
