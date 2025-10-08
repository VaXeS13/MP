using System;
using Volo.Abp.Application.Dtos;

namespace MP.Carts
{
    public class CartItemDto : EntityDto<Guid>
    {
        public Guid CartId { get; set; }
        public Guid BoothId { get; set; }
        public Guid BoothTypeId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal PricePerDay { get; set; }
        public string? Notes { get; set; }

        // Calculated fields
        public int DaysCount { get; set; }
        public decimal TotalPrice { get; set; }

        // Related entity data for display
        public string? BoothNumber { get; set; }
        public string? BoothDescription { get; set; }
        public string? BoothTypeName { get; set; }
        public string? Currency { get; set; }
    }
}