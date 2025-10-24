using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using MP.Application.Contracts.CustomerDashboard;
using MP.Domain.Rentals;
using MP.Domain.Items;
using MP.Domain.Booths;
using MP.Permissions;
using MP.Rentals;
using MP.Domain.OrganizationalUnits;

namespace MP.Application.CustomerDashboard
{
    [Authorize(MPPermissions.CustomerDashboard.ManageMyItems)]
    public class MyItemAppService : ApplicationService, IMyItemAppService
    {
        private readonly IRepository<ItemSheetItem, Guid> _itemSheetItemRepository;
        private readonly IRepository<Item, Guid> _itemRepository;
        private readonly IRepository<ItemSheet, Guid> _itemSheetRepository;
        private readonly IRepository<Rental, Guid> _rentalRepository;
        private readonly ICurrentOrganizationalUnit _currentOrganizationalUnit;

        public MyItemAppService(
            IRepository<ItemSheetItem, Guid> itemSheetItemRepository,
            IRepository<Item, Guid> itemRepository,
            IRepository<ItemSheet, Guid> itemSheetRepository,
            IRepository<Rental, Guid> rentalRepository,
            ICurrentOrganizationalUnit currentOrganizationalUnit)
        {
            _itemSheetItemRepository = itemSheetItemRepository;
            _itemRepository = itemRepository;
            _itemSheetRepository = itemSheetRepository;
            _rentalRepository = rentalRepository;
            _currentOrganizationalUnit = currentOrganizationalUnit;
        }

        public async Task<PagedResultDto<MyItemDto>> GetMyItemsAsync(GetMyItemsDto input)
        {
            var userId = CurrentUser.GetId();

            var itemSheetItemQueryable = await _itemSheetItemRepository.GetQueryableAsync();
            var itemSheetQueryable = await _itemSheetRepository.GetQueryableAsync();

            var query = from isi in itemSheetItemQueryable
                        join sheet in itemSheetQueryable on isi.ItemSheetId equals sheet.Id
                        where sheet.UserId == userId && sheet.RentalId.HasValue
                        select new { ItemSheetItem = isi, ItemSheet = sheet };

            // Apply filters
            if (input.RentalId.HasValue)
            {
                query = query.Where(x => x.ItemSheet.RentalId == input.RentalId.Value);
            }

            if (!string.IsNullOrWhiteSpace(input.Status))
            {
                query = query.Where(x => x.ItemSheetItem.Status.ToString() == input.Status);
            }

            if (!string.IsNullOrWhiteSpace(input.Category))
            {
                query = query.Where(x => x.ItemSheetItem.Item.Category == input.Category);
            }

            if (!string.IsNullOrWhiteSpace(input.SearchTerm))
            {
                var searchTerm = input.SearchTerm.ToLower();
                query = query.Where(x => x.ItemSheetItem.Item.Name.ToLower().Contains(searchTerm));
            }

            var totalCount = await AsyncExecuter.CountAsync(query);

            var items = await AsyncExecuter.ToListAsync(
                query.OrderByDescending(x => x.ItemSheetItem.CreationTime)
                     .Skip(input.SkipCount)
                     .Take(input.MaxResultCount));

            var dtos = items.Select(x => new MyItemDto
            {
                Id = x.ItemSheetItem.Id,
                RentalId = x.ItemSheet.RentalId ?? Guid.Empty,
                BoothNumber = x.ItemSheet.Rental?.Booth.Number ?? "N/A",
                Name = x.ItemSheetItem.Item.Name,
                Description = null, // Not available in new Item model
                Category = x.ItemSheetItem.Item.Category,
                PhotoUrls = new List<string>(), // Not available in new Item model
                ItemNumber = x.ItemSheetItem.ItemNumber,
                Barcode = x.ItemSheetItem.Barcode,
                EstimatedPrice = null, // Not available - only Price exists
                ActualPrice = x.ItemSheetItem.Item.Price,
                CommissionPercentage = x.ItemSheetItem.CommissionPercentage,
                Status = x.ItemSheetItem.Status.ToString(),
                StatusDisplayName = x.ItemSheetItem.Status.ToString(),
                SoldAt = x.ItemSheetItem.SoldAt,
                DaysForSale = x.ItemSheetItem.SoldAt.HasValue ?
                    (int)(x.ItemSheetItem.SoldAt.Value - x.ItemSheetItem.CreationTime).TotalDays :
                    (int)(DateTime.Now - x.ItemSheetItem.CreationTime).TotalDays,
                CommissionAmount = x.ItemSheetItem.GetCommissionAmount(x.ItemSheetItem.Item.Price),
                CustomerAmount = x.ItemSheetItem.GetCustomerAmount(x.ItemSheetItem.Item.Price),
                CanEdit = x.ItemSheetItem.Status == ItemSheetItemStatus.ForSale,
                CanDelete = x.ItemSheetItem.Status == ItemSheetItemStatus.ForSale,
                CreationTime = x.ItemSheetItem.CreationTime
            }).ToList();

            return new PagedResultDto<MyItemDto>(totalCount, dtos);
        }

