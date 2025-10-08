using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace MP.Application.Contracts.CustomerDashboard
{
    /// <summary>
    /// Customer's item DTO with enhanced information
    /// </summary>
    public class MyItemDto : FullAuditedEntityDto<Guid>
    {
        public Guid RentalId { get; set; }
        public string BoothNumber { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public List<string> PhotoUrls { get; set; } = new();
        public int ItemNumber { get; set; }
        public string? Barcode { get; set; }

        public decimal? EstimatedPrice { get; set; }
        public decimal? ActualPrice { get; set; }
        public decimal CommissionPercentage { get; set; }

        public string Status { get; set; } = null!;
        public string StatusDisplayName { get; set; } = null!;

        public DateTime? SoldAt { get; set; }
        public int DaysForSale { get; set; }

        public decimal? CommissionAmount { get; set; }
        public decimal? CustomerAmount { get; set; }

        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    /// <summary>
    /// Create/Update item DTO for customer
    /// </summary>
    public class CreateMyItemDto
    {
        [Required]
        public Guid RentalId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public decimal? EstimatedPrice { get; set; }

        public List<string> PhotoUrls { get; set; } = new();
    }

    /// <summary>
    /// Update item DTO for customer
    /// </summary>
    public class UpdateMyItemDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public decimal? EstimatedPrice { get; set; }

        public List<string> PhotoUrls { get; set; } = new();
    }

    /// <summary>
    /// Get customer items filter
    /// </summary>
    public class GetMyItemsDto : PagedAndSortedResultRequestDto
    {
        public Guid? RentalId { get; set; }
        public string? Status { get; set; }
        public string? Category { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
    }

    /// <summary>
    /// Bulk operations for customer items
    /// </summary>
    public class BulkUpdateMyItemsDto
    {
        [Required]
        public List<Guid> ItemIds { get; set; } = new();

        public string? Category { get; set; }
        public decimal? CommissionPercentage { get; set; }
    }

    /// <summary>
    /// Item upload result
    /// </summary>
    public class ItemPhotoUploadResultDto
    {
        public string Url { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
