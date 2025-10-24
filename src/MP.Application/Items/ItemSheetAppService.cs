using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using MP.Domain.Items;
using MP.Domain.Rentals;

namespace MP.Items
{
    [Authorize]
    public class ItemSheetAppService : ApplicationService, IItemSheetAppService
    {
        private readonly IItemSheetRepository _itemSheetRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IRentalRepository _rentalRepository;
        private readonly ItemManager _itemManager;

        public ItemSheetAppService(
            IItemSheetRepository itemSheetRepository,
            IItemRepository itemRepository,
            IRentalRepository rentalRepository,
            ItemManager itemManager)
        {
            _itemSheetRepository = itemSheetRepository;
            _itemRepository = itemRepository;
            _rentalRepository = rentalRepository;
            _itemManager = itemManager;
        }

        public async Task<ItemSheetDto> GetAsync(Guid id)
        {
            var sheet = await _itemSheetRepository.GetWithItemsAsync(id);
            if (sheet == null)
                throw new Volo.Abp.BusinessException("ITEM_SHEET_NOT_FOUND");

            return MapToDto(sheet);
        }

        public async Task<PagedResultDto<ItemSheetDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            var queryable = await _itemSheetRepository.GetQueryableAsync();
            var query = queryable
                .OrderByDescending(x => x.CreationTime)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount);

            var sheets = await AsyncExecuter.ToListAsync(query);
            var totalCount = await _itemSheetRepository.GetCountAsync();

