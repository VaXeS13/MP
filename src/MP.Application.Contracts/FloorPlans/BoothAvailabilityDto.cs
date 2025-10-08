using System;
using System.Collections.Generic;

namespace MP.FloorPlans
{
    public class BoothAvailabilityDto
    {
        public Guid BoothId { get; set; }
        public string BoothNumber { get; set; } = null!;
        public string Status { get; set; } = null!; // available, reserved, rented, maintenance
        public DateTime NextAvailableFrom { get; set; }
        public List<RentalOverlapDto> Overlaps { get; set; } = new();
    }
}
