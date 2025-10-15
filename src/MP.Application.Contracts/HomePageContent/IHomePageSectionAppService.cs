using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.HomePageContent
{
    public interface IHomePageSectionAppService : ICrudAppService<
        HomePageSectionDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateHomePageSectionDto,
        UpdateHomePageSectionDto>
    {
        /// <summary>
        /// Get all sections ordered by Order property (for admin panel)
        /// </summary>
        Task<List<HomePageSectionDto>> GetAllOrderedAsync();

        /// <summary>
        /// Get only active sections valid for public display
        /// </summary>
        Task<List<HomePageSectionDto>> GetActiveForDisplayAsync();

        /// <summary>
        /// Activate a section (publish)
        /// </summary>
        Task<HomePageSectionDto> ActivateAsync(Guid id);

        /// <summary>
        /// Deactivate a section (unpublish)
        /// </summary>
        Task<HomePageSectionDto> DeactivateAsync(Guid id);

        /// <summary>
        /// Reorder sections (for drag-drop functionality)
        /// </summary>
        Task ReorderAsync(List<ReorderSectionDto> reorderList);
    }
}
