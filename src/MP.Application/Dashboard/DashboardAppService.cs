using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using MP.Application.Contracts.Dashboard;
using MP.Domain.Rentals;
using MP.Domain.Booths;
using MP.Domain.BoothTypes;
using MP.Domain.Payments;
using MP.Domain.Items;
using MP.Permissions;
using MP.Rentals;

namespace MP.Application.Dashboard
{
    [Authorize(MPPermissions.Dashboard.Default)]
    public class DashboardAppService : ApplicationService, IDashboardAppService
    {
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IRepository<ItemSheetItem, Guid> _itemSheetItemRepository;
        private readonly IRepository<ItemSheet, Guid> _itemSheetRepository;
        private readonly IRepository<Booth, Guid> _boothRepository;
        private readonly IRepository<BoothType, Guid> _boothTypeRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IP24TransactionRepository _p24TransactionRepository;

        public DashboardAppService(
            IRepository<Rental, Guid> rentalRepository,
            IRepository<ItemSheetItem, Guid> itemSheetItemRepository,
            IRepository<ItemSheet, Guid> itemSheetRepository,
            IRepository<Booth, Guid> boothRepository,
            IRepository<BoothType, Guid> boothTypeRepository,
            IRepository<IdentityUser, Guid> userRepository,
            IP24TransactionRepository p24TransactionRepository)
        {
            _rentalRepository = rentalRepository;
            _itemSheetItemRepository = itemSheetItemRepository;
            _itemSheetRepository = itemSheetRepository;
            _boothRepository = boothRepository;
            _boothTypeRepository = boothTypeRepository;
            _userRepository = userRepository;
            _p24TransactionRepository = p24TransactionRepository;
        }

        public async Task<DashboardOverviewDto> GetOverviewAsync(PeriodFilterDto filter)
        {
            var overview = new DashboardOverviewDto
            {
                SalesOverview = await GetSalesAnalyticsAsync(filter),
                BoothOccupancy = await GetBoothOccupancyAsync(filter),
                Financial = await GetFinancialReportsAsync(filter),
                PaymentAnalytics = await GetPaymentAnalyticsAsync(filter),
                RecentRentals = await GetRecentRentalsAsync(),
                TopSellingItems = await GetTopSellingItemsAsync(filter)
            };

            return overview;
        }

        [Authorize(MPPermissions.Dashboard.ViewSalesAnalytics)]
        public async Task<SalesOverviewDto> GetSalesAnalyticsAsync(PeriodFilterDto filter)
        {
            var now = DateTime.Today;
            var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfYear = new DateTime(now.Year, 1, 1);

            var queryable = await _itemSheetItemRepository.GetQueryableAsync();
            var soldItems = queryable.Where(isi => isi.Status == ItemSheetItemStatus.Sold);

            var todaySales = await AsyncExecuter.SumAsync(
                soldItems.Where(isi => isi.SoldAt.HasValue && isi.SoldAt.Value.Date == now),
                isi => isi.Item.Price);

            var weekSales = await AsyncExecuter.SumAsync(
                soldItems.Where(isi => isi.SoldAt.HasValue && isi.SoldAt.Value.Date >= startOfWeek),
                isi => isi.Item.Price);

            var monthSales = await AsyncExecuter.SumAsync(
                soldItems.Where(isi => isi.SoldAt.HasValue && isi.SoldAt.Value.Date >= startOfMonth),
                isi => isi.Item.Price);

            var yearSales = await AsyncExecuter.SumAsync(
                soldItems.Where(isi => isi.SoldAt.HasValue && isi.SoldAt.Value.Date >= startOfYear),
                isi => isi.Item.Price);

            var todayItemsCount = await AsyncExecuter.CountAsync(
                soldItems.Where(isi => isi.SoldAt.HasValue && isi.SoldAt.Value.Date == now));

            var weekItemsCount = await AsyncExecuter.CountAsync(
                soldItems.Where(isi => isi.SoldAt.HasValue && isi.SoldAt.Value.Date >= startOfWeek));

            var monthItemsCount = await AsyncExecuter.CountAsync(
                soldItems.Where(isi => isi.SoldAt.HasValue && isi.SoldAt.Value.Date >= startOfMonth));

            // Calculate last 30 days sales for chart
            var last30DaysSales = new List<DailySalesDto>();
            for (int i = 29; i >= 0; i--)
            {
                var date = now.AddDays(-i);
                var dailySales = await AsyncExecuter.SumAsync(
                    soldItems.Where(isi => isi.SoldAt.HasValue && isi.SoldAt.Value.Date == date),
                    isi => isi.Item.Price);
                var dailyCount = await AsyncExecuter.CountAsync(
                    soldItems.Where(isi => isi.SoldAt.HasValue && isi.SoldAt.Value.Date == date));

                last30DaysSales.Add(new DailySalesDto
                {
                    Date = date,
                    SalesAmount = dailySales,
                    ItemsSold = dailyCount
                });
            }

            // Calculate growth percentage (current month vs previous month)
            var previousMonth = startOfMonth.AddMonths(-1);
            var previousMonthEnd = startOfMonth.AddDays(-1);
            var previousMonthSales = await AsyncExecuter.SumAsync(
                soldItems.Where(isi => isi.SoldAt.HasValue &&
                    isi.SoldAt.Value.Date >= previousMonth &&
                    isi.SoldAt.Value.Date <= previousMonthEnd),
                isi => isi.Item.Price);

            var growthPercentage = previousMonthSales > 0
                ? ((monthSales - previousMonthSales) / previousMonthSales) * 100
                : 0;

            return new SalesOverviewDto
            {
                TodaySales = todaySales,
                WeekSales = weekSales,
                MonthSales = monthSales,
                YearSales = yearSales,
                TotalItemsSoldToday = todayItemsCount,
                TotalItemsSoldWeek = weekItemsCount,
                TotalItemsSoldMonth = monthItemsCount,
                AverageSalePerItem = monthItemsCount > 0 ? monthSales / monthItemsCount : 0,
                SalesGrowthPercentage = growthPercentage,
                Last30DaysSales = last30DaysSales
            };
        }

