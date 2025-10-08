using System;
using System.Collections.Generic;

namespace MP.Rentals
{
    public class BoothCalendarResponseDto
    {
        public Guid BoothId { get; set; }
        public string BoothNumber { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<CalendarDateDto> Dates { get; set; } = new();

        // Legend information for the UI
        public Dictionary<string, string> Legend { get; set; } = new();
    }
}