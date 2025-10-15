import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { RentalDto } from '../rentals/models';

export interface CreatePaymentTransactionDto {
  sessionId: string;
  merchantId: number;
  posId: number;
  amount: number;
  currency: string;
  email: string;
  description: string;
  method?: string;
  transferLabel?: string;
  sign: string;
  orderId?: string;
  returnUrl?: string;
  statement?: string;
  extraProperties?: string;
  rentalId?: string;
}

export interface GetPaymentTransactionListDto extends PagedAndSortedResultRequestDto {
  filter?: string;
  status?: string;
  paymentMethod?: string;
  startDate?: string;
  endDate?: string;
  minAmount?: number;
  maxAmount?: number;
  rentalId?: string;
  email?: string;
}

export interface PaymentSuccessViewModel {
  transaction: PaymentTransactionDto;
  rentals: RentalDto[];
  success: boolean;
  message?: string;
  nextStepUrl?: string;
  nextStepText?: string;
  totalAmount: number;
  currency?: string;
  paymentDate?: string;
  paymentMethod?: string;
  formattedPaymentDate?: string;
  formattedTotalAmount?: string;
  orderId?: string;
  paymentProvider?: string;
  isVerified: boolean;
  method?: string;
}

export interface PaymentTransactionDto extends FullAuditedEntityDto<string> {
  sessionId?: string;
  merchantId: number;
  posId: number;
  amount: number;
  currency?: string;
  email?: string;
  description?: string;
  method?: string;
  transferLabel?: string;
  sign?: string;
  orderId?: string;
  verified: boolean;
  returnUrl?: string;
  statement?: string;
  extraProperties?: string;
  manualStatusCheckCount: number;
  status?: string;
  lastStatusCheck?: string;
  rentalId?: string;
  statusDisplayName?: string;
  isCompleted: boolean;
  isFailed: boolean;
  isPending: boolean;
  isVerified: boolean;
  formattedAmount?: string;
  formattedCreatedAt?: string;
  formattedCompletedAt?: string;
  paymentMethodDisplayName?: string;
  transactionGuid?: string;
  createdAt?: string;
  completedAt?: string;
}

export interface UpdatePaymentTransactionDto {
  description?: string;
  status?: string;
  lastStatusCheck?: string;
  method?: string;
  transferLabel?: string;
  returnUrl?: string;
  statement?: string;
  extraProperties?: string;
  verified?: boolean;
  manualStatusCheckCount?: number;
}
