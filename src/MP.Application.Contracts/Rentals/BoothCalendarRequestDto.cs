using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Rentals
{
    public class BoothCalendarRequestDto
    {
        [Required]
        public Guid BoothId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Optional: Exclude cart items from this cart ID when checking availability
        /// Used when editing cart items to allow keeping the same dates
        /// </summary>
        public Guid? ExcludeCartId { get; set; }
    }
}