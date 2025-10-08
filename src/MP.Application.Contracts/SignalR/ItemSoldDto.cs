using System;

namespace MP.Application.Contracts.SignalR
{
    /// <summary>
    /// DTO for live item sold notification
    /// </summary>
    public class ItemSoldDto
    {
        public Guid ItemId { get; set; }
        public string ItemName { get; set; } = null!;
        public decimal SalePrice { get; set; }
        public DateTime SoldAt { get; set; }
        public Guid RentalId { get; set; }
    }
}