        [Authorize(MPPermissions.Dashboard.ViewBoothOccupancy)]
        public async Task<BoothOccupancyOverviewDto> GetBoothOccupancyAsync(PeriodFilterDto filter)
        {
            var totalBooths = await _boothRepository.CountAsync();
            var now = DateTime.Today;

            var boothQueryable = await _boothRepository.GetQueryableAsync();
            var rentalQueryable = await _rentalRepository.GetQueryableAsync();
            var activeRentals = rentalQueryable.Where(r =>
                r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended);

            var occupiedBooths = await AsyncExecuter.CountAsync(
                activeRentals.Where(r => r.Period.StartDate <= now && r.Period.EndDate >= now));

            var availableBooths = await AsyncExecuter.CountAsync(
                boothQueryable.Where(b => b.Status == BoothStatus.Available));

            var reservedBooths = await AsyncExecuter.CountAsync(
                boothQueryable.Where(b => b.Status == BoothStatus.Reserved));

            var maintenanceBooths = await AsyncExecuter.CountAsync(
                boothQueryable.Where(b => b.Status == BoothStatus.Maintenance));

            var occupancyRate = totalBooths > 0 ? (decimal)occupiedBooths / totalBooths * 100 : 0;

            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var rentalsThisMonth = await AsyncExecuter.CountAsync(
                rentalQueryable.Where(r => r.CreationTime >= startOfMonth));

            var allRentals = await AsyncExecuter.ToListAsync(
                rentalQueryable.Where(r => r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended));

            var avgRentalDuration = allRentals.Any()
                ? (decimal)allRentals.Average(r => r.Period.GetDaysCount())
                : 0;

            var monthlyRentalRevenue = await AsyncExecuter.SumAsync(
                rentalQueryable.Where(r => r.CreationTime >= startOfMonth && r.Payment.PaymentStatus == PaymentStatus.Completed),
                r => r.Payment.TotalAmount);

            // Generate occupancy timeline for last 30 days
            var occupancyTimeline = new List<BoothOccupancyTimelineDto>();
            for (int i = 29; i >= 0; i--)
            {
                var date = now.AddDays(-i);
                var occupiedOnDate = await AsyncExecuter.CountAsync(
                    activeRentals.Where(r => r.Period.StartDate <= date && r.Period.EndDate >= date));

                occupancyTimeline.Add(new BoothOccupancyTimelineDto
                {
                    Date = date,
                    OccupiedBooths = occupiedOnDate,
                    TotalBooths = totalBooths,
                    OccupancyRate = totalBooths > 0 ? (decimal)occupiedOnDate / totalBooths * 100 : 0
                });
            }

            return new BoothOccupancyOverviewDto
            {
                TotalBooths = totalBooths,
                OccupiedBooths = occupiedBooths,
                AvailableBooths = availableBooths,
                ReservedBooths = reservedBooths,
                MaintenanceBooths = maintenanceBooths,
                OccupancyRate = occupancyRate,
                RentalsThisMonth = rentalsThisMonth,
                AverageRentalDuration = avgRentalDuration,
                MonthlyRentalRevenue = monthlyRentalRevenue,
                OccupancyTimeline = occupancyTimeline
            };
        }

