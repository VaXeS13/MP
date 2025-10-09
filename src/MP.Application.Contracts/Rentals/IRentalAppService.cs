using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MP.Rentals
{
    public interface IRentalAppService : IApplicationService
    {
        // CRUD Operations
        Task<RentalDto> GetAsync(Guid id);
        Task<PagedResultDto<RentalListDto>> GetListAsync(GetRentalListDto input);
        Task<RentalDto> CreateAsync(CreateRentalDto input);
        Task<RentalDto> UpdateAsync(Guid id, UpdateRentalDto input);
        Task DeleteAsync(Guid id);

        // Business Operations
        Task<RentalDto> PayAsync(Guid id, PaymentDto input);
        Task<RentalDto> StartRentalAsync(Guid id);
        Task<RentalDto> CompleteRentalAsync(Guid id);
        Task<RentalDto> CancelRentalAsync(Guid id, string reason);
        Task<RentalDto> ExtendRentalAsync(Guid id, ExtendRentalDto input);
        Task<RentalDto?> GetActiveRentalForBoothAsync(Guid boothId);

        // User specific
        Task<PagedResultDto<RentalListDto>> GetMyRentalsAsync(GetRentalListDto input);
        Task<RentalDto> CreateMyRentalAsync(CreateMyRentalDto input);
        Task<CreateRentalWithPaymentResultDto> CreateMyRentalWithPaymentAsync(CreateRentalWithPaymentDto input);

        // Reports
        Task<List<RentalListDto>> GetActiveRentalsAsync();
        Task<List<RentalListDto>> GetExpiredRentalsAsync();
        Task<List<RentalListDto>> GetOverdueRentalsAsync();

        // Calendar
        Task<BoothCalendarResponseDto> GetBoothCalendarAsync(BoothCalendarRequestDto input);

        // Availability and cost calculation
        Task<bool> CheckAvailabilityAsync(Guid boothId, DateTime startDate, DateTime endDate);
        Task<decimal> CalculateCostAsync(Guid boothId, Guid boothTypeId, DateTime startDate, DateTime endDate);
    }
}