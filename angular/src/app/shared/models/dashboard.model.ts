export interface DashboardOverviewDto {
  salesOverview: SalesOverviewDto;
  boothOccupancy: BoothOccupancyOverviewDto;
  financial: FinancialOverviewDto;
  paymentAnalytics: PaymentAnalyticsDto;
  recentRentals: RecentRentalDto[];
  topSellingItems: TopSellingItemDto[];
}

export interface SalesOverviewDto {
  todaySales: number;
  weekSales: number;
  monthSales: number;
  yearSales: number;
  totalItemsSoldToday: number;
  totalItemsSoldWeek: number;
  totalItemsSoldMonth: number;
  averageSalePerItem: number;
  salesGrowthPercentage: number;
  last30DaysSales: DailySalesDto[];
}

export interface BoothOccupancyOverviewDto {
  totalBooths: number;
  occupiedBooths: number;
  availableBooths: number;
  reservedBooths: number;
  maintenanceBooths: number;
  occupancyRate: number;
  rentalsThisMonth: number;
  averageRentalDuration: number;
  monthlyRentalRevenue: number;
  occupancyTimeline: BoothOccupancyTimelineDto[];
}

export interface FinancialOverviewDto {
  monthlyRevenue: number;
  monthlyRentalIncome: number;
  monthlyCommissionIncome: number;
  pendingPayments: number;
  processingPayments: number;
  outstandingDebts: number;
  monthlyRevenueChart: MonthlyRevenueDto[];
  revenueBreakdown: RevenueSourceDto[];
}

export interface DailySalesDto {
  date: string;
  salesAmount: number;
  itemsSold: number;
}

export interface BoothOccupancyTimelineDto {
  date: string;
  occupiedBooths: number;
  totalBooths: number;
  occupancyRate: number;
}

export interface MonthlyRevenueDto {
  month: string;
  monthName: string;
  totalRevenue: number;
  rentalIncome: number;
  commissionIncome: number;
}

export interface RevenueSourceDto {
  source: string;
  amount: number;
  percentage: number;
  color: string;
}

export interface RecentRentalDto {
  rentalId: string;
  boothNumber: string;
  customerName: string;
  customerEmail: string;
  startDate: string;
  endDate: string;
  totalCost: number;
  status: string;
  daysRemaining: number;
}

export interface TopSellingItemDto {
  itemName: string;
  boothNumber: string;
  salePrice: number;
  soldDate: string;
  boothTypeName: string;
  commissionEarned: number;
}

export interface CustomerAnalyticsDto {
  totalCustomers: number;
  newCustomersThisMonth: number;
  activeRentals: number;
  topCustomers: TopCustomerDto[];
  registrationTimeline: CustomerRegistrationTimelineDto[];
}

export interface TopCustomerDto {
  userId: string;
  name: string;
  email: string;
  totalSpent: number;
  rentalsCount: number;
  totalSales: number;
  commissionGenerated: number;
  lastRentalDate: string;
}

export interface CustomerRegistrationTimelineDto {
  date: string;
  newRegistrations: number;
}

export interface PeriodFilterDto {
  startDate?: string;
  endDate?: string;
  period: PeriodType;
}

export enum PeriodType {
  Day = 'Day',
  Week = 'Week',
  Month = 'Month',
  Quarter = 'Quarter',
  Year = 'Year',
  Custom = 'Custom'
}

export interface PaymentAnalyticsDto {
  totalTransactions: number;
  completedTransactions: number;
  processingTransactions: number;
  failedTransactions: number;
  successRate: number;
  averageTransactionValue: number;
  totalProcessedAmount: number;
  averageProcessingTime: number;
  transactionTimeline: DailyTransactionStatsDto[];
  paymentMethodBreakdown: PaymentMethodStatsDto[];
  recentTransactions: RecentTransactionDto[];
}

export interface DailyTransactionStatsDto {
  date: string;
  totalTransactions: number;
  completedTransactions: number;
  failedTransactions: number;
  totalAmount: number;
  successRate: number;
}

export interface PaymentMethodStatsDto {
  method: string;
  methodName: string;
  transactionCount: number;
  totalAmount: number;
  successRate: number;
  color: string;
}

export interface RecentTransactionDto {
  sessionId: string;
  orderId: string;
  amount: number;
  currency: string;
  status: string;
  method: string;
  creationTime: string;
  customerEmail: string;
  boothNumber: string;
}

export enum ExportFormat {
  Excel = 'Excel',
  Pdf = 'Pdf',
  Csv = 'Csv'
}