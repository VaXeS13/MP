export enum CartStatus {
  Active = 0,
  CheckedOut = 1,
  Abandoned = 2
}

export interface BoothPricingPeriodDto {
  days: number;
  pricePerPeriod: number;
}

export interface CartItemDto {
  id: string;
  cartId: string;
  boothId: string;
  boothTypeId: string;
  startDate: string;
  endDate: string;
  pricePerDay: number;
  notes?: string;
  daysCount: number;
  totalPrice: number;
  discountAmount: number;
  discountPercentage: number;
  finalPrice: number;
  boothNumber?: string;
  boothDescription?: string;
  boothTypeName?: string;
  currency?: string;
  pricingPeriods?: BoothPricingPeriodDto[]; // Pricing periods for detailed breakdown
  reservationExpiresAt?: string; // ISO datetime string
  isExpired: boolean; // Deprecated: use reservationExpiresAt instead
  // Price update tracking
  oldStoredTotalPrice?: number; // Previous price before admin update
  priceWasUpdated: boolean; // True if admin changed pricing
}

export interface CartDto {
  id: string;
  userId: string;
  status: CartStatus;
  statusDisplayName: string;
  items: CartItemDto[];
  itemCount: number;
  totalAmount: number;
  finalAmount: number;
  totalDays: number;
  userName?: string;
  userEmail?: string;
  creationTime: string;
  lastModificationTime?: string;
  // Promotion fields
  appliedPromotionId?: string;
  promotionName?: string;
  discountAmount: number;
  promoCodeUsed?: string;
}

export interface AddToCartDto {
  boothId: string;
  boothTypeId: string;
  startDate: string;
  endDate: string;
  notes?: string;
}

export interface UpdateCartItemDto {
  boothTypeId: string;
  startDate: string;
  endDate: string;
  notes?: string;
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