            return new PagedResultDto<ItemSheetDto>(
                totalCount,
                sheets.Select(MapToDto).ToList()
            );
        }

        public async Task<PagedResultDto<ItemSheetDto>> GetMyItemSheetsAsync(PagedAndSortedResultRequestDto input)
        {
            var userId = CurrentUser.Id.Value;
            var sheets = await _itemSheetRepository.GetListByUserIdAsync(userId);

            var pagedSheets = sheets
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList();

            return new PagedResultDto<ItemSheetDto>(
                sheets.Count,
                pagedSheets.Select(MapToDto).ToList()
            );
        }

        public async Task<ItemSheetDto> CreateAsync(CreateItemSheetDto input)
        {
            var userId = CurrentUser.Id.Value;

            var sheet = await _itemManager.CreateSheetAsync(
                userId,
                Guid.Empty, // TODO: Get organizationalUnitId from user context or input
                CurrentTenant.Id
            );

            return MapToDto(sheet);
        }

        public async Task<ItemSheetDto> AddItemToSheetAsync(Guid sheetId, AddItemToSheetDto input)
        {
            var sheet = await _itemSheetRepository.GetWithItemsAsync(sheetId);
            if (sheet == null)
                throw new Volo.Abp.BusinessException("ITEM_SHEET_NOT_FOUND");

            var item = await _itemRepository.GetAsync(input.ItemId);

            if (sheet.UserId != CurrentUser.Id.Value)
                throw new Volo.Abp.Authorization.AbpAuthorizationException("You can only modify your own sheets");

            await _itemManager.AddItemToSheetAsync(sheet, item, input.CommissionPercentage);

            return MapToDto(sheet);
        }

        public async Task<BatchAddItemsResultDto> BatchAddItemsAsync(BatchAddItemsDto input)
        {
            var result = new BatchAddItemsResultDto();

            var sheet = await _itemSheetRepository.GetWithItemsAsync(input.SheetId);
            if (sheet == null)
                throw new Volo.Abp.BusinessException("ITEM_SHEET_NOT_FOUND");

            if (sheet.UserId != CurrentUser.Id.Value)
                throw new Volo.Abp.Authorization.AbpAuthorizationException("You can only modify your own sheets");

            var items = await _itemRepository.GetListByIdsAsync(input.ItemIds);

            foreach (var itemId in input.ItemIds)
            {
                try
                {
                    var item = items.FirstOrDefault(i => i.Id == itemId);
                    if (item == null)
                    {
                        result.Results.Add(new BatchItemResultDto
                        {
                            ItemId = itemId,
                            Success = false,
                            ErrorMessage = "Item not found"
                        });
                        continue;
                    }

                    await _itemManager.AddItemToSheetAsync(sheet, item, input.CommissionPercentage);

                    result.Results.Add(new BatchItemResultDto
                    {
                        ItemId = itemId,
                        Success = true
                    });
                }
                catch (Exception ex)
                {
                    result.Results.Add(new BatchItemResultDto
                    {
                        ItemId = itemId,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            return result;
        }

        public async Task<ItemSheetDto> RemoveItemFromSheetAsync(Guid sheetId, Guid itemId)
        {
            var sheet = await _itemSheetRepository.GetWithItemsAsync(sheetId);
            if (sheet == null)
                throw new Volo.Abp.BusinessException("ITEM_SHEET_NOT_FOUND");

            var item = await _itemRepository.GetAsync(itemId);

            if (sheet.UserId != CurrentUser.Id.Value)
                throw new Volo.Abp.Authorization.AbpAuthorizationException("You can only modify your own sheets");

            await _itemManager.RemoveItemFromSheetAsync(sheet, item);

            return MapToDto(sheet);
        }

        public async Task<ItemSheetDto> AssignToRentalAsync(Guid sheetId, AssignSheetToRentalDto input)
        {
            var sheet = await _itemSheetRepository.GetWithItemsAsync(sheetId);
            if (sheet == null)
                throw new Volo.Abp.BusinessException("ITEM_SHEET_NOT_FOUND");

            var rental = await _rentalRepository.GetAsync(input.RentalId);

            if (sheet.UserId != CurrentUser.Id.Value)
                throw new Volo.Abp.Authorization.AbpAuthorizationException("You can only modify your own sheets");

            sheet.AssignToRental(rental);
            await _itemSheetRepository.UpdateAsync(sheet);

            return MapToDto(sheet);
        }

        public async Task<ItemSheetDto> UnassignFromRentalAsync(Guid sheetId)
        {
            var sheet = await _itemSheetRepository.GetAsync(sheetId);

            if (sheet.UserId != CurrentUser.Id.Value)
                throw new Volo.Abp.Authorization.AbpAuthorizationException("You can only modify your own sheets");

            sheet.UnassignFromRental();
            await _itemSheetRepository.UpdateAsync(sheet);

            return MapToDto(sheet);
        }

        public async Task<ItemSheetDto> GenerateBarcodesAsync(Guid sheetId)
        {
            var sheet = await _itemSheetRepository.GetWithItemsAsync(sheetId);
            if (sheet == null)
                throw new Volo.Abp.BusinessException("ITEM_SHEET_NOT_FOUND");

            if (sheet.UserId != CurrentUser.Id.Value)
                throw new Volo.Abp.Authorization.AbpAuthorizationException("You can only modify your own sheets");

            await _itemManager.GenerateBarcodesAsync(sheet);

            return MapToDto(sheet);
        }

        public async Task<ItemSheetDto> FindByBarcodeAsync(string barcode)
        {
            var sheet = await _itemSheetRepository.FindByBarcodeAsync(barcode);
            if (sheet == null)
                throw new Volo.Abp.BusinessException("ITEM_SHEET_NOT_FOUND_BY_BARCODE");

            return MapToDto(sheet);
        }

        public async Task DeleteAsync(Guid id)
        {
            var sheet = await _itemSheetRepository.GetAsync(id);

            if (sheet.UserId != CurrentUser.Id.Value)
                throw new Volo.Abp.Authorization.AbpAuthorizationException("You can only delete your own sheets");

            if (sheet.Status != ItemSheetStatus.Draft)
                throw new Volo.Abp.BusinessException("ONLY_DRAFT_SHEETS_CAN_BE_DELETED");

            await _itemSheetRepository.DeleteAsync(id);
        }

        private ItemSheetDto MapToDto(ItemSheet sheet)
        {
            var dto = ObjectMapper.Map<ItemSheet, ItemSheetDto>(sheet);
            dto.TotalItemsCount = sheet.GetItemsCount();
            dto.SoldItemsCount = sheet.GetSoldItemsCount();
            dto.ReclaimedItemsCount = sheet.GetReclaimedItemsCount();
            return dto;
        }
    }
}
