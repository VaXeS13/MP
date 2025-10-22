using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using MP.Application.Contracts.CustomerDashboard;
using MP.Application.Contracts.Rentals;
using MP.Domain.Rentals;
using MP.Permissions;
using MP.Rentals;

namespace MP.Application.CustomerDashboard
{
    [Authorize(MPPermissions.CustomerDashboard.ManageMyRentals)]
    public class MyRentalAppService : ApplicationService, IMyRentalAppService
    {
        private readonly IRentalRepository _rentalRepository;

        public MyRentalAppService(IRentalRepository rentalRepository)
        {
            _rentalRepository = rentalRepository;
        }

        public async Task<PagedResultDto<MyActiveRentalDto>> GetMyRentalsAsync(GetMyRentalsDto input)
        {
            try
            {
                var userId = CurrentUser.GetId();
                var rentals = await _rentalRepository.GetRentalsForUserAsync(userId);

                var query = rentals.AsQueryable();

                if (input.Status.HasValue)
                {
                    query = query.Where(r => r.Status == input.Status.Value);
                }

                if (!input.IncludeCompleted.GetValueOrDefault(true))
                {
                    query = query.Where(r => r.Status != RentalStatus.Expired && r.Status != RentalStatus.Cancelled);
                }

                var totalCount = query.Count();
                var items = query.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

                var dtos = new List<MyActiveRentalDto>();

                foreach (var r in items)
                {
                    try
                    {
                        // Defensive: check for null period (should not happen but be safe)
                        var period = r.Period;
                        var startDate = period?.StartDate ?? DateTime.Today;
                        var endDate = period?.EndDate ?? DateTime.Today;
                        var daysRemaining = (int)(endDate - DateTime.Today).TotalDays;
                        var isExpiringSoon = (endDate - DateTime.Today).TotalDays <= 7;
                        var totalItems = r.GetItemsCount();
                        var soldItems = r.GetSoldItemsCount();

                        dtos.Add(new MyActiveRentalDto
                        {
                            RentalId = r.Id,
                            BoothNumber = r.Booth?.Number ?? "N/A",
                            BoothTypeName = r.BoothType?.Name ?? "N/A",
                            StartDate = startDate,
                            EndDate = endDate,
                            DaysRemaining = daysRemaining,
                            IsExpiringSoon = isExpiringSoon,
                            Status = r.Status.ToString(),
                            TotalItems = totalItems,
                            SoldItems = soldItems,
                            AvailableItems = totalItems - soldItems,
                            TotalSales = r.GetTotalSalesAmount(),
                            TotalCommission = r.GetTotalCommissionEarned(),
                            CanExtend = r.IsActive(),
                            TotalCost = r.Payment.TotalAmount,
                            DiscountAmount = r.DiscountAmount,
                            OriginalAmount = r.GetOriginalAmount(),
                            PromoCodeUsed = r.PromoCodeUsed
                        });
                    }
                    catch
                    {
                        // If mapping fails, skip this rental
                    }
                }

                return new PagedResultDto<MyActiveRentalDto>(totalCount, dtos);
            }
            catch
            {
                // Return empty result if anything fails
                return new PagedResultDto<MyActiveRentalDto>(0, new List<MyActiveRentalDto>());
            }
        }

        public async Task<MyRentalDetailDto> GetMyRentalDetailAsync(Guid id)
        {
            var userId = CurrentUser.GetId();
            var rental = await _rentalRepository.GetRentalWithItemsAsync(id);

            if (rental == null || rental.UserId != userId)
            {
                throw new BusinessException("RENTAL_NOT_FOUND");
            }

            // Defensive: get safe values for nullable navigation properties
            var period = rental.Period;
            var startDate = period?.StartDate ?? DateTime.Today;
            var endDate = period?.EndDate ?? DateTime.Today;
            var boothNumber = rental.Booth?.Number ?? "N/A";
            var boothTypeName = rental.BoothType?.Name ?? "N/A";
            var boothPricePerDay = rental.Booth?.PricePerDay ?? 0m;
            var payment = rental.Payment;

            return new MyRentalDetailDto
            {
                Id = rental.Id,
                BoothId = rental.BoothId,
                BoothNumber = boothNumber,
                BoothTypeName = boothTypeName,
                BoothPricePerDay = boothPricePerDay,
                StartDate = startDate,
                EndDate = endDate,
                TotalDays = period?.GetDaysCount() ?? 0,
                DaysRemaining = (int)(endDate - DateTime.Today).TotalDays,
                DaysElapsed = (int)(DateTime.Today - startDate).TotalDays,
                Status = rental.Status,
                StatusDisplayName = rental.Status.ToString(),
                TotalCost = payment?.TotalAmount ?? 0m,
                PaidAmount = payment?.PaidAmount ?? 0m,
                IsPaid = payment?.IsPaid ?? false,
                PaidDate = payment?.PaidDate,
                PriceBreakdown = DeserializePriceBreakdown(rental.PriceBreakdown),
                AppliedPromotionId = rental.AppliedPromotionId,
                DiscountAmount = rental.DiscountAmount,
                OriginalAmount = rental.GetOriginalAmount(),
                PromoCodeUsed = rental.PromoCodeUsed,
                Notes = rental.Notes,
                StartedAt = rental.StartedAt,
                CompletedAt = rental.CompletedAt,
                TotalItems = rental.GetItemsCount(),
                SoldItems = rental.GetSoldItemsCount(),
                AvailableItems = rental.GetItemsCount() - rental.GetSoldItemsCount(),
                ReclaimedItems = 0,
                TotalSalesAmount = rental.GetTotalSalesAmount(),
                TotalCommissionPaid = rental.GetTotalCommissionEarned(),
                NetEarnings = rental.GetTotalSalesAmount() - rental.GetTotalCommissionEarned(),
                CanExtend = rental.IsActive(),
                CanCancel = rental.Status == RentalStatus.Draft || rental.Status == RentalStatus.Active,
                IsExpiringSoon = (endDate - DateTime.Today).TotalDays <= 7,
                IsOverdue = rental.IsOverdue(),
                ExtensionOptions = new List<ExtensionOptionDto>
                {
                    new ExtensionOptionDto { Days = 7, DisplayName = "1 week", Cost = boothPricePerDay * 7, NewEndDate = endDate.AddDays(7) },
                    new ExtensionOptionDto { Days = 14, DisplayName = "2 weeks", Cost = boothPricePerDay * 14, NewEndDate = endDate.AddDays(14) },
                    new ExtensionOptionDto { Days = 30, DisplayName = "1 month", Cost = boothPricePerDay * 30, NewEndDate = endDate.AddDays(30) }
                },
                RecentActivity = new List<RentalActivityDto>(),
                CreationTime = rental.CreationTime
            };
        }

