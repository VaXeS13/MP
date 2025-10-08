using System;

namespace MP.Application.Contracts.SignalR
{
    /// <summary>
    /// DTO for live booth status updates on floor plan
    /// </summary>
    public class BoothStatusUpdateDto
    {
        public Guid BoothId { get; set; }
        public string Status { get; set; } = null!;
        public bool IsOccupied { get; set; }
        public Guid? CurrentRentalId { get; set; }
        public DateTime? OccupiedUntil { get; set; }
    }
}
