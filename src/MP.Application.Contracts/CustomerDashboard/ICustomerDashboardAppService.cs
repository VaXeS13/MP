using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.CustomerDashboard
{
    /// <summary>
    /// Customer dashboard application service
    /// </summary>
    public interface ICustomerDashboardAppService : IApplicationService
    {
        /// <summary>
        /// Get main customer dashboard overview
        /// </summary>
        Task<CustomerDashboardDto> GetDashboardAsync();

        /// <summary>
        /// Get customer sales statistics
        /// </summary>
        Task<CustomerSalesStatisticsDto> GetSalesStatisticsAsync(CustomerStatisticsFilterDto filter);

        /// <summary>
        /// Get customer active rentals
        /// </summary>
        Task<List<MyActiveRentalDto>> GetMyActiveRentalsAsync();

        /// <summary>
        /// Get customer settlements
        /// </summary>
        Task<PagedResultDto<SettlementItemDto>> GetMySettlementsAsync(PagedAndSortedResultRequestDto input);

        /// <summary>
        /// Get customer settlement summary
        /// </summary>
        Task<SettlementSummaryDto> GetSettlementSummaryAsync();

        /// <summary>
        /// Request settlement/withdrawal
        /// </summary>
        Task<SettlementItemDto> RequestSettlementAsync(RequestSettlementDto input);

        /// <summary>
        /// Get customer notifications
        /// </summary>
        Task<PagedResultDto<CustomerNotificationDto>> GetMyNotificationsAsync(PagedAndSortedResultRequestDto input);

        /// <summary>
        /// Mark notification as read
        /// </summary>
        Task MarkNotificationAsReadAsync(Guid notificationId);

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        Task MarkAllNotificationsAsReadAsync();

        /// <summary>
        /// Get QR code for booth access
        /// </summary>
        Task<QRCodeDto> GetBoothQRCodeAsync(Guid rentalId);
    }

    /// <summary>
    /// Statistics filter
    /// </summary>
    public class CustomerStatisticsFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? RentalId { get; set; }
    }

    /// <summary>
    /// Request settlement DTO
    /// </summary>
    public class RequestSettlementDto
    {
        public List<Guid> ItemIds { get; set; } = new();
        public string? Notes { get; set; }
        public string? BankAccountNumber { get; set; }
    }

    /// <summary>
    /// QR Code DTO
    /// </summary>
    public class QRCodeDto
    {
        public Guid RentalId { get; set; }
        public string BoothNumber { get; set; } = null!;
        public string QRCodeBase64 { get; set; } = null!;
        public string AccessCode { get; set; } = null!;
        public DateTime GeneratedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
