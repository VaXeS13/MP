using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using MP.Domain.Items;
using MP.Domain.Booths;

namespace MP.Items
{
    [Authorize]
    public class ItemAppService : ApplicationService, IItemAppService
    {
        private readonly IItemRepository _itemRepository;
        private readonly ItemManager _itemManager;

        public ItemAppService(
            IItemRepository itemRepository,
            ItemManager itemManager)
        {
            _itemRepository = itemRepository;
            _itemManager = itemManager;
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
            var items = await _itemRepository.GetListByUserIdAsync(userId);

            var pagedItems = items
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList();

            return new PagedResultDto<ItemDto>(
                items.Count,
                ObjectMapper.Map<System.Collections.Generic.List<Item>, System.Collections.Generic.List<ItemDto>>(pagedItems)
            );
        }

        public async Task<ItemDto> CreateAsync(CreateItemDto input)
        {
            var userId = CurrentUser.Id.Value;

            var currency = Enum.Parse<Currency>(input.Currency);

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
            item.SetCurrency(Enum.Parse<Currency>(input.Currency));

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
