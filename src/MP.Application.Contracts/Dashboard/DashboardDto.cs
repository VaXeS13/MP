using System;
using System.Collections.Generic;

namespace MP.Application.Contracts.Dashboard
{
    public class DashboardOverviewDto
    {
        public SalesOverviewDto SalesOverview { get; set; } = null!;
        public BoothOccupancyOverviewDto BoothOccupancy { get; set; } = null!;
        public FinancialOverviewDto Financial { get; set; } = null!;
        public PaymentAnalyticsDto PaymentAnalytics { get; set; } = null!;
        public List<RecentRentalDto> RecentRentals { get; set; } = new();
        public List<TopSellingItemDto> TopSellingItems { get; set; } = new();
    }

    public class SalesOverviewDto
    {
        public decimal TodaySales { get; set; }
        public decimal WeekSales { get; set; }
        public decimal MonthSales { get; set; }
        public decimal YearSales { get; set; }
        public int TotalItemsSoldToday { get; set; }
        public int TotalItemsSoldWeek { get; set; }
        public int TotalItemsSoldMonth { get; set; }
        public decimal AverageSalePerItem { get; set; }
        public decimal SalesGrowthPercentage { get; set; } // vs previous period
        public List<DailySalesDto> Last30DaysSales { get; set; } = new();
    }

    public class BoothOccupancyOverviewDto
    {
        public int TotalBooths { get; set; }
        public int OccupiedBooths { get; set; }
        public int AvailableBooths { get; set; }
        public int ReservedBooths { get; set; }
        public int MaintenanceBooths { get; set; }
        public decimal OccupancyRate { get; set; } // percentage
        public int RentalsThisMonth { get; set; }
        public decimal AverageRentalDuration { get; set; } // in days
        public decimal MonthlyRentalRevenue { get; set; }
        public List<BoothOccupancyTimelineDto> OccupancyTimeline { get; set; } = new();
    }

    public class FinancialOverviewDto
    {
        public decimal MonthlyRevenue { get; set; }
        public decimal MonthlyRentalIncome { get; set; }
        public decimal MonthlyCommissionIncome { get; set; }
        public decimal PendingPayments { get; set; }
        public decimal OutstandingDebts { get; set; }
        public decimal ProcessingPayments { get; set; }
        public List<MonthlyRevenueDto> MonthlyRevenueChart { get; set; } = new();
        public List<RevenueSourceDto> RevenueBreakdown { get; set; } = new();
    }

    public class DailySalesDto
    {
        public DateTime Date { get; set; }
        public decimal SalesAmount { get; set; }
        public int ItemsSold { get; set; }
    }

    public class BoothOccupancyTimelineDto
    {
        public DateTime Date { get; set; }
        public int OccupiedBooths { get; set; }
        public int TotalBooths { get; set; }
        public decimal OccupancyRate { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; } = null!; // "2024-01"
        public string MonthName { get; set; } = null!; // "January 2024"
        public decimal TotalRevenue { get; set; }
        public decimal RentalIncome { get; set; }
        public decimal CommissionIncome { get; set; }
    }

    public class RevenueSourceDto
    {
        public string Source { get; set; } = null!; // "Booth Rentals", "Sales Commission", etc.
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
        public string Color { get; set; } = null!; // for charts
    }

    public class RecentRentalDto
    {
        public Guid RentalId { get; set; }
        public string BoothNumber { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalCost { get; set; }
        public string Status { get; set; } = null!;
        public int DaysRemaining { get; set; }
    }

    public class TopSellingItemDto
    {
        public string ItemName { get; set; } = null!;
        public string BoothNumber { get; set; } = null!;
        public decimal SalePrice { get; set; }
        public DateTime SoldDate { get; set; }
        public string BoothTypeName { get; set; } = null!;
        public decimal CommissionEarned { get; set; }
    }

    public class CustomerAnalyticsDto
    {
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int ActiveRentals { get; set; }
        public List<TopCustomerDto> TopCustomers { get; set; } = new();
        public List<CustomerRegistrationTimelineDto> RegistrationTimeline { get; set; } = new();
    }

    public class TopCustomerDto
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public decimal TotalSpent { get; set; }
        public int RentalsCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal CommissionGenerated { get; set; }
        public DateTime LastRentalDate { get; set; }
    }

    public class CustomerRegistrationTimelineDto
    {
        public DateTime Date { get; set; }
        public int NewRegistrations { get; set; }
    }

    public class PeriodFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public PeriodType Period { get; set; } = PeriodType.Month;
    }

    public class PaymentAnalyticsDto
    {
        public int TotalTransactions { get; set; }
        public int CompletedTransactions { get; set; }
        public int ProcessingTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal SuccessRate { get; set; } // percentage
        public decimal AverageTransactionValue { get; set; }
        public decimal TotalProcessedAmount { get; set; }
        public decimal AverageProcessingTime { get; set; } // in minutes
        public List<DailyTransactionStatsDto> TransactionTimeline { get; set; } = new();
        public List<PaymentMethodStatsDto> PaymentMethodBreakdown { get; set; } = new();
        public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
    }

    public class DailyTransactionStatsDto
    {
        public DateTime Date { get; set; }
        public int TotalTransactions { get; set; }
        public int CompletedTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SuccessRate { get; set; }
    }

    public class PaymentMethodStatsDto
    {
        public string Method { get; set; } = null!; // "154", "25", etc.
        public string MethodName { get; set; } = null!; // "BLIK", "Card", etc.
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SuccessRate { get; set; }
        public string Color { get; set; } = null!;
    }

    public class RecentTransactionDto
    {
        public string SessionId { get; set; } = null!;
        public string OrderId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string Method { get; set; } = null!;
        public DateTime CreationTime { get; set; }
        public string CustomerEmail { get; set; } = null!;
        public string BoothNumber { get; set; } = null!;
    }

    public enum PeriodType
    {
        Day,
        Week,
        Month,
        Quarter,
        Year,
        Custom
    }
}