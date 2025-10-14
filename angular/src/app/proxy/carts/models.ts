import type { EntityDto } from '@abp/ng.core';
import type { CartStatus } from './cart-status.enum';

export interface AddToCartDto {
  boothId: string;
  boothTypeId: string;
  startDate: string;
  endDate: string;
  notes?: string;
}

export interface CartDto extends EntityDto<string> {
  userId?: string;
  status?: CartStatus;
  statusDisplayName?: string;
  items: CartItemDto[];
  itemCount: number;
  totalAmount: number;
  totalDays: number;
  userName?: string;
  userEmail?: string;
  appliedPromotionId?: string;
  promotionName?: string;
  discountAmount: number;
  promoCodeUsed?: string;
  creationTime?: string;
  lastModificationTime?: string;
}

export interface CartItemDto extends EntityDto<string> {
  cartId?: string;
  boothId?: string;
  boothTypeId?: string;
  startDate?: string;
  endDate?: string;
  pricePerDay: number;
  notes?: string;
  daysCount: number;
  totalPrice: number;
  discountAmount: number;
  finalPrice: number;
  boothNumber?: string;
  boothDescription?: string;
  boothTypeName?: string;
  currency?: string;
  reservationExpiresAt?: string;
  isExpired: boolean;
}

export interface CheckoutCartDto {
  paymentProviderId: string;
  paymentMethodId?: string;
}

export interface CheckoutResultDto {
  success: boolean;
  errorMessage?: string;
  transactionId?: string;
  paymentUrl?: string;
  rentalIds: string[];
  totalAmount: number;
  itemCount: number;
}

export interface UpdateCartItemDto {
  boothTypeId: string;
  startDate: string;
  endDate: string;
  notes?: string;
}
