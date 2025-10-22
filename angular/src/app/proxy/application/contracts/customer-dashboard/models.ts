import type { NotificationSeverity } from '../../../domain/notifications/notification-severity.enum';
import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { RentalStatus } from '../../../rentals/rental-status.enum';
import type { PriceBreakdownDto } from '../rentals/models';
import type { SettlementStatus } from '../../../domain/settlements/settlement-status.enum';

export interface BulkUpdateMyItemsDto {
  itemIds: string[];
  category?: string;
  commissionPercentage?: number;
}

export interface CategorySalesDto {
  category?: string;
  itemCount: number;
  totalAmount: number;
  percentage: number;
}

export interface CategoryStatDto {
  category?: string;
  totalItems: number;
  soldItems: number;
  salesValue: number;
}

export interface CreateMyItemDto {
  rentalId: string;
  name: string;
  description?: string;
  category?: string;
  estimatedPrice?: number;
  photoUrls: string[];
}

export interface CustomerDashboardDto {
  overview: CustomerOverviewDto;
  activeRentals: MyActiveRentalDto[];
  salesStatistics: CustomerSalesStatisticsDto;
  recentSales: RecentItemSaleDto[];
  recentNotifications: CustomerNotificationDto[];
  settlementSummary: SettlementSummaryDto;
}

export interface CustomerNotificationDto {
  id?: string;
  type?: string;
  title?: string;
  message?: string;
  createdAt?: string;
  isRead: boolean;
  actionUrl?: string;
  severity?: NotificationSeverity;
}

export interface CustomerOverviewDto {
  totalActiveRentals: number;
  totalItemsForSale: number;
  totalItemsSold: number;
  totalSalesAmount: number;
  totalCommissionPaid: number;
  availableForWithdrawal: number;
  daysUntilNextRentalExpiration: number;
  hasExpiringRentals: boolean;
  hasPendingSettlements: boolean;
}

export interface CustomerSalesStatisticsDto {
  todaySales: number;
  weekSales: number;
  monthSales: number;
  allTimeSales: number;
  todayItemsSold: number;
  weekItemsSold: number;
  monthItemsSold: number;
  allTimeItemsSold: number;
  averageSalePrice: number;
  highestSalePrice: number;
  lowestSalePrice: number;
  last30DaysSales: DailySalesChartDto[];
  salesByCategory: CategorySalesDto[];
  monthlyGrowthPercentage: number;
}

export interface CustomerStatisticsFilterDto {
  startDate?: string;
  endDate?: string;
  rentalId?: string;
}

export interface DailySalesChartDto {
  date?: string;
  amount: number;
  itemCount: number;
}

export interface ExtensionCostCalculationDto {
  rentalId?: string;
  extensionDays: number;
  currentEndDate?: string;
  newEndDate?: string;
  pricePerDay: number;
  totalCost: number;
  isAvailable: boolean;
  unavailableReason?: string;
}

export interface ExtensionOptionDto {
  days: number;
  displayName?: string;
  cost: number;
  newEndDate?: string;
}

export interface GetMyItemsDto extends PagedAndSortedResultRequestDto {
  rentalId?: string;
  status?: string;
  category?: string;
  searchTerm?: string;
  createdAfter?: string;
  createdBefore?: string;
}

export interface GetMyRentalsDto extends PagedAndSortedResultRequestDto {
  status?: RentalStatus;
  startDateFrom?: string;
  startDateTo?: string;
  includeCompleted?: boolean;
}

export interface MonthlyItemStatDto {
  month?: string;
  itemsAdded: number;
  itemsSold: number;
  salesValue: number;
}

export interface MyActiveRentalDto {
  rentalId?: string;
  boothNumber?: string;
  boothTypeName?: string;
  startDate?: string;
  endDate?: string;
  daysRemaining: number;
  isExpiringSoon: boolean;
  status?: string;
  totalItems: number;
  soldItems: number;
  availableItems: number;
  totalSales: number;
  totalCommission: number;
  canExtend: boolean;
  qrCodeUrl?: string;
  totalCost: number;
  discountAmount: number;
  originalAmount: number;
  promoCodeUsed?: string;
}

