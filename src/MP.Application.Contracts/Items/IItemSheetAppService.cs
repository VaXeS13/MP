using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.Items
{
    public interface IItemSheetAppService : IApplicationService
    {
        Task<ItemSheetDto> GetAsync(Guid id);

        Task<PagedResultDto<ItemSheetDto>> GetListAsync(PagedAndSortedResultRequestDto input);

        Task<PagedResultDto<ItemSheetDto>> GetMyItemSheetsAsync(PagedAndSortedResultRequestDto input);

        Task<ItemSheetDto> CreateAsync(CreateItemSheetDto input);

        Task<ItemSheetDto> AddItemToSheetAsync(Guid sheetId, AddItemToSheetDto input);

        Task<BatchAddItemsResultDto> BatchAddItemsAsync(BatchAddItemsDto input);

        Task<ItemSheetDto> RemoveItemFromSheetAsync(Guid sheetId, Guid itemId);

        Task<ItemSheetDto> AssignToRentalAsync(Guid sheetId, AssignSheetToRentalDto input);

        Task<ItemSheetDto> UnassignFromRentalAsync(Guid sheetId);

        Task<ItemSheetDto> GenerateBarcodesAsync(Guid sheetId);

        Task<ItemSheetDto> FindByBarcodeAsync(string barcode);

        Task DeleteAsync(Guid id);
    }
}