        public async Task<MyItemDto> GetMyItemAsync(Guid id)
        {
            var userId = CurrentUser.GetId();
            var queryable = await _itemSheetItemRepository.GetQueryableAsync();
            var itemSheetItem = await AsyncExecuter.FirstOrDefaultAsync(
                queryable
                    .Where(x => x.Id == id)
                    .Select(x => new { ItemSheetItem = x, ItemSheet = x.ItemSheet }));

            if (itemSheetItem == null)
            {
                throw new BusinessException("ITEM_NOT_FOUND");
            }

            if (itemSheetItem.ItemSheet.UserId != userId)
            {
                throw new BusinessException("NOT_YOUR_ITEM");
            }

            return new MyItemDto
            {
                Id = itemSheetItem.ItemSheetItem.Id,
                RentalId = itemSheetItem.ItemSheet.RentalId ?? Guid.Empty,
                BoothNumber = itemSheetItem.ItemSheet.Rental?.Booth.Number ?? "N/A",
                Name = itemSheetItem.ItemSheetItem.Item.Name,
                Description = null,
                Category = itemSheetItem.ItemSheetItem.Item.Category,
                PhotoUrls = new List<string>(),
                ItemNumber = itemSheetItem.ItemSheetItem.ItemNumber,
                Barcode = itemSheetItem.ItemSheetItem.Barcode,
                EstimatedPrice = null,
                ActualPrice = itemSheetItem.ItemSheetItem.Item.Price,
                CommissionPercentage = itemSheetItem.ItemSheetItem.CommissionPercentage,
                Status = itemSheetItem.ItemSheetItem.Status.ToString(),
                StatusDisplayName = itemSheetItem.ItemSheetItem.Status.ToString(),
                SoldAt = itemSheetItem.ItemSheetItem.SoldAt,
                DaysForSale = itemSheetItem.ItemSheetItem.SoldAt.HasValue ?
                    (int)(itemSheetItem.ItemSheetItem.SoldAt.Value - itemSheetItem.ItemSheetItem.CreationTime).TotalDays :
                    (int)(DateTime.Now - itemSheetItem.ItemSheetItem.CreationTime).TotalDays,
                CommissionAmount = itemSheetItem.ItemSheetItem.GetCommissionAmount(itemSheetItem.ItemSheetItem.Item.Price),
                CustomerAmount = itemSheetItem.ItemSheetItem.GetCustomerAmount(itemSheetItem.ItemSheetItem.Item.Price),
                CanEdit = itemSheetItem.ItemSheetItem.Status == ItemSheetItemStatus.ForSale,
                CanDelete = itemSheetItem.ItemSheetItem.Status == ItemSheetItemStatus.ForSale,
                CreationTime = itemSheetItem.ItemSheetItem.CreationTime
            };
        }

