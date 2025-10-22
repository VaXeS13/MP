using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using MP.Application.Contracts.CustomerDashboard;
using MP.Domain.Rentals;
using MP.Domain.Booths;
using MP.Domain.Notifications;
using MP.Domain.Settlements;
using MP.Domain.Items;
using MP.Permissions;
using MP.Rentals;

namespace MP.Application.CustomerDashboard
{
    [Authorize]
    public class CustomerDashboardAppService : ApplicationService, ICustomerDashboardAppService
    {
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly IRepository<ItemSheetItem, Guid> _itemSheetItemRepository;
        private readonly IRepository<ItemSheet, Guid> _itemSheetRepository;
        private readonly IRepository<Booth, Guid> _boothRepository;
        private readonly IUserNotificationRepository _notificationRepository;
        private readonly ISettlementRepository _settlementRepository;

        public CustomerDashboardAppService(
            IRepository<Rental, Guid> rentalRepository,
            IRepository<ItemSheetItem, Guid> itemSheetItemRepository,
            IRepository<ItemSheet, Guid> itemSheetRepository,
            IRepository<Booth, Guid> boothRepository,
            IUserNotificationRepository notificationRepository,
            ISettlementRepository settlementRepository)
        {
            _rentalRepository = rentalRepository;
            _itemSheetItemRepository = itemSheetItemRepository;
            _itemSheetRepository = itemSheetRepository;
            _boothRepository = boothRepository;
            _notificationRepository = notificationRepository;
            _settlementRepository = settlementRepository;
        }

        [Authorize(MPPermissions.CustomerDashboard.ViewDashboard)]
        public async Task<CustomerDashboardDto> GetDashboardAsync()
        {
            var userId = CurrentUser.GetId();
            var now = DateTime.Today;

            // Get active rentals
            var activeRentals = await GetMyActiveRentalsAsync();

            // Get sales statistics
            var salesStats = await GetSalesStatisticsAsync(new CustomerStatisticsFilterDto());

            // Get settlement summary
            var settlementSummary = await GetSettlementSummaryAsync();

            // Get recent notifications
            var notifications = await _notificationRepository.GetUserNotificationsAsync(
                userId, isRead: false, includeExpired: false, maxResultCount: 5);

            // Get recent sales
            var itemSheetItemQueryable = await _itemSheetItemRepository.GetQueryableAsync();
            var itemSheetQueryable = await _itemSheetRepository.GetQueryableAsync();

            var recentSalesQuery = from isi in itemSheetItemQueryable
                                   join sheet in itemSheetQueryable on isi.ItemSheetId equals sheet.Id
                                   where sheet.UserId == userId &&
                                         isi.Status == ItemSheetItemStatus.Sold &&
                                         isi.SoldAt.HasValue &&
                                         sheet.RentalId.HasValue
                                   orderby isi.SoldAt descending
                                   select new RecentItemSaleDto
                                   {
                                       ItemId = isi.Id,
                                       ItemName = isi.Item.Name,
                                       Category = isi.Item.Category,
                                       SalePrice = isi.Item.Price,
                                       CommissionAmount = isi.GetCommissionAmount(isi.Item.Price),
                                       CustomerAmount = isi.GetCustomerAmount(isi.Item.Price),
                                       SoldAt = isi.SoldAt!.Value,
                                       BoothNumber = sheet.Rental!.Booth.Number
                                   };

            var recentSales = await AsyncExecuter.ToListAsync(recentSalesQuery.Take(10));

            // Build overview
            var totalItemsForSale = await AsyncExecuter.CountAsync(
                from isi in itemSheetItemQueryable
                join sheet in itemSheetQueryable on isi.ItemSheetId equals sheet.Id
                where sheet.UserId == userId && isi.Status == ItemSheetItemStatus.ForSale
                select isi);

            var totalItemsSold = await AsyncExecuter.CountAsync(
                from isi in itemSheetItemQueryable
                join sheet in itemSheetQueryable on isi.ItemSheetId equals sheet.Id
                where sheet.UserId == userId && isi.Status == ItemSheetItemStatus.Sold
                select isi);

            var overview = new CustomerOverviewDto
            {
                TotalActiveRentals = activeRentals.Count,
                TotalItemsForSale = totalItemsForSale,
                TotalItemsSold = totalItemsSold,
                TotalSalesAmount = salesStats.AllTimeSales,
                TotalCommissionPaid = salesStats.AllTimeSales * 0.1m, // Placeholder
                AvailableForWithdrawal = settlementSummary.AvailableForWithdrawal,
                DaysUntilNextRentalExpiration = activeRentals.Any() ?
                    activeRentals.Min(r => r.DaysRemaining) : 0,
                HasExpiringRentals = activeRentals.Any(r => r.IsExpiringSoon),
                HasPendingSettlements = settlementSummary.PendingItemsCount > 0
            };

            return new CustomerDashboardDto
            {
                Overview = overview,
                ActiveRentals = activeRentals,
                SalesStatistics = salesStats,
                RecentSales = recentSales,
                RecentNotifications = ObjectMapper.Map<List<UserNotification>, List<CustomerNotificationDto>>(notifications),
                SettlementSummary = settlementSummary
            };
        }

