using AutoMapper.Internal.Mappers;
using Microsoft.AspNetCore.Authorization;
using MP.Domain.Booths;
using MP.Domain.Rentals;
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
            var booth = await _boothRepository.GetAsync(id);
            return ObjectMapper.Map<Booth, BoothDto>(booth);
        }

        public async Task<PagedResultDto<BoothListDto>> GetListAsync(GetBoothListDto input)
        {
            var totalCount = await _boothRepository.GetCountAsync(input.Filter, input.Status);
            var items = await _boothRepository.GetListWithActiveRentalsAsync(
                input.SkipCount,
                input.MaxResultCount,
                input.Filter,
                input.Status
            );

            // Get all active rentals
            var activeRentals = await _rentalRepository.GetActiveRentalsAsync();
            var today = DateTime.Today;

            // Filter to only rentals active today and create a lookup by BoothId
            var activeRentalsByBoothId = activeRentals
                .Where(r => r.Period.StartDate <= today && r.Period.EndDate >= today)
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
                    dto.CurrentRentalUserName = $"{rental.User.Name} {rental.User.Surname}";
                    dto.CurrentRentalUserEmail = rental.User.Email;
                    dto.CurrentRentalStartDate = rental.Period.StartDate;
                    dto.CurrentRentalEndDate = rental.Period.EndDate;
                }
            }

            return new PagedResultDto<BoothListDto>(totalCount, dtos);
        }

        [Authorize(MPPermissions.Booths.Create)]
        public async Task<BoothDto> CreateAsync(CreateBoothDto input)
        {
            var booth = await _boothManager.CreateAsync(
                input.Number,
                input.PricePerDay,
                input.Currency
            );

            await _boothRepository.InsertAsync(booth);
            await UnitOfWorkManager.Current!.SaveChangesAsync();

            return ObjectMapper.Map<Booth, BoothDto>(booth);
        }

        [Authorize(MPPermissions.Booths.Edit)]
        public async Task<BoothDto> UpdateAsync(Guid id, UpdateBoothDto input)
        {
            var booth = await _boothRepository.GetAsync(id);

            // Zmiana numeru (przez domain service dla walidacji)
            if (booth.Number != input.Number.ToUpper())
            {
                await _boothManager.ChangeNumberAsync(booth, input.Number);
            }

            // Inne zmiany
            booth.SetPricePerDay(input.PricePerDay);
            booth.SetCurrency(input.Currency);

            // Zmiana statusu
            switch (input.Status)
            {
                case BoothStatus.Available:
                    booth.MarkAsAvailable();
                    break;
                case BoothStatus.Maintenance:
                    booth.MarkAsMaintenace();
                    break;
                    // Rented nie pozwalamy zmieniać ręcznie - to robi się przez wynajęcie
            }

            await _boothRepository.UpdateAsync(booth);
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

        public async Task<List<BoothDto>> GetAvailableBoothsAsync()
        {
            var booths = await _boothRepository.GetAvailableBoothsAsync();
            return ObjectMapper.Map<List<Booth>, List<BoothDto>>(booths);
        }

        [Authorize(MPPermissions.Booths.Edit)]
        public async Task<BoothDto> ChangeStatusAsync(Guid id, BoothStatus newStatus)
        {
            var booth = await _boothRepository.GetAsync(id);

            switch (newStatus)
            {
                case BoothStatus.Available:
                    booth.MarkAsAvailable();
                    break;
                case BoothStatus.Maintenance:
                    booth.MarkAsMaintenace();
                    break;
                case BoothStatus.Rented:
                    booth.MarkAsRented();
                    break;
            }

            await _boothRepository.UpdateAsync(booth);
            return ObjectMapper.Map<Booth, BoothDto>(booth);
        }

        public async Task<PagedResultDto<BoothListDto>> GetMyBoothsAsync(GetBoothListDto input)
        {
            var totalCount = await _boothRepository.GetCountAsync(input.Filter, input.Status);
            var items = await _boothRepository.GetListWithActiveRentalsAsync(
                input.SkipCount,
                input.MaxResultCount,
                input.Filter,
                input.Status
            );

            // Get all active rentals
            var activeRentals = await _rentalRepository.GetActiveRentalsAsync();
            var today = DateTime.Today;

            // Filter to only rentals active today and create a lookup by BoothId
            var activeRentalsByBoothId = activeRentals
                .Where(r => r.Period.StartDate <= today && r.Period.EndDate >= today)
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
                    dto.CurrentRentalUserName = $"{rental.User.Name} {rental.User.Surname}";
                    dto.CurrentRentalUserEmail = rental.User.Email;
                    dto.CurrentRentalStartDate = rental.Period.StartDate;
                    dto.CurrentRentalEndDate = rental.Period.EndDate;
                }
            }

            return new PagedResultDto<BoothListDto>(totalCount, dtos);
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