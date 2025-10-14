using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Settings;
using MP.Domain.Items;
using MP.Domain.Booths;
using MP.Domain.Settings;

namespace MP.Items
{
    [Authorize]
    public class ItemAppService : ApplicationService, IItemAppService
    {
        private readonly IItemRepository _itemRepository;
        private readonly ItemManager _itemManager;
        private readonly ISettingProvider _settingProvider;

        public ItemAppService(
            IItemRepository itemRepository,
            ItemManager itemManager,
            ISettingProvider settingProvider)
        {
            _itemRepository = itemRepository;
            _itemManager = itemManager;
            _settingProvider = settingProvider;
        }

        public async Task<ItemDto> GetAsync(Guid id)
        {
            var item = await _itemRepository.GetAsync(id);
            return ObjectMapper.Map<Item, ItemDto>(item);
        }

        public async Task<PagedResultDto<ItemDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            var queryable = await _itemRepository.GetQueryableAsync();
            var query = queryable
                .OrderByDescending(x => x.CreationTime)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount);

            var items = await AsyncExecuter.ToListAsync(query);
            var totalCount = await _itemRepository.GetCountAsync();

            return new PagedResultDto<ItemDto>(
                totalCount,
                ObjectMapper.Map<System.Collections.Generic.List<Item>, System.Collections.Generic.List<ItemDto>>(items)
            );
        }

        public async Task<PagedResultDto<ItemDto>> GetMyItemsAsync(PagedAndSortedResultRequestDto input)
        {
            var userId = CurrentUser.Id.Value;

            var items = await _itemRepository.GetListByUserIdAsync(
                userId,
                input.SkipCount,
                input.MaxResultCount);

            var totalCount = await _itemRepository.GetCountByUserIdAsync(userId);

            return new PagedResultDto<ItemDto>(
                totalCount,
                ObjectMapper.Map<System.Collections.Generic.List<Item>, System.Collections.Generic.List<ItemDto>>(items)
            );
        }

        public async Task<ItemDto> CreateAsync(CreateItemDto input)
        {
            var userId = CurrentUser.Id.Value;

            // Get tenant currency from settings (default to PLN if not set)
            var currencySettingValue = await _settingProvider.GetOrNullAsync(MPSettings.Tenant.Currency);
            var currency = string.IsNullOrEmpty(currencySettingValue)
                ? Currency.PLN
                : Enum.Parse<Currency>(currencySettingValue);

            var item = await _itemManager.CreateAsync(
                userId,
                input.Name,
                input.Price,
                currency,
                input.Category,
                CurrentTenant.Id
            );

            return ObjectMapper.Map<Item, ItemDto>(item);
        }

        public async Task<ItemDto> UpdateAsync(Guid id, UpdateItemDto input)
        {
            var item = await _itemRepository.GetAsync(id);

            if (item.UserId != CurrentUser.Id.Value)
                throw new Volo.Abp.Authorization.AbpAuthorizationException("You can only update your own items");

            item.SetName(input.Name);
            item.SetCategory(input.Category);
            item.SetPrice(input.Price);
            // Note: Currency is NOT updated - items keep their original currency (historical data)

            await _itemRepository.UpdateAsync(item);

            return ObjectMapper.Map<Item, ItemDto>(item);
        }

        public async Task DeleteAsync(Guid id)
        {
            var item = await _itemRepository.GetAsync(id);

            if (item.UserId != CurrentUser.Id.Value)
                throw new Volo.Abp.Authorization.AbpAuthorizationException("You can only delete your own items");

            if (item.Status != ItemStatus.Draft)
                throw new Volo.Abp.BusinessException("ONLY_DRAFT_ITEMS_CAN_BE_DELETED");

            await _itemRepository.DeleteAsync(id);
        }
    }
}