        [Authorize(MPPermissions.Dashboard.ViewFinancialReports)]
        public async Task<FinancialOverviewDto> GetFinancialReportsAsync(PeriodFilterDto filter)
        {
            var now = DateTime.Today;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var rentalQueryable = await _rentalRepository.GetQueryableAsync();
            var itemSheetItemQueryable = await _itemSheetItemRepository.GetQueryableAsync();

            // Monthly rental income
            var monthlyRentalIncome = await AsyncExecuter.SumAsync(
                rentalQueryable.Where(r => r.CreationTime >= startOfMonth && r.Payment.PaymentStatus == PaymentStatus.Completed),
                r => r.Payment.TotalAmount);

            // Monthly commission income
            var monthlyCommissionIncome = await AsyncExecuter.SumAsync(
                itemSheetItemQueryable.Where(isi => isi.Status == ItemSheetItemStatus.Sold &&
                    isi.SoldAt.HasValue && isi.SoldAt.Value >= startOfMonth),
                isi => isi.Item.Price * (isi.CommissionPercentage / 100));

            var monthlyRevenue = monthlyRentalIncome + monthlyCommissionIncome;

            // Pending payments
            var pendingPayments = await AsyncExecuter.SumAsync(
                rentalQueryable.Where(r => r.Payment.PaymentStatus != PaymentStatus.Completed && r.Status != RentalStatus.Cancelled),
                r => r.Payment.TotalAmount - r.Payment.PaidAmount);

            // Processing payments from P24 transactions
            var processingPayments = await AsyncExecuter.SumAsync(
                (await _p24TransactionRepository.GetQueryableAsync())
                    .Where(t => t.Status == "processing" && t.CreationTime >= startOfMonth),
                t => t.Amount);

            // Generate monthly revenue chart for last 12 months
            var monthlyRevenueChart = new List<MonthlyRevenueDto>();
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = startOfMonth.AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);

                var rentalIncomeForMonth = await AsyncExecuter.SumAsync(
                    rentalQueryable.Where(r => r.CreationTime >= monthStart && r.CreationTime < monthEnd && r.Payment.PaymentStatus == PaymentStatus.Completed),
                    r => r.Payment.TotalAmount);

                var commissionIncomeForMonth = await AsyncExecuter.SumAsync(
                    itemSheetItemQueryable.Where(isi => isi.Status == ItemSheetItemStatus.Sold &&
                        isi.SoldAt.HasValue &&
                        isi.SoldAt.Value >= monthStart &&
                        isi.SoldAt.Value < monthEnd),
                    isi => isi.Item.Price * (isi.CommissionPercentage / 100));

                monthlyRevenueChart.Add(new MonthlyRevenueDto
                {
                    Month = monthStart.ToString("yyyy-MM"),
                    MonthName = monthStart.ToString("MMMM yyyy"),
                    TotalRevenue = rentalIncomeForMonth + commissionIncomeForMonth,
                    RentalIncome = rentalIncomeForMonth,
                    CommissionIncome = commissionIncomeForMonth
                });
            }

            // Revenue breakdown
            var revenueBreakdown = new List<RevenueSourceDto>
            {
                new RevenueSourceDto
                {
                    Source = "Booth Rentals",
                    Amount = monthlyRentalIncome,
                    Percentage = monthlyRevenue > 0 ? (monthlyRentalIncome / monthlyRevenue) * 100 : 0,
                    Color = "#007bff"
                },
                new RevenueSourceDto
                {
                    Source = "Sales Commission",
                    Amount = monthlyCommissionIncome,
                    Percentage = monthlyRevenue > 0 ? (monthlyCommissionIncome / monthlyRevenue) * 100 : 0,
                    Color = "#28a745"
                }
            };

