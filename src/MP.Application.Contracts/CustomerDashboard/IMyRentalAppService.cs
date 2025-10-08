using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.CustomerDashboard
{
    /// <summary>
    /// Customer rental management service
    /// </summary>
    public interface IMyRentalAppService : IApplicationService
    {
        /// <summary>
        /// Get customer's rentals
        /// </summary>
        Task<PagedResultDto<MyActiveRentalDto>> GetMyRentalsAsync(GetMyRentalsDto input);

        /// <summary>
        /// Get detailed rental information
        /// </summary>
        Task<MyRentalDetailDto> GetMyRentalDetailAsync(Guid id);

        /// <summary>
        /// Get rental calendar events
        /// </summary>
        Task<MyRentalCalendarDto> GetMyRentalCalendarAsync();

        /// <summary>
        /// Request rental extension
        /// </summary>
        Task<RentalExtensionResultDto> RequestExtensionAsync(RequestRentalExtensionDto input);

        /// <summary>
        /// Calculate extension cost
        /// </summary>
        Task<ExtensionCostCalculationDto> CalculateExtensionCostAsync(Guid rentalId, int days);

        /// <summary>
        /// Get rental activity log
        /// </summary>
        Task<List<RentalActivityDto>> GetRentalActivityAsync(Guid rentalId);

        /// <summary>
        /// Cancel rental (if allowed)
        /// </summary>
        Task CancelMyRentalAsync(Guid id, string reason);
    }

    /// <summary>
    /// Extension cost calculation
    /// </summary>
    public class ExtensionCostCalculationDto
    {
        public Guid RentalId { get; set; }
        public int ExtensionDays { get; set; }
        public DateTime CurrentEndDate { get; set; }
        public DateTime NewEndDate { get; set; }
        public decimal PricePerDay { get; set; }
        public decimal TotalCost { get; set; }
        public bool IsAvailable { get; set; }
        public string? UnavailableReason { get; set; }
    }
}
