import type { EntityDto } from '@abp/ng.core';

export interface CompleteWithdrawalDto {
  settlementId?: string;
  transactionReference?: string;
  providerMetadata?: string;
}

export interface PaymentWithdrawalDto extends EntityDto<string> {
  settlementNumber?: string;
  userId?: string;
  userName?: string;
  userEmail?: string;
  bankAccountNumber?: string;
  totalAmount: number;
  commissionAmount: number;
  netAmount: number;
  status?: string;
  itemsCount: number;
  creationTime?: string;
  processedAt?: string;
  paidAt?: string;
  processedByUserName?: string;
  transactionReference?: string;
  rejectionReason?: string;
  notes?: string;
  paymentMethod?: string;
  paymentProviderMetadata?: string;
}

export interface PaymentWithdrawalStatsDto {
  pendingCount: number;
  pendingAmount: number;
  processingCount: number;
  processingAmount: number;
  completedThisMonthCount: number;
  completedThisMonthAmount: number;
}

export interface ProcessWithdrawalDto {
  settlementId?: string;
  paymentMethod?: string;
  notes?: string;
}

export interface RejectWithdrawalDto {
  settlementId?: string;
  reason?: string;
}
