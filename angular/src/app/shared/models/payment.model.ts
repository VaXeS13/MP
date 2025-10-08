export enum PaymentStatus {
  Pending = 0,
  Processing = 1,
  Completed = 2,
  Failed = 3,
  Cancelled = 4,
  Refunded = 5
}

export interface PaymentProvider {
  id: string;
  name: string;
  displayName: string;
  logoUrl?: string;
  isActive: boolean;
  supportedCurrencies: string[];
  description?: string;
}

export interface PaymentMethod {
  id: string;
  name: string;
  displayName: string;
  iconUrl?: string;
  description?: string;
  isActive: boolean;
  processingTime?: string; // e.g., "Instant", "1-2 days"
  type?: PaymentMethodType;
  fees?: {
    fixed?: number;
    percentage?: number;
    description?: string;
  };
}

export interface PaymentRequest {
  amount: number;
  currency: string;
  description: string;
  providerId: string;
  methodId?: string;
  metadata?: { [key: string]: any };
}

export interface PaymentResponse {
  success: boolean;
  transactionId?: string;
  paymentUrl?: string;
  errorMessage?: string;
  requiresRedirect?: boolean;
  providerResponse?: any;
}

export interface Przelewy24Method {
  id: number;
  name: string;
  imgUrl: string;
  available: boolean;
  mobileImgUrl?: string;
}

export interface GetPaymentProvidersResponse {
  providers: PaymentProvider[];
}

export interface GetPaymentMethodsRequest {
  providerId: string;
  amount: number;
  currency: string;
}

export interface GetPaymentMethodsResponse {
  methods: PaymentMethod[];
}

export enum PaymentMethodType {
  BankTransfer = 0,
  CreditCard = 1,
  DebitCard = 2,
  DigitalWallet = 3,
  Cryptocurrency = 4,
  BLIK = 5,
  PayByLink = 6,
  Other = 99
}