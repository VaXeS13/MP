using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.CustomerDashboard
{
    /// <summary>
    /// Customer item management service
    /// </summary>
    public interface IMyItemAppService : IApplicationService
    {
        /// <summary>
        /// Get customer's items
        /// </summary>
        Task<PagedResultDto<MyItemDto>> GetMyItemsAsync(GetMyItemsDto input);

        /// <summary>
        /// Get single item
        /// </summary>
        Task<MyItemDto> GetMyItemAsync(Guid id);

        /// <summary>
        /// Create new item
        /// </summary>
        Task<MyItemDto> CreateAsync(CreateMyItemDto input);

        /// <summary>
        /// Update existing item
        /// </summary>
        Task<MyItemDto> UpdateAsync(Guid id, UpdateMyItemDto input);

        /// <summary>
        /// Delete item
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// Bulk update items
        /// </summary>
        Task BulkUpdateAsync(BulkUpdateMyItemsDto input);

        /// <summary>
        /// Get item categories used by customer
        /// </summary>
        Task<List<string>> GetMyItemCategoriesAsync();

        /// <summary>
        /// Get item statistics for customer
        /// </summary>
        Task<MyItemStatisticsDto> GetMyItemStatisticsAsync(Guid? rentalId = null);

        /// <summary>
        /// Generate printable labels for items
        /// </summary>
        Task<byte[]> GenerateItemLabelsAsync(List<Guid> itemIds);
    }

    /// <summary>
    /// Item statistics for customer
    /// </summary>
    public class MyItemStatisticsDto
    {
        public int TotalItems { get; set; }
        public int ForSaleItems { get; set; }
        public int SoldItems { get; set; }
        public int ReclaimedItems { get; set; }
        public int ExpiredItems { get; set; }

        public decimal TotalEstimatedValue { get; set; }
        public decimal TotalSalesValue { get; set; }
        public decimal AverageItemPrice { get; set; }

        public List<CategoryStatDto> ByCategory { get; set; } = new();
        public List<MonthlyItemStatDto> MonthlyTrend { get; set; } = new();
    }

    public class CategoryStatDto
    {
        public string Category { get; set; } = null!;
        public int TotalItems { get; set; }
        public int SoldItems { get; set; }
        public decimal SalesValue { get; set; }
    }

    public class MonthlyItemStatDto
    {
        public string Month { get; set; } = null!;
        public int ItemsAdded { get; set; }
        public int ItemsSold { get; set; }
        public decimal SalesValue { get; set; }
    }
}
