using System;
using System.Collections.Generic;
using MP.Domain.Settlements;
using MP.Domain.Notifications;

namespace MP.Application.Contracts.CustomerDashboard
{
    /// <summary>
    /// Main customer dashboard overview
    /// </summary>
    public class CustomerDashboardDto
    {
        public CustomerOverviewDto Overview { get; set; } = null!;
        public List<MyActiveRentalDto> ActiveRentals { get; set; } = new();
        public CustomerSalesStatisticsDto SalesStatistics { get; set; } = null!;
        public List<RecentItemSaleDto> RecentSales { get; set; } = new();
        public List<CustomerNotificationDto> RecentNotifications { get; set; } = new();
        public SettlementSummaryDto SettlementSummary { get; set; } = null!;
    }

    /// <summary>
    /// Customer overview statistics
    /// </summary>
    public class CustomerOverviewDto
    {
        public int TotalActiveRentals { get; set; }
        public int TotalItemsForSale { get; set; }
        public int TotalItemsSold { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalCommissionPaid { get; set; }
        public decimal AvailableForWithdrawal { get; set; }
        public int DaysUntilNextRentalExpiration { get; set; }
        public bool HasExpiringRentals { get; set; }
        public bool HasPendingSettlements { get; set; }
    }

    /// <summary>
    /// Active rental summary for customer
    /// </summary>
    public class MyActiveRentalDto
    {
        public Guid RentalId { get; set; }
        public string BoothNumber { get; set; } = null!;
        public string BoothTypeName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsExpiringSoon { get; set; } // Less than 7 days
        public string Status { get; set; } = null!;
        public int TotalItems { get; set; }
        public int SoldItems { get; set; }
        public int AvailableItems { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalCommission { get; set; }
        public bool CanExtend { get; set; }
        public string? QRCodeUrl { get; set; }
    }

    /// <summary>
    /// Customer sales statistics
    /// </summary>
    public class CustomerSalesStatisticsDto
    {
        public decimal TodaySales { get; set; }
        public decimal WeekSales { get; set; }
        public decimal MonthSales { get; set; }
        public decimal AllTimeSales { get; set; }

        public int TodayItemsSold { get; set; }
        public int WeekItemsSold { get; set; }
        public int MonthItemsSold { get; set; }
        public int AllTimeItemsSold { get; set; }

        public decimal AverageSalePrice { get; set; }
        public decimal HighestSalePrice { get; set; }
        public decimal LowestSalePrice { get; set; }

        public List<DailySalesChartDto> Last30DaysSales { get; set; } = new();
        public List<CategorySalesDto> SalesByCategory { get; set; } = new();
        public decimal MonthlyGrowthPercentage { get; set; }
    }

    /// <summary>
    /// Daily sales for chart
    /// </summary>
    public class DailySalesChartDto
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public int ItemCount { get; set; }
    }

    /// <summary>
    /// Sales by category
    /// </summary>
    public class CategorySalesDto
    {
        public string Category { get; set; } = null!;
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Recent item sale
    /// </summary>
    public class RecentItemSaleDto
    {
        public Guid ItemId { get; set; }
        public string ItemName { get; set; } = null!;
        public string? Category { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal CustomerAmount { get; set; }
        public DateTime SoldAt { get; set; }
        public string BoothNumber { get; set; } = null!;
    }

    /// <summary>
    /// Settlement summary
    /// </summary>
    public class SettlementSummaryDto
    {
        public decimal TotalEarnings { get; set; }
        public decimal TotalCommissionPaid { get; set; }
        public decimal NetEarnings { get; set; }
        public decimal PendingSettlement { get; set; }
        public decimal AvailableForWithdrawal { get; set; }
        public int PendingItemsCount { get; set; }
        public DateTime? LastSettlementDate { get; set; }
        public List<SettlementItemDto> RecentSettlements { get; set; } = new();
    }

    /// <summary>
    /// Individual settlement item
    /// </summary>
    public class SettlementItemDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Amount { get; set; }
        public SettlementStatus Status { get; set; }
        public string StatusDisplayName { get; set; } = null!;
        public int ItemsCount { get; set; }
        public string? Notes { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? TransactionReference { get; set; }
    }

    /// <summary>
    /// Customer notification
    /// </summary>
    public class CustomerNotificationDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? ActionUrl { get; set; }
        public NotificationSeverity Severity { get; set; }
    }
}
