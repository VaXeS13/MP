namespace MP.Application.Contracts.SignalR
{
    /// <summary>
    /// DTO for live dashboard updates
    /// </summary>
    public class DashboardUpdateDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalRentals { get; set; }
        public int ActiveRentals { get; set; }
        public int TotalBooths { get; set; }
        public int OccupiedBooths { get; set; }
        public int AvailableBooths { get; set; }
        public decimal OccupancyRate { get; set; }
        public int TotalItems { get; set; }
        public int SoldItems { get; set; }
        public int PendingSettlements { get; set; }
    }
}