        public async Task<MyItemDto> CreateAsync(CreateMyItemDto input)
        {
            var userId = CurrentUser.GetId();
            var rental = await _rentalRepository.GetAsync(input.RentalId);

            if (rental.UserId != userId)
            {
                throw new BusinessException("NOT_YOUR_RENTAL");
            }

            if (rental.Status != RentalStatus.Active && rental.Status != RentalStatus.Extended)
            {
                throw new BusinessException("RENTAL_NOT_ACTIVE");
            }

            var organizationalUnitId = _currentOrganizationalUnit.Id ?? throw new BusinessException("ORGANIZATIONAL_UNIT_REQUIRED")
                .WithData("message", "Current organizational unit context is not set");

            // Create Item
            var price = input.EstimatedPrice ?? 0m; // Use EstimatedPrice as initial Price
            var item = new Item(
                GuidGenerator.Create(),
                userId,
                organizationalUnitId,
                input.Name,
                price,
                Currency.PLN,
                CurrentTenant.Id);

            if (!string.IsNullOrWhiteSpace(input.Category))
            {
                item.SetCategory(input.Category);
            }

            await _itemRepository.InsertAsync(item);

            // Find or create ItemSheet for this rental
            var itemSheetQueryable = await _itemSheetRepository.GetQueryableAsync();
            var itemSheet = await AsyncExecuter.FirstOrDefaultAsync(
                itemSheetQueryable.Where(s => s.RentalId == input.RentalId && s.UserId == userId));

            if (itemSheet == null)
            {
                itemSheet = new ItemSheet(GuidGenerator.Create(), userId, organizationalUnitId, CurrentTenant.Id);
                await _itemSheetRepository.InsertAsync(itemSheet);
                itemSheet.AssignToRental(rental);
                await _itemSheetRepository.UpdateAsync(itemSheet);
            }

            // Add item to sheet (creates ItemSheetItem)
            var defaultCommission = 20m; // Default commission percentage
            itemSheet.AddItem(item.Id, defaultCommission);
            await _itemSheetRepository.UpdateAsync(itemSheet);

            // Get the created ItemSheetItem
            var itemSheetItemQueryable = await _itemSheetItemRepository.GetQueryableAsync();
            var itemSheetItem = await AsyncExecuter.FirstOrDefaultAsync(
                itemSheetItemQueryable.Where(isi => isi.ItemId == item.Id && isi.ItemSheetId == itemSheet.Id));

            if (itemSheetItem == null)
            {
                throw new BusinessException("FAILED_TO_CREATE_ITEM_SHEET_ITEM");
            }

            return new MyItemDto
            {
                Id = itemSheetItem.Id,
                RentalId = input.RentalId,
                BoothNumber = rental.Booth?.Number ?? "N/A",
                Name = item.Name,
                Description = null,
                Category = item.Category,
                PhotoUrls = new List<string>(),
                ItemNumber = itemSheetItem.ItemNumber,
                Barcode = itemSheetItem.Barcode,
                EstimatedPrice = price > 0 ? price : null,
                ActualPrice = item.Price,
                CommissionPercentage = itemSheetItem.CommissionPercentage,
                Status = itemSheetItem.Status.ToString(),
                StatusDisplayName = itemSheetItem.Status.ToString(),
                SoldAt = null,
                DaysForSale = 0,
                CommissionAmount = 0,
                CustomerAmount = 0,
                CanEdit = true,
                CanDelete = true,
                CreationTime = itemSheetItem.CreationTime
            };
        }

        public async Task<MyItemDto> UpdateAsync(Guid id, UpdateMyItemDto input)
        {
            var userId = CurrentUser.GetId();
            var queryable = await _itemSheetItemRepository.GetQueryableAsync();
            var itemSheetItem = await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Id == id));

            if (itemSheetItem == null)
            {
                throw new BusinessException("ITEM_NOT_FOUND");
            }

            var itemSheet = await _itemSheetRepository.GetAsync(itemSheetItem.ItemSheetId);

            if (itemSheet.UserId != userId)
            {
                throw new BusinessException("NOT_YOUR_ITEM");
            }

            if (itemSheetItem.Status != ItemSheetItemStatus.ForSale)
            {
                throw new BusinessException("CAN_ONLY_EDIT_FOR_SALE_ITEMS");
            }

            // Update the Item
            var item = await _itemRepository.GetAsync(itemSheetItem.ItemId);
            item.SetName(input.Name);

            if (!string.IsNullOrWhiteSpace(input.Category))
            {
                item.SetCategory(input.Category);
            }

            if (input.EstimatedPrice.HasValue && input.EstimatedPrice.Value > 0)
            {
                item.SetPrice(input.EstimatedPrice.Value);
            }

            await _itemRepository.UpdateAsync(item);