export interface MyItemDto extends FullAuditedEntityDto<string> {
  rentalId?: string;
  boothNumber?: string;
  name?: string;
  description?: string;
  category?: string;
  photoUrls: string[];
  itemNumber: number;
  barcode?: string;
  estimatedPrice?: number;
  actualPrice?: number;
  commissionPercentage: number;
  status?: string;
  statusDisplayName?: string;
  soldAt?: string;
  daysForSale: number;
  commissionAmount?: number;
  customerAmount?: number;
  canEdit: boolean;
  canDelete: boolean;
}

export interface MyItemStatisticsDto {
  totalItems: number;
  forSaleItems: number;
  soldItems: number;
  reclaimedItems: number;
  expiredItems: number;
  totalEstimatedValue: number;
  totalSalesValue: number;
  averageItemPrice: number;
  byCategory: CategoryStatDto[];
  monthlyTrend: MonthlyItemStatDto[];
}

export interface MyRentalCalendarDto {
  events: RentalCalendarEventDto[];
  importantDates: string[];
}

export interface MyRentalDetailDto extends FullAuditedEntityDto<string> {
  boothId?: string;
  boothNumber?: string;
  boothTypeName?: string;
  boothPricePerDay: number;
  startDate?: string;
  endDate?: string;
  totalDays: number;
  daysRemaining: number;
  daysElapsed: number;
  status?: RentalStatus;
  statusDisplayName?: string;
  totalCost: number;
  paidAmount: number;
  isPaid: boolean;
  paidDate?: string;
  priceBreakdown: PriceBreakdownDto;
  appliedPromotionId?: string;
  discountAmount: number;
  originalAmount: number;
  promoCodeUsed?: string;
  notes?: string;
  startedAt?: string;
  completedAt?: string;
  totalItems: number;
  soldItems: number;
  availableItems: number;
  reclaimedItems: number;
  totalSalesAmount: number;
  totalCommissionPaid: number;
  netEarnings: number;
  canExtend: boolean;
  canCancel: boolean;
  isExpiringSoon: boolean;
  isOverdue: boolean;
  qrCodeUrl?: string;
  extensionOptions: ExtensionOptionDto[];
  recentActivity: RentalActivityDto[];
}

export interface QRCodeDto {
  rentalId?: string;
  boothNumber?: string;
  qrCodeBase64?: string;
  accessCode?: string;
  generatedAt?: string;
  expiresAt?: string;
}

export interface RecentItemSaleDto {
  itemId?: string;
  itemName?: string;
  category?: string;
  salePrice: number;
  commissionAmount: number;
  customerAmount: number;
  soldAt?: string;
  boothNumber?: string;
}

export interface RentalActivityDto {
  timestamp?: string;
  type?: string;
  description?: string;
  itemName?: string;
  amount?: number;
}

export interface RentalCalendarEventDto {
  rentalId?: string;
  boothNumber?: string;
  startDate?: string;
  endDate?: string;
  status?: string;
  color?: string;
  isExpiringSoon: boolean;
}

export interface RentalExtensionResultDto {
  rentalId?: string;
  newEndDate?: string;
  additionalCost: number;
  paymentRequired: boolean;
  paymentUrl?: string;
  paymentSessionId?: string;
}

export interface RequestRentalExtensionDto {
  rentalId: string;
  extensionDays: number;
  paymentProviderId?: string;
}

export interface RequestSettlementDto {
  itemIds: string[];
  notes?: string;
  bankAccountNumber?: string;
}

export interface SettlementItemDto {
  id?: string;
  createdAt?: string;
  amount: number;
  status?: SettlementStatus;
  statusDisplayName?: string;
  itemsCount: number;
  notes?: string;
  paidAt?: string;
  transactionReference?: string;
}

export interface SettlementSummaryDto {
  totalEarnings: number;
  totalCommissionPaid: number;
  netEarnings: number;
  pendingSettlement: number;
  availableForWithdrawal: number;
  pendingItemsCount: number;
  lastSettlementDate?: string;
  recentSettlements: SettlementItemDto[];
}

export interface UpdateMyItemDto {
  name: string;
  description?: string;
  category?: string;
  estimatedPrice?: number;
  photoUrls: string[];
}
