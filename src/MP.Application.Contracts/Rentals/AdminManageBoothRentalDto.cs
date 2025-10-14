using System;
using System.ComponentModel.DataAnnotations;
using MP.Domain.Rentals;

namespace MP.Rentals
{
    public class AdminManageBoothRentalDto
    {
        [Required]
        public Guid BoothId { get; set; }

        /// <summary>
        /// User ID for new rental. Null if this is an extension.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Booth Type ID for new rental. Null if this is an extension.
        /// </summary>
        public Guid? BoothTypeId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public RentalPaymentMethod PaymentMethod { get; set; }

        /// <summary>
        /// Terminal transaction ID (required for Terminal payment method)
        /// </summary>
        public string? TerminalTransactionId { get; set; }

        /// <summary>
        /// Terminal receipt number (optional for Terminal payment method)
        /// </summary>
        public string? TerminalReceiptNumber { get; set; }

        /// <summary>
        /// Optional notes about this rental
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// True if this is an extension of existing rental, false if new rental
        /// </summary>
        [Required]
        public bool IsExtension { get; set; }

        /// <summary>
        /// Existing rental ID (required if IsExtension = true)
        /// </summary>
        public Guid? ExistingRentalId { get; set; }

        /// <summary>
        /// Timeout in minutes for online payment (optional, defaults to 30)
        /// </summary>
        public int? OnlineTimeoutMinutes { get; set; }
    }
}
