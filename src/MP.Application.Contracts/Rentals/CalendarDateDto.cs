using System;

namespace MP.Rentals
{
    public class CalendarDateDto
    {
        /// <summary>
        /// Date in YYYY-MM-DD format
        /// </summary>
        public string Date { get; set; } = null!;
        public CalendarDateStatus Status { get; set; }
        public string StatusDisplayName { get; set; } = null!;
        public Guid? RentalId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public DateTime? RentalStartDate { get; set; }
        public DateTime? RentalEndDate { get; set; }
        public string? Notes { get; set; }
    }
}