using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MP.Domain.HomePageContent
{
    public interface IHomePageSectionRepository : IRepository<HomePageSection, Guid>
    {
        /// <summary>
        /// Get all active sections ordered by Order property
        /// </summary>
        Task<List<HomePageSection>> GetActiveOrderedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all sections ordered by Order property
        /// </summary>
        Task<List<HomePageSection>> GetAllOrderedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the maximum order value
        /// </summary>
        Task<int> GetMaxOrderAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Update order for multiple sections (for drag-drop reordering)
        /// </summary>
        Task UpdateOrdersAsync(Dictionary<Guid, int> idOrderMap, CancellationToken cancellationToken = default);
    }
}
