import type { RentalPaymentMethod } from '../domain/rentals/rental-payment-method.enum';
import type { CalendarDateStatus } from './calendar-date-status.enum';
import type { ExtensionPaymentType } from './extension-payment-type.enum';
import type { EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { RentalStatus } from './rental-status.enum';

export interface AdminManageBoothRentalDto {
  boothId: string;
  userId?: string;
  boothTypeId?: string;
  startDate: string;
  endDate: string;
  paymentMethod: RentalPaymentMethod;
  terminalTransactionId?: string;
  terminalReceiptNumber?: string;
  notes?: string;
  isExtension: boolean;
  existingRentalId?: string;
  onlineTimeoutMinutes?: number;
}

export interface BoothCalendarRequestDto {
  boothId: string;
  startDate: string;
  endDate: string;
  excludeCartId?: string;
}

export interface BoothCalendarResponseDto {
  boothId?: string;
  boothNumber?: string;
  startDate?: string;
  endDate?: string;
  dates: CalendarDateDto[];
  legend: Record<string, string>;
}

export interface CalendarDateDto {
  date?: string;
  status?: CalendarDateStatus;
  statusDisplayName?: string;
  rentalId?: string;
  userName?: string;
  userEmail?: string;
  rentalStartDate?: string;
  rentalEndDate?: string;
  notes?: string;
}

export interface CreateMyRentalDto {
  boothId: string;
  boothTypeId: string;
  startDate: string;
  endDate: string;
  notes?: string;
}

export interface CreateRentalDto {
  userId: string;
  boothId: string;
  boothTypeId: string;
  startDate: string;
  endDate: string;
  notes?: string;
}

export interface CreateRentalWithPaymentDto {
  boothId: string;
  boothTypeId: string;
  startDate: string;
  endDate: string;
  notes?: string;
  paymentProviderId: string;
  paymentMethodId?: string;
}

export interface CreateRentalWithPaymentResultDto {
  success: boolean;
  rentalId?: string;
  transactionId?: string;
  paymentUrl?: string;
  errorMessage?: string;
}

export interface ExtendRentalDto {
  rentalId: string;
  newEndDate: string;
  paymentType: ExtensionPaymentType;
  terminalTransactionId?: string;
  terminalReceiptNumber?: string;
  onlineTimeoutMinutes?: number;
}

export interface GetRentalListDto extends PagedAndSortedResultRequestDto {
  filter?: string;
  status?: RentalStatus;
  userId?: string;
  boothId?: string;
  fromDate?: string;
  toDate?: string;
  isOverdue?: boolean;
}

export interface MaxExtensionDateResponseDto {
  maxExtensionDate?: string;
  hasBlockingRental: boolean;
  nextRentalId?: string;
  nextRentalStartDate?: string;
  message?: string;
}

export interface PaymentDto {
  amount: number;
  paidDate: string;
}

export interface RentalDto extends FullAuditedEntityDto<string> {
  userId?: string;
  userName?: string;
  userEmail?: string;
  boothId?: string;
  boothNumber?: string;
  startDate?: string;
  endDate?: string;
  daysCount: number;
  status?: RentalStatus;
  statusDisplayName?: string;
  totalAmount: number;
  currency?: string;
  paidAmount: number;
  paidDate?: string;
  isPaid: boolean;
  remainingAmount: number;
  notes?: string;
  startedAt?: string;
  completedAt?: string;
  itemsCount: number;
  soldItemsCount: number;
  totalSalesAmount: number;
  totalCommissionEarned: number;
}

export interface RentalListDto extends EntityDto<string> {
  userId?: string;
  userName?: string;
  userEmail?: string;
  boothId?: string;
  boothNumber?: string;
  startDate?: string;
  endDate?: string;
  daysCount: number;
  status?: RentalStatus;
  statusDisplayName?: string;
  totalAmount: number;
  paidAmount: number;
  isPaid: boolean;
  creationTime?: string;
  startedAt?: string;
  itemsCount: number;
  soldItemsCount: number;
}

export interface UpdateRentalDto {
  startDate: string;
  endDate: string;
  notes?: string;
}