        [Authorize(MPPermissions.CustomerDashboard.ViewStatistics)]
        public async Task<CustomerSalesStatisticsDto> GetSalesStatisticsAsync(CustomerStatisticsFilterDto filter)
        {
            var userId = CurrentUser.GetId();
            var now = DateTime.Today;

            var itemSheetItemQueryable = await _itemSheetItemRepository.GetQueryableAsync();
            var itemSheetQueryable = await _itemSheetRepository.GetQueryableAsync();

            var soldItemsQuery = from isi in itemSheetItemQueryable
                                 join sheet in itemSheetQueryable on isi.ItemSheetId equals sheet.Id
                                 where sheet.UserId == userId &&
                                       isi.Status == ItemSheetItemStatus.Sold
                                 select isi;

            var todaySales = await AsyncExecuter.SumAsync(
                soldItemsQuery.Where(isi => isi.SoldAt.HasValue && isi.SoldAt.Value.Date == now),
                isi => isi.Item.Price);

            var weekStart = now.AddDays(-(int)now.DayOfWeek);
            var weekSales = await AsyncExecuter.SumAsync(
                soldItemsQuery.Where(isi => isi.SoldAt.HasValue && isi.SoldAt.Value.Date >= weekStart),
                isi => isi.Item.Price);

            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthSales = await AsyncExecuter.SumAsync(
                soldItemsQuery.Where(isi => isi.SoldAt.HasValue && isi.SoldAt.Value.Date >= monthStart),
                isi => isi.Item.Price);

            var allTimeSales = await AsyncExecuter.SumAsync(soldItemsQuery, isi => isi.Item.Price);
            var allTimeCount = await AsyncExecuter.CountAsync(soldItemsQuery);

            return new CustomerSalesStatisticsDto
            {
                TodaySales = todaySales,
                WeekSales = weekSales,
                MonthSales = monthSales,
                AllTimeSales = allTimeSales,
                AllTimeItemsSold = allTimeCount,
                AverageSalePrice = allTimeCount > 0 ? allTimeSales / allTimeCount : 0,
                Last30DaysSales = new List<DailySalesChartDto>(),
                SalesByCategory = new List<CategorySalesDto>()
            };
        }

        [Authorize(MPPermissions.CustomerDashboard.ManageMyRentals)]
        public async Task<List<MyActiveRentalDto>> GetMyActiveRentalsAsync()
        {
            var userId = CurrentUser.GetId();
            var now = DateTime.Today;

            var queryable = await _rentalRepository.GetQueryableAsync();
            var boothQueryable = await _boothRepository.GetQueryableAsync();

            var query = from r in queryable
                        join b in boothQueryable on r.BoothId equals b.Id
                        where r.UserId == userId &&
                              (r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended)
                        orderby r.Period.EndDate
                        select new MyActiveRentalDto
                        {
                            RentalId = r.Id,
                            BoothNumber = b.Number,
                            BoothTypeName = r.BoothType.Name,
                            StartDate = r.Period.StartDate,
                            EndDate = r.Period.EndDate,
                            DaysRemaining = (int)(r.Period.EndDate - now).TotalDays,
                            IsExpiringSoon = (r.Period.EndDate - now).TotalDays <= 7,
                            Status = r.Status.ToString(),
                            TotalItems = r.GetItemsCount(),
                            SoldItems = r.GetSoldItemsCount(),
                            AvailableItems = r.GetItemsCount() - r.GetSoldItemsCount(),
                            TotalSales = r.GetTotalSalesAmount(),
                            TotalCommission = r.GetTotalCommissionEarned(),
                            CanExtend = true
                        };

            return await AsyncExecuter.ToListAsync(query);
        }

