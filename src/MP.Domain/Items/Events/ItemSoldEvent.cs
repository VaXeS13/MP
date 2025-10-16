using System;

namespace MP.Domain.Items.Events
{
    /// <summary>
    /// Event published when an item is sold at checkout
    /// </summary>
    public class ItemSoldEvent
    {
        public Guid UserId { get; set; }
        public Guid ItemId { get; set; }
        public string ItemName { get; set; } = null!;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "PLN";
        public DateTime SoldAt { get; set; } = DateTime.UtcNow;
        public Guid? RentalId { get; set; }
    }
}
