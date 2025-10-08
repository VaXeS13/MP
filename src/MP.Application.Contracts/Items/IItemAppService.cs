using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.Items
{
    public interface IItemAppService : IApplicationService
    {
        Task<ItemDto> GetAsync(Guid id);

        Task<PagedResultDto<ItemDto>> GetListAsync(PagedAndSortedResultRequestDto input);

        Task<PagedResultDto<ItemDto>> GetMyItemsAsync(PagedAndSortedResultRequestDto input);

        Task<ItemDto> CreateAsync(CreateItemDto input);

        Task<ItemDto> UpdateAsync(Guid id, UpdateItemDto input);

        Task DeleteAsync(Guid id);
    }
}
