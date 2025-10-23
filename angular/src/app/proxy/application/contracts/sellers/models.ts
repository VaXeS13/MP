import type { PaymentMethodType } from './payment-method-type.enum';

export interface AvailablePaymentMethodsDto {
  cashEnabled: boolean;
  cardEnabled: boolean;
  terminalProviderId?: string;
  terminalProviderName?: string;
}

export interface CheckoutItemDto {
  itemSheetItemId: string;
  paymentMethod: PaymentMethodType;
  amount: number;
}

export interface CheckoutItemsDto {
  itemSheetItemIds: string[];
  paymentMethod: PaymentMethodType;
  totalAmount: number;
}

export interface CheckoutResultDto {
  success: boolean;
  transactionId?: string;
  errorMessage?: string;
  paymentMethod?: PaymentMethodType;
  amount: number;
  processedAt?: string;
}

export interface CheckoutSummaryDto {
  items: ItemForCheckoutDto[];
  totalAmount: number;
  totalCommission: number;
  totalCustomerAmount: number;
  itemsCount: number;
}

export interface FindItemByBarcodeDto {
  barcode: string;
}

export interface ItemForCheckoutDto {
  id?: string;
  rentalId?: string;
  name?: string;
  description?: string;
  category?: string;
  photoUrl?: string;
  barcode?: string;
  actualPrice?: number;
  commissionPercentage: number;
  commissionAmount: number;
  customerAmount: number;
  status?: string;
  customerName?: string;
  customerEmail?: string;
  customerPhone?: string;
}
