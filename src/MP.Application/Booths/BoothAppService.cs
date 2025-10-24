using AutoMapper.Internal.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MP.Domain.Booths;
using MP.Domain.Rentals;
using MP.Rentals;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Uow;
using Volo.Abp;
using MP.Permissions;
using MP.Domain.BoothTypes;

namespace MP.Booths
{
    [Authorize(MPPermissions.Booths.Default)]
    public class BoothAppService : ApplicationService, IBoothAppService
    {
        private readonly IBoothRepository _boothRepository;
        private readonly IRentalRepository _rentalRepository;
        private readonly BoothManager _boothManager;
        private readonly RentalManager _rentalManager;
        private readonly IBoothTypeRepository _boothTypeRepository;

        public BoothAppService(
            IBoothRepository boothRepository,
            IRentalRepository rentalRepository,
            BoothManager boothManager,
            RentalManager rentalManager,
            IBoothTypeRepository boothTypeRepository)
        {
            _boothRepository = boothRepository;
            _rentalRepository = rentalRepository;
            _boothManager = boothManager;
            _rentalManager = rentalManager;
            _boothTypeRepository = boothTypeRepository;
        }

        public async Task<BoothDto> GetAsync(Guid id)
        {
            var booth = await _boothRepository.GetAsync(id, includeDetails: true);
            return ObjectMapper.Map<Booth, BoothDto>(booth);
        }

        [HttpGet]
        public async Task<PagedResultDto<BoothListDto>> GetListAsync(GetBoothListDto input)
        {
            var totalCount = await _boothRepository.GetCountAsync(input.Filter, input.Status);
            var items = await _boothRepository.GetListWithActiveRentalsAsync(
                input.SkipCount,
                input.MaxResultCount,
                input.Filter,
                input.Status
            );

            // Use projection to load only required rental fields instead of full User entity
            var queryable = await _rentalRepository.GetQueryableAsync();
            var today = DateTime.Today;

            var activeRentalsProjection = await AsyncExecuter.ToListAsync(
                queryable
                    .AsNoTracking()
                    .Where(r => (r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended) &&
                               r.Period.StartDate <= today && r.Period.EndDate >= today)
                    .Select(r => new ActiveRentalProjection
                    {
                        Id = r.Id,
                        BoothId = r.BoothId,
                        UserName = r.User.Name,
                        UserSurname = r.User.Surname,
                        UserEmail = r.User.Email,
                        StartDate = r.Period.StartDate,
                        EndDate = r.Period.EndDate
                    })
            );

            // Create a lookup by BoothId
            var activeRentalsByBoothId = activeRentalsProjection
                .GroupBy(r => r.BoothId)
                .ToDictionary(g => g.Key, g => g.First());

            // Map booths to DTOs
            var dtos = ObjectMapper.Map<List<Booth>, List<BoothListDto>>(items);

            // Populate CurrentRental fields
            foreach (var dto in dtos)
            {
                if (activeRentalsByBoothId.TryGetValue(dto.Id, out var rental))
                {
                    dto.CurrentRentalId = rental.Id;
                    dto.CurrentRentalUserName = $"{rental.UserName} {rental.UserSurname}";
                    dto.CurrentRentalUserEmail = rental.UserEmail;
                    dto.CurrentRentalStartDate = rental.StartDate;
                    dto.CurrentRentalEndDate = rental.EndDate;
                }
            }

            return new PagedResultDto<BoothListDto>(totalCount, dtos);
        }