            return new FinancialOverviewDto
            {
                MonthlyRevenue = monthlyRevenue,
                MonthlyRentalIncome = monthlyRentalIncome,
                MonthlyCommissionIncome = monthlyCommissionIncome,
                PendingPayments = pendingPayments,
                ProcessingPayments = processingPayments,
                OutstandingDebts = pendingPayments, // Same as pending for now
                MonthlyRevenueChart = monthlyRevenueChart,
                RevenueBreakdown = revenueBreakdown
            };
        }

        [Authorize(MPPermissions.Dashboard.ViewCustomerAnalytics)]
        public async Task<CustomerAnalyticsDto> GetCustomerAnalyticsAsync(PeriodFilterDto filter)
        {
            var totalCustomers = await _userRepository.CountAsync();
            var now = DateTime.Today;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var newCustomersThisMonth = await _userRepository.CountAsync(u => u.CreationTime >= startOfMonth);

            var rentalQueryable = await _rentalRepository.GetQueryableAsync();
            var activeRentals = await AsyncExecuter.CountAsync(
                rentalQueryable.Where(r => r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended));

            // Get top customers
            var customerRentals = await AsyncExecuter.ToListAsync(
                rentalQueryable.Where(r => r.Payment.PaymentStatus == PaymentStatus.Completed)
                    .GroupBy(r => r.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        TotalSpent = g.Sum(r => r.Payment.TotalAmount),
                        RentalsCount = g.Count(),
                        LastRentalDate = g.Max(r => r.CreationTime)
                    })
                    .OrderByDescending(c => c.TotalSpent)
                    .Take(10));

            var topCustomers = new List<TopCustomerDto>();
            foreach (var customerData in customerRentals)
            {
                var user = await _userRepository.GetAsync(customerData.UserId);

                // Get total sales and commission for this customer
                var customerRentalIds = await AsyncExecuter.ToListAsync(
                    rentalQueryable.Where(r => r.UserId == customerData.UserId).Select(r => r.Id));

                var itemSheetQueryable = await _itemSheetRepository.GetQueryableAsync();
                var customerItemSheetIds = await AsyncExecuter.ToListAsync(
                    itemSheetQueryable.Where(sheet => customerRentalIds.Contains(sheet.RentalId ?? Guid.Empty))
                        .Select(sheet => sheet.Id));

                var totalSales = await AsyncExecuter.SumAsync(
                    (await _itemSheetItemRepository.GetQueryableAsync())
                        .Where(isi => customerItemSheetIds.Contains(isi.ItemSheetId) && isi.Status == ItemSheetItemStatus.Sold),
                    isi => isi.Item.Price);

                var commissionGenerated = await AsyncExecuter.SumAsync(
                    (await _itemSheetItemRepository.GetQueryableAsync())
                        .Where(isi => customerItemSheetIds.Contains(isi.ItemSheetId) &&
                            isi.Status == ItemSheetItemStatus.Sold),
                    isi => isi.Item.Price * (isi.CommissionPercentage / 100));

                topCustomers.Add(new TopCustomerDto
                {
                    UserId = customerData.UserId,
                    Name = user.Name ?? user.UserName ?? "",
                    Email = user.Email ?? "",
                    TotalSpent = customerData.TotalSpent,
                    RentalsCount = customerData.RentalsCount,
                    TotalSales = totalSales,
                    CommissionGenerated = commissionGenerated,
                    LastRentalDate = customerData.LastRentalDate
                });
            }

            // Registration timeline
            var registrationTimeline = new List<CustomerRegistrationTimelineDto>();
            for (int i = 29; i >= 0; i--)
            {
                var date = now.AddDays(-i);
                var newRegs = await _userRepository.CountAsync(u => u.CreationTime.Date == date);
                registrationTimeline.Add(new CustomerRegistrationTimelineDto
                {
                    Date = date,
                    NewRegistrations = newRegs
                });
            }

            return new CustomerAnalyticsDto
            {
                TotalCustomers = totalCustomers,
                NewCustomersThisMonth = newCustomersThisMonth,
                ActiveRentals = activeRentals,
                TopCustomers = topCustomers,
                RegistrationTimeline = registrationTimeline
            };
        }

        public async Task<byte[]> ExportSalesReportAsync(PeriodFilterDto filter, ExportFormat format)
        {
            // TODO: Implement export functionality
            throw new NotImplementedException("Export functionality will be implemented in next phase");
        }

        public async Task<byte[]> ExportFinancialReportAsync(PeriodFilterDto filter, ExportFormat format)
        {
            // TODO: Implement export functionality
            throw new NotImplementedException("Export functionality will be implemented in next phase");
        }

        private async Task<List<RecentRentalDto>> GetRecentRentalsAsync()
        {
            var queryable = await _rentalRepository.GetQueryableAsync();
            var recentRentals = await AsyncExecuter.ToListAsync(
                queryable.OrderByDescending(r => r.CreationTime)
                    .Take(10)
                    .Select(r => new RecentRentalDto
                    {
                        RentalId = r.Id,
                        BoothNumber = r.Booth.Number,
                        CustomerName = r.User.Name ?? r.User.UserName ?? "",
                        CustomerEmail = r.User.Email ?? "",
                        StartDate = r.Period.StartDate,
                        EndDate = r.Period.EndDate,
                        TotalCost = r.Payment.TotalAmount,
                        Status = r.Status.ToString(),
                        DaysRemaining = (int)(r.Period.EndDate - DateTime.Today).TotalDays
                    }));

            return recentRentals;
        }

        private async Task<List<TopSellingItemDto>> GetTopSellingItemsAsync(PeriodFilterDto filter)
        {
            var itemSheetItemQueryable = await _itemSheetItemRepository.GetQueryableAsync();
            var itemSheetQueryable = await _itemSheetRepository.GetQueryableAsync();
            var rentalQueryable = await _rentalRepository.GetQueryableAsync();
            var boothQueryable = await _boothRepository.GetQueryableAsync();
            var boothTypeQueryable = await _boothTypeRepository.GetQueryableAsync();

            var query = from isi in itemSheetItemQueryable.Where(isi => isi.Status == ItemSheetItemStatus.Sold)
                        join sheet in itemSheetQueryable on isi.ItemSheetId equals sheet.Id
                        join r in rentalQueryable on sheet.RentalId equals r.Id
                        join b in boothQueryable on r.BoothId equals b.Id
                        join bt in boothTypeQueryable on r.BoothTypeId equals bt.Id
                        orderby isi.Item.Price descending
                        select new TopSellingItemDto
                        {
                            ItemName = isi.Item.Name,
                            BoothNumber = b.Number,
                            SalePrice = isi.Item.Price,
                            SoldDate = isi.SoldAt ?? isi.CreationTime,
                            BoothTypeName = bt.Name,
                            CommissionEarned = isi.Item.Price * isi.CommissionPercentage / 100
                        };

            var topItems = await AsyncExecuter.ToListAsync(query.Take(10));

            return topItems;
        }

        [Authorize(MPPermissions.Dashboard.ViewFinancialReports)]
        public async Task<PaymentAnalyticsDto> GetPaymentAnalyticsAsync(PeriodFilterDto filter)
        {
            var now = DateTime.Today;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var transactionQueryable = await _p24TransactionRepository.GetQueryableAsync();
            var monthlyTransactions = transactionQueryable.Where(t => t.CreationTime >= startOfMonth);

            var totalTransactions = await AsyncExecuter.CountAsync(monthlyTransactions);
            var completedTransactions = await AsyncExecuter.CountAsync(
                monthlyTransactions.Where(t => t.Status == "completed"));
            var processingTransactions = await AsyncExecuter.CountAsync(
                monthlyTransactions.Where(t => t.Status == "processing"));
            var failedTransactions = await AsyncExecuter.CountAsync(
                monthlyTransactions.Where(t => t.Status == "failed"));

            var successRate = totalTransactions > 0 ? (decimal)completedTransactions / totalTransactions * 100 : 0;

            var totalProcessedAmount = await AsyncExecuter.SumAsync(
                monthlyTransactions.Where(t => t.Status == "completed"),
                t => t.Amount);

            var averageTransactionValue = completedTransactions > 0 ? totalProcessedAmount / completedTransactions : 0;

            // Calculate average processing time for completed transactions
            var completedWithTimes = await AsyncExecuter.ToListAsync(
                monthlyTransactions.Where(t => t.Status == "completed" && t.LastStatusCheck.HasValue)
                    .Select(t => new { t.CreationTime, t.LastStatusCheck }));

            var averageProcessingTime = completedWithTimes.Any()
                ? (decimal)completedWithTimes.Average(t => (t.LastStatusCheck!.Value - t.CreationTime).TotalMinutes)
                : 0;

            // Generate transaction timeline for last 30 days
            var transactionTimeline = new List<DailyTransactionStatsDto>();
            for (int i = 29; i >= 0; i--)
            {
                var date = now.AddDays(-i);
                var dailyTransactions = transactionQueryable.Where(t => t.CreationTime.Date == date);

                var dailyTotal = await AsyncExecuter.CountAsync(dailyTransactions);
                var dailyCompleted = await AsyncExecuter.CountAsync(
                    dailyTransactions.Where(t => t.Status == "completed"));
                var dailyFailed = await AsyncExecuter.CountAsync(
                    dailyTransactions.Where(t => t.Status == "failed"));
                var dailyAmount = await AsyncExecuter.SumAsync(
                    dailyTransactions.Where(t => t.Status == "completed"),
                    t => t.Amount);

                transactionTimeline.Add(new DailyTransactionStatsDto
                {
                    Date = date,
                    TotalTransactions = dailyTotal,
                    CompletedTransactions = dailyCompleted,
                    FailedTransactions = dailyFailed,
                    TotalAmount = dailyAmount,
                    SuccessRate = dailyTotal > 0 ? (decimal)dailyCompleted / dailyTotal * 100 : 0
                });
            }

            // Payment method breakdown
            var methodStats = await AsyncExecuter.ToListAsync(
                monthlyTransactions.GroupBy(t => t.Method ?? "Unknown")
                    .Select(g => new
                    {
                        Method = g.Key,
                        Count = g.Count(),
                        Amount = g.Where(t => t.Status == "completed").Sum(t => t.Amount),
                        CompletedCount = g.Count(t => t.Status == "completed")
                    }));

            var paymentMethodBreakdown = methodStats.Select(m => new PaymentMethodStatsDto
            {
                Method = m.Method,
                MethodName = GetPaymentMethodName(m.Method),
                TransactionCount = m.Count,
                TotalAmount = m.Amount,
                SuccessRate = m.Count > 0 ? (decimal)m.CompletedCount / m.Count * 100 : 0,
                Color = GetPaymentMethodColor(m.Method)
            }).ToList();

            // Recent transactions
            var recentTransactions = await AsyncExecuter.ToListAsync(
                transactionQueryable.OrderByDescending(t => t.CreationTime)
                    .Take(10)
                    .Select(t => new RecentTransactionDto
                    {
                        SessionId = t.SessionId,
                        OrderId = t.OrderId ?? "",
                        Amount = t.Amount,
                        Currency = t.Currency,
                        Status = t.Status,
                        Method = t.Method ?? "Unknown",
                        CreationTime = t.CreationTime,
                        CustomerEmail = t.Email,
                        BoothNumber = "" // TODO: Join with rental to get booth number
                    }));

            return new PaymentAnalyticsDto
            {
                TotalTransactions = totalTransactions,
                CompletedTransactions = completedTransactions,
                ProcessingTransactions = processingTransactions,
                FailedTransactions = failedTransactions,
                SuccessRate = successRate,
                AverageTransactionValue = averageTransactionValue,
                TotalProcessedAmount = totalProcessedAmount,
                AverageProcessingTime = averageProcessingTime,
                TransactionTimeline = transactionTimeline,
                PaymentMethodBreakdown = paymentMethodBreakdown,
                RecentTransactions = recentTransactions
            };
        }

        private string GetPaymentMethodName(string methodId)
        {
            return methodId switch
            {
                "154" => "BLIK",
                "25" => "Card Payment",
                "31" => "Bank Transfer",
                "68" => "PayPo",
                _ => $"Method {methodId}"
            };
        }

        private string GetPaymentMethodColor(string methodId)
        {
            return methodId switch
            {
                "154" => "#007bff", // Blue for BLIK
                "25" => "#28a745",  // Green for Cards
                "31" => "#ffc107",  // Yellow for Bank Transfer
                "68" => "#dc3545",  // Red for PayPo
                _ => "#6c757d"       // Gray for Unknown
            };
        }
    }
}