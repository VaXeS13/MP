using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace MP.Domain.Items
{
    public class ItemManager : DomainService
    {
        private readonly IItemRepository _itemRepository;
        private readonly IItemSheetRepository _itemSheetRepository;

        public ItemManager(
            IItemRepository itemRepository,
            IItemSheetRepository itemSheetRepository)
        {
            _itemRepository = itemRepository;
            _itemSheetRepository = itemSheetRepository;
        }

        public async Task<Item> CreateAsync(
            Guid userId,
            string name,
            decimal price,
            Booths.Currency currency,
            string? category = null,
            Guid? tenantId = null)
        {
            var item = new Item(
                GuidGenerator.Create(),
                userId,
                name,
                price,
                currency,
                tenantId
            );

            if (!string.IsNullOrWhiteSpace(category))
            {
                item.SetCategory(category);
            }

            return await _itemRepository.InsertAsync(item);
        }

        public async Task<ItemSheet> CreateSheetAsync(
            Guid userId,
            Guid? tenantId = null)
        {
            var sheet = new ItemSheet(
                GuidGenerator.Create(),
                userId,
                tenantId
            );

            return await _itemSheetRepository.InsertAsync(sheet);
        }

        public async Task AddItemToSheetAsync(
            ItemSheet sheet,
            Item item,
            decimal commissionPercentage = 0)
        {
            if (item.UserId != sheet.UserId)
                throw new BusinessException("ITEM_AND_SHEET_MUST_BELONG_TO_SAME_USER");

            if (item.Status != ItemStatus.Draft)
                throw new BusinessException("ONLY_DRAFT_ITEMS_CAN_BE_ADDED_TO_SHEET");

            sheet.AddItem(item.Id, commissionPercentage);
            item.MarkAsInSheet();

            await _itemSheetRepository.UpdateAsync(sheet);
            await _itemRepository.UpdateAsync(item);
        }

        public async Task RemoveItemFromSheetAsync(
            ItemSheet sheet,
            Item item)
        {
            sheet.RemoveItem(item.Id);
            item.MarkAsDraft();

            await _itemSheetRepository.UpdateAsync(sheet);
            await _itemRepository.UpdateAsync(item);
        }

        public async Task GenerateBarcodesAsync(ItemSheet sheet)
        {
            sheet.GenerateBarcodes();

            var items = await _itemRepository.GetListByIdsAsync(
                sheet.Items.Select(x => x.ItemId).ToList());

            foreach (var item in items)
            {
                item.MarkAsForSale();
            }

            await _itemSheetRepository.UpdateAsync(sheet);
            await _itemRepository.UpdateManyAsync(items);
        }

        public async Task<ItemSheet?> FindSheetByBarcodeAsync(string barcode)
        {
            if (!BarcodeHelper.IsValidBarcode(barcode))
                return null;

            return await _itemSheetRepository.FindByBarcodeAsync(barcode);
        }
    }
}