        [Authorize(MPPermissions.Booths.Create)]
        [HttpPost]
        public async Task<BoothDto> CreateAsync(CreateBoothDto input)
        {
            Booth booth;

            // Use new multi-period pricing if provided, otherwise fallback to legacy single price
            if (input.PricingPeriods != null && input.PricingPeriods.Count > 0)
            {
                var pricingPeriods = input.PricingPeriods
                    .Select(p => (p.Days, p.PricePerPeriod))
                    .ToList();

                booth = await _boothManager.CreateWithPricingPeriodsAsync(
                    input.Number,
                    pricingPeriods,
                    input.OrganizationalUnitId
                );
            }
            else
            {
                // Backward compatibility - use legacy PricePerDay
                if (!input.PricePerDay.HasValue)
                {
                    throw new BusinessException("BOOTH_PRICE_REQUIRED")
                        .WithData("message", "Either PricingPeriods or PricePerDay must be provided");
                }

                booth = await _boothManager.CreateAsync(
                    input.Number,
                    input.PricePerDay.Value,
                    input.OrganizationalUnitId
                );
            }

            await _boothRepository.InsertAsync(booth);
            await UnitOfWorkManager.Current!.SaveChangesAsync();

            return ObjectMapper.Map<Booth, BoothDto>(booth);
        }

        [Authorize(MPPermissions.Booths.Edit)]
        public async Task<BoothDto> UpdateAsync(Guid id, UpdateBoothDto input)
        {
            var booth = await _boothRepository.GetAsync(id, includeDetails: true);

            // Zmiana numeru (przez domain service dla walidacji)
            if (booth.Number != input.Number.ToUpper())
            {
                await _boothManager.ChangeNumberAsync(booth, input.Number);
            }

            // Update pricing - use new multi-period pricing if provided, otherwise legacy single price
            if (input.PricingPeriods != null && input.PricingPeriods.Count > 0)
            {
                var pricingPeriods = input.PricingPeriods
                    .Select(p => (p.Days, p.PricePerPeriod))
                    .ToList();

                _boothManager.UpdatePricingPeriods(booth, pricingPeriods);
            }
            else if (input.PricePerDay.HasValue)
            {
                // Backward compatibility - use legacy PricePerDay
                booth.SetPricePerDay(input.PricePerDay.Value);
            }

            // Zmiana statusu
            switch (input.Status)
            {
                case BoothStatus.Available:
                    booth.MarkAsAvailable();
                    break;
                case BoothStatus.Maintenance:
                    booth.MarkAsMaintenance();
                    break;
                    // Rented nie pozwalamy zmieniać ręcznie - to robi się przez wynajęcie
            }

            await _boothRepository.UpdateAsync(booth);
            await UnitOfWorkManager.Current!.SaveChangesAsync();

            return ObjectMapper.Map<Booth, BoothDto>(booth);
        }

