using System;

namespace MP.Rentals
{
    public class MaxExtensionDateResponseDto
    {
        /// <summary>
        /// Maximum date to which the rental can be extended
        /// </summary>
        public DateTime? MaxExtensionDate { get; set; }

        /// <summary>
        /// True if there is a rental blocking further extension
        /// </summary>
        public bool HasBlockingRental { get; set; }

        /// <summary>
        /// ID of the next rental (if exists)
        /// </summary>
        public Guid? NextRentalId { get; set; }

        /// <summary>
        /// Start date of the next rental (if exists)
        /// </summary>
        public DateTime? NextRentalStartDate { get; set; }

        /// <summary>
        /// Optional message explaining the limitation
        /// </summary>
        public string? Message { get; set; }
    }
}