            return new MyItemDto
            {
                Id = itemSheetItem.Id,
                RentalId = itemSheet.RentalId ?? Guid.Empty,
                BoothNumber = itemSheet.Rental?.Booth.Number ?? "N/A",
                Name = item.Name,
                Description = null,
                Category = item.Category,
                PhotoUrls = new List<string>(),
                ItemNumber = itemSheetItem.ItemNumber,
                Barcode = itemSheetItem.Barcode,
                EstimatedPrice = item.Price,
                ActualPrice = item.Price,
                CommissionPercentage = itemSheetItem.CommissionPercentage,
                Status = itemSheetItem.Status.ToString(),
                StatusDisplayName = itemSheetItem.Status.ToString(),
                SoldAt = itemSheetItem.SoldAt,
                DaysForSale = (int)(DateTime.Now - itemSheetItem.CreationTime).TotalDays,
                CommissionAmount = itemSheetItem.GetCommissionAmount(item.Price),
                CustomerAmount = itemSheetItem.GetCustomerAmount(item.Price),
                CanEdit = itemSheetItem.Status == ItemSheetItemStatus.ForSale,
                CanDelete = itemSheetItem.Status == ItemSheetItemStatus.ForSale,
                CreationTime = itemSheetItem.CreationTime
            };
        }

        public async Task DeleteAsync(Guid id)
        {
            var userId = CurrentUser.GetId();
            var queryable = await _itemSheetItemRepository.GetQueryableAsync();
            var itemSheetItem = await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Id == id));

            if (itemSheetItem == null)
            {
                throw new BusinessException("ITEM_NOT_FOUND");
            }

            var itemSheet = await _itemSheetRepository.GetAsync(itemSheetItem.ItemSheetId);

            if (itemSheet.UserId != userId)
            {
                throw new BusinessException("NOT_YOUR_ITEM");
            }

            if (itemSheetItem.Status == ItemSheetItemStatus.Sold)
            {
                throw new BusinessException("CANNOT_DELETE_SOLD_ITEM");
            }

            // Delete ItemSheetItem
            await _itemSheetItemRepository.DeleteAsync(id);

            // Optionally delete the Item if it's not referenced elsewhere
            var item = await _itemRepository.GetAsync(itemSheetItem.ItemId);
            await _itemRepository.DeleteAsync(item.Id);
        }

        public async Task BulkUpdateAsync(BulkUpdateMyItemsDto input)
        {
            var userId = CurrentUser.GetId();

            foreach (var itemSheetItemId in input.ItemIds)
            {
                var queryable = await _itemSheetItemRepository.GetQueryableAsync();
                var itemSheetItem = await AsyncExecuter.FirstOrDefaultAsync(
                    queryable.Where(x => x.Id == itemSheetItemId));

                if (itemSheetItem == null)
                    continue;

                var itemSheet = await _itemSheetRepository.GetAsync(itemSheetItem.ItemSheetId);

                if (itemSheet.UserId != userId)
                {
                    throw new BusinessException("NOT_YOUR_ITEM");
                }

                var item = await _itemRepository.GetAsync(itemSheetItem.ItemId);

                if (input.Category != null)
                {
                    item.SetCategory(input.Category);
                }

                if (input.CommissionPercentage.HasValue)
                {
                    itemSheetItem.SetCommissionPercentage(input.CommissionPercentage.Value);
                    await _itemSheetItemRepository.UpdateAsync(itemSheetItem);
                }

                await _itemRepository.UpdateAsync(item);
            }
        }

        public async Task<List<string>> GetMyItemCategoriesAsync()
        {
            var userId = CurrentUser.GetId();

            var itemSheetItemQueryable = await _itemSheetItemRepository.GetQueryableAsync();
            var itemSheetQueryable = await _itemSheetRepository.GetQueryableAsync();

            var query = from isi in itemSheetItemQueryable
                        join sheet in itemSheetQueryable on isi.ItemSheetId equals sheet.Id
                        where sheet.UserId == userId && isi.Item.Category != null
                        select isi.Item.Category;

            return await AsyncExecuter.ToListAsync(query.Distinct().OrderBy(c => c));
        }

        public async Task<MyItemStatisticsDto> GetMyItemStatisticsAsync(Guid? rentalId = null)
        {
            var userId = CurrentUser.GetId();

            var itemSheetItemQueryable = await _itemSheetItemRepository.GetQueryableAsync();
            var itemSheetQueryable = await _itemSheetRepository.GetQueryableAsync();

            var query = from isi in itemSheetItemQueryable
                        join sheet in itemSheetQueryable on isi.ItemSheetId equals sheet.Id
                        where sheet.UserId == userId
                        select new { ItemSheetItem = isi, ItemSheet = sheet };

            if (rentalId.HasValue)
            {
                query = query.Where(x => x.ItemSheet.RentalId == rentalId.Value);
            }

            var items = await AsyncExecuter.ToListAsync(query);

            var totalSalesValue = items
                .Where(x => x.ItemSheetItem.Status == ItemSheetItemStatus.Sold)
                .Sum(x => x.ItemSheetItem.Item.Price);

            var pricesForAverage = items
                .Select(x => x.ItemSheetItem.Item.Price)
                .Where(p => p > 0)
                .ToList();

            return new MyItemStatisticsDto
            {
                TotalItems = items.Count,
                ForSaleItems = items.Count(x => x.ItemSheetItem.Status == ItemSheetItemStatus.ForSale),
                SoldItems = items.Count(x => x.ItemSheetItem.Status == ItemSheetItemStatus.Sold),
                ReclaimedItems = items.Count(x => x.ItemSheetItem.Status == ItemSheetItemStatus.Reclaimed),
                ExpiredItems = 0, // Not applicable in new model
                TotalEstimatedValue = items.Sum(x => x.ItemSheetItem.Item.Price),
                TotalSalesValue = totalSalesValue,
                AverageItemPrice = pricesForAverage.Any() ? pricesForAverage.Average() : 0,
                ByCategory = new List<CategoryStatDto>(),
                MonthlyTrend = new List<MonthlyItemStatDto>()
            };
        }

        public async Task<byte[]> GenerateItemLabelsAsync(List<Guid> itemIds)
        {
            throw new NotImplementedException("Label generation will be implemented in next phase");
        }
    }
}