        [Authorize(MPPermissions.Booths.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            var booth = await _boothRepository.GetAsync(id);

            // Sprawdź czy stanowisko nie jest wynajęte
            if (booth.Status == BoothStatus.Rented)
            {
                throw new BusinessException("CANNOT_DELETE_RENTED_BOOTH");
            }

            await _boothRepository.DeleteAsync(booth);
        }

        [HttpGet("available")]
        public async Task<List<BoothDto>> GetAvailableBoothsAsync()
        {
            var booths = await _boothRepository.GetAvailableBoothsAsync();
            return ObjectMapper.Map<List<Booth>, List<BoothDto>>(booths);
        }

        [HttpPost("{id}/change-status")]
        [Authorize(MPPermissions.Booths.Edit)]
        public async Task<BoothDto> ChangeStatusAsync(Guid id, BoothStatus newStatus)
        {
            var booth = await _boothRepository.GetAsync(id);

            switch (newStatus)
            {
                case BoothStatus.Available:
                    // If booth is in maintenance, restore to previous status instead of forcing Available
                    if (booth.Status == BoothStatus.Maintenance)
                    {
                        booth.RestoreFromMaintenance();
                    }
                    else
                    {
                        booth.MarkAsAvailable();
                    }
                    break;
                case BoothStatus.Maintenance:
                    booth.MarkAsMaintenance();
                    break;
                case BoothStatus.Rented:
                    booth.MarkAsRented();
                    break;
            }

            await _boothRepository.UpdateAsync(booth);
            return ObjectMapper.Map<Booth, BoothDto>(booth);
        }

        [HttpGet("my-booths")]
        public async Task<PagedResultDto<BoothListDto>> GetMyBoothsAsync([FromQuery] GetBoothListDto input)
        {
            var totalCount = await _boothRepository.GetCountAsync(input.Filter, input.Status);
            var items = await _boothRepository.GetListWithActiveRentalsAsync(
                input.SkipCount,
                input.MaxResultCount,
                input.Filter,
                input.Status
            );

            // Use projection to load only required rental fields instead of full User entity
            var queryable = await _rentalRepository.GetQueryableAsync();
            var today = DateTime.Today;

            var activeRentalsProjection = await AsyncExecuter.ToListAsync(
                queryable
                    .AsNoTracking()
                    .Where(r => (r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended) &&
                               r.Period.StartDate <= today && r.Period.EndDate >= today)
                    .Select(r => new ActiveRentalProjection
                    {
                        Id = r.Id,
                        BoothId = r.BoothId,
                        UserName = r.User.Name,
                        UserSurname = r.User.Surname,
                        UserEmail = r.User.Email,
                        StartDate = r.Period.StartDate,
                        EndDate = r.Period.EndDate
                    })
            );

            // Create a lookup by BoothId
            var activeRentalsByBoothId = activeRentalsProjection
                .GroupBy(r => r.BoothId)
                .ToDictionary(g => g.Key, g => g.First());

            // Map booths to DTOs
            var dtos = ObjectMapper.Map<List<Booth>, List<BoothListDto>>(items);

            // Populate CurrentRental fields
            foreach (var dto in dtos)
            {
                if (activeRentalsByBoothId.TryGetValue(dto.Id, out var rental))
                {
                    dto.CurrentRentalId = rental.Id;
                    dto.CurrentRentalUserName = $"{rental.UserName} {rental.UserSurname}";
                    dto.CurrentRentalUserEmail = rental.UserEmail;
                    dto.CurrentRentalStartDate = rental.StartDate;
                    dto.CurrentRentalEndDate = rental.EndDate;
                }
            }

            return new PagedResultDto<BoothListDto>(totalCount, dtos);
        }

        // Projection class for active rental queries - loads only required fields
        private class ActiveRentalProjection
        {
            public Guid Id { get; set; }
            public Guid BoothId { get; set; }
            public string UserName { get; set; } = null!;
            public string UserSurname { get; set; } = null!;
            public string? UserEmail { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        // Helper method do wyświetlania nazw enum
        private string GetEnumDisplayName<T>(T enumValue) where T : Enum
        {
            var field = typeof(T).GetField(enumValue.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? enumValue.ToString();
        }

        [Authorize(MPPermissions.Booths.ManualReservation)]
        public async Task<BoothDto> CreateManualReservationAsync(CreateManualReservationDto input)
        {
            // Validate target status
            if (input.TargetStatus != BoothStatus.Reserved && input.TargetStatus != BoothStatus.Rented)
            {
                throw new BusinessException("INVALID_MANUAL_RESERVATION_STATUS");
            }

            // Get first active booth type as default
            var boothTypes = await _boothTypeRepository.GetListAsync();
            var defaultBoothType = boothTypes.FirstOrDefault(bt => bt.IsActive);

            if (defaultBoothType == null)
            {
                throw new BusinessException("NO_ACTIVE_BOOTH_TYPE_AVAILABLE");
            }

            // Create rental using RentalManager
            var rental = await _rentalManager.CreateRentalAsync(
                input.UserId,
                input.BoothId,
                defaultBoothType.Id,
                input.StartDate,
                input.EndDate
            );

            // Insert rental
            await _rentalRepository.InsertAsync(rental);

            // If target status is Rented, confirm the rental immediately
            if (input.TargetStatus == BoothStatus.Rented)
            {
                // Mark as paid to allow confirmation
                rental.MarkAsPaid(rental.Payment.TotalAmount, DateTime.Now, "MANUAL_RESERVATION");

                // Get booth and mark as rented
                var booth = await _boothRepository.GetAsync(input.BoothId);
                booth.MarkAsRented();
                await _boothRepository.UpdateAsync(booth);
            }

            await UnitOfWorkManager.Current!.SaveChangesAsync();

            // Return updated booth with rental info
            return await GetAsync(input.BoothId);
        }
    }
}