        [Authorize(MPPermissions.CustomerDashboard.ManageMyRentals)]
        public async Task<PagedResultDto<MyActiveRentalDto>> GetMyActiveRentalsPagedAsync(PagedAndSortedResultRequestDto input)
        {
            var userId = CurrentUser.GetId();
            var now = DateTime.Today;

            var queryable = await _rentalRepository.GetQueryableAsync();
            var boothQueryable = await _boothRepository.GetQueryableAsync();

            var query = from r in queryable
                        join b in boothQueryable on r.BoothId equals b.Id
                        where r.UserId == userId &&
                              (r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended)
                        orderby r.Period.EndDate descending
                        select new MyActiveRentalDto
                        {
                            RentalId = r.Id,
                            BoothNumber = b.Number,
                            BoothTypeName = r.BoothType.Name,
                            StartDate = r.Period.StartDate,
                            EndDate = r.Period.EndDate,
                            DaysRemaining = (int)(r.Period.EndDate - now).TotalDays,
                            IsExpiringSoon = (r.Period.EndDate - now).TotalDays <= 7,
                            Status = r.Status.ToString(),
                            TotalItems = r.GetItemsCount(),
                            SoldItems = r.GetSoldItemsCount(),
                            AvailableItems = r.GetItemsCount() - r.GetSoldItemsCount(),
                            TotalSales = r.GetTotalSalesAmount(),
                            TotalCommission = r.GetTotalCommissionEarned(),
                            CanExtend = true
                        };

            var totalCount = await AsyncExecuter.CountAsync(query);
            var rentals = await AsyncExecuter.ToListAsync(
                query.Skip(input.SkipCount).Take(input.MaxResultCount));

            return new PagedResultDto<MyActiveRentalDto>(totalCount, rentals);
        }

        [Authorize(MPPermissions.CustomerDashboard.RequestSettlement)]
        public async Task<SettlementSummaryDto> GetSettlementSummaryAsync()
        {
            var userId = CurrentUser.GetId();

            var totalEarnings = await _settlementRepository.GetUserTotalEarningsAsync(
                userId, Domain.Settlements.SettlementStatus.Completed);

            var pendingSettlement = await _settlementRepository.GetUserTotalEarningsAsync(
                userId, Domain.Settlements.SettlementStatus.Pending);

            return new SettlementSummaryDto
            {
                TotalEarnings = totalEarnings,
                NetEarnings = totalEarnings,
                PendingSettlement = pendingSettlement,
                AvailableForWithdrawal = totalEarnings - pendingSettlement,
                RecentSettlements = new List<SettlementItemDto>()
            };
        }

        public async Task<PagedResultDto<SettlementItemDto>> GetMySettlementsAsync(PagedAndSortedResultRequestDto input)
        {
            var userId = CurrentUser.GetId();

            var settlements = await _settlementRepository.GetUserSettlementsAsync(
                userId,
                skipCount: input.SkipCount,
                maxResultCount: input.MaxResultCount,
                sorting: input.Sorting);

            var count = await _settlementRepository.GetUserSettlementsCountAsync(userId);

            return new PagedResultDto<SettlementItemDto>(
                count,
                ObjectMapper.Map<List<Settlement>, List<SettlementItemDto>>(settlements));
        }

        public async Task<SettlementItemDto> RequestSettlementAsync(RequestSettlementDto input)
        {
            throw new NotImplementedException("Settlement request will be implemented in next phase");
        }

        public async Task<PagedResultDto<CustomerNotificationDto>> GetMyNotificationsAsync(PagedAndSortedResultRequestDto input)
        {
            var userId = CurrentUser.GetId();

            var notifications = await _notificationRepository.GetUserNotificationsAsync(
                userId,
                maxResultCount: input.MaxResultCount);

            return new PagedResultDto<CustomerNotificationDto>(
                notifications.Count,
                ObjectMapper.Map<List<UserNotification>, List<CustomerNotificationDto>>(notifications));
        }

        public async Task MarkNotificationAsReadAsync(Guid notificationId)
        {
            var notification = await _notificationRepository.GetAsync(notificationId);
            notification.MarkAsRead();
            await _notificationRepository.UpdateAsync(notification);
        }

        public async Task MarkAllNotificationsAsReadAsync()
        {
            var userId = CurrentUser.GetId();
            await _notificationRepository.MarkAllAsReadAsync(userId);
        }

        public async Task<QRCodeDto> GetBoothQRCodeAsync(Guid rentalId)
        {
            throw new NotImplementedException("QR Code generation will be implemented in next phase");
        }
    }
}