        public async Task<MyRentalCalendarDto> GetMyRentalCalendarAsync()
        {
            var userId = CurrentUser.GetId();
            var rentals = await _rentalRepository.GetRentalsForUserAsync(userId);

            var events = rentals.Select(r =>
            {
                var period = r.Period;
                var endDate = period?.EndDate ?? DateTime.Today;
                return new RentalCalendarEventDto
                {
                    RentalId = r.Id,
                    BoothNumber = r.Booth?.Number ?? "N/A",
                    StartDate = period?.StartDate ?? DateTime.Today,
                    EndDate = endDate,
                    Status = r.Status.ToString(),
                    Color = GetColorByStatus(r.Status),
                    IsExpiringSoon = (endDate - DateTime.Today).TotalDays <= 7
                };
            }).ToList();

            return new MyRentalCalendarDto
            {
                Events = events,
                ImportantDates = events.Select(e => e.EndDate).Distinct().ToList()
            };
        }

        public async Task<RentalExtensionResultDto> RequestExtensionAsync(RequestRentalExtensionDto input)
        {
            throw new NotImplementedException("Rental extension with payment will be implemented in next phase");
        }

        public async Task<ExtensionCostCalculationDto> CalculateExtensionCostAsync(Guid rentalId, int days)
        {
            var userId = CurrentUser.GetId();
            var rental = await _rentalRepository.GetAsync(rentalId);

            if (rental.UserId != userId)
            {
                throw new BusinessException("NOT_YOUR_RENTAL");
            }

            var period = rental.Period;
            var endDate = period?.EndDate ?? DateTime.Today;
            var pricePerDay = rental.Booth?.PricePerDay ?? 0m;

            return new ExtensionCostCalculationDto
            {
                RentalId = rentalId,
                ExtensionDays = days,
                CurrentEndDate = endDate,
                NewEndDate = endDate.AddDays(days),
                PricePerDay = pricePerDay,
                TotalCost = pricePerDay * days,
                IsAvailable = rental.IsActive()
            };
        }

        public async Task<List<RentalActivityDto>> GetRentalActivityAsync(Guid rentalId)
        {
            var userId = CurrentUser.GetId();
            var rental = await _rentalRepository.GetAsync(rentalId);

            if (rental.UserId != userId)
            {
                throw new BusinessException("NOT_YOUR_RENTAL");
            }

            return new List<RentalActivityDto>();
        }

        public async Task CancelMyRentalAsync(Guid id, string reason)
        {
            var userId = CurrentUser.GetId();
            var rental = await _rentalRepository.GetAsync(id);

            if (rental.UserId != userId)
            {
                throw new BusinessException("NOT_YOUR_RENTAL");
            }

            rental.Cancel(reason);
            await _rentalRepository.UpdateAsync(rental);
        }

        private string GetColorByStatus(RentalStatus status)
        {
            return status switch
            {
                RentalStatus.Active => "#28a745",
                RentalStatus.Extended => "#007bff",
                RentalStatus.Draft => "#ffc107",
                RentalStatus.Expired => "#6c757d",
                RentalStatus.Cancelled => "#dc3545",
                _ => "#6c757d"
            };
        }

        private static PriceBreakdownDto? DeserializePriceBreakdown(string? priceBreakdownJson)
        {
            if (string.IsNullOrWhiteSpace(priceBreakdownJson))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<PriceBreakdownDto>(priceBreakdownJson);
            }
            catch
            {
                // If deserialization fails, return null to avoid breaking the response
                return null;
            }
        }
    }
}
