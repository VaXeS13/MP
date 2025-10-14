using AutoFixture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;
using Volo.Abp.MultiTenancy;
using Volo.Abp;
using MP.Domain.Booths;
using MP.Rentals;
using Volo.Abp.Settings;
using MP.Domain.Settings;

namespace MP.Domain.Rentals
{
    public class RentalManager : DomainService
    {
        private readonly IRentalRepository _rentalRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly BoothTypes.IBoothTypeRepository _boothTypeRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly ISettingProvider _settingProvider;

        public RentalManager(
            IRentalRepository rentalRepository,
            IBoothRepository boothRepository,
            BoothTypes.IBoothTypeRepository boothTypeRepository,
            ICurrentTenant currentTenant,
            ISettingProvider settingProvider)
        {
            _rentalRepository = rentalRepository;
            _boothRepository = boothRepository;
            _boothTypeRepository = boothTypeRepository;
            _currentTenant = currentTenant;
            _settingProvider = settingProvider;
        }

        public async Task<Rental> CreateRentalAsync(
            Guid userId,
            Guid boothId,
            Guid boothTypeId,
            DateTime startDate,
            DateTime endDate,
            decimal? customDailyRate = null)
        {
            // Sprawdź czy stanowisko istnieje
            var booth = await _boothRepository.GetAsync(boothId);

            // Sprawdź czy typ stanowiska istnieje
            var boothType = await _boothTypeRepository.GetAsync(boothTypeId);
            if (!boothType.IsActive)
            {
                throw new BusinessException("BOOTH_TYPE_NOT_AVAILABLE");
            }

            // Utwórz okres wynajęcia (waliduje 7 dni minimum)
            var period = new RentalPeriod(startDate, endDate);

            // Sprawdź czy stanowisko jest wolne w tym okresie (to też sprawdzi Maintenance)
            await ValidateBoothAvailabilityAsync(boothId, period);

            // Oblicz całkowity koszt używając ceny stanowiska lub custom rate
            var dailyRate = customDailyRate ?? booth.PricePerDay;
            var totalCost = CalculateTotalCost(period, dailyRate);

            // Pobierz walutę tenanta
            var currency = await GetTenantCurrencyAsync();

            // Oznacz stanowisko jako zarezerwowane
            booth.MarkAsReserved();

            // Utwórz wynajęcie z walutą tenanta
            var rental = new Rental(
                GuidGenerator.Create(),
                userId,
                boothId,
                boothTypeId,
                period,
                totalCost,
                currency,
                _currentTenant.Id
            );

            return rental;
        }

        public async Task ValidateExtensionAsync(Rental rental, DateTime newEndDate)
        {
            var newPeriod = new RentalPeriod(rental.Period.StartDate, newEndDate);

            // Sprawdź konflikty po dacie rozszerzenia
            var hasConflict = await _rentalRepository.HasActiveRentalForBoothAsync(
                rental.BoothId,
                rental.Period.EndDate.AddDays(1),
                newEndDate,
                rental.Id);

            if (hasConflict)
            {
                throw new BusinessException("CANNOT_EXTEND_DUE_TO_EXISTING_RENTAL");
            }
        }

        public async Task ValidateGapRulesAsync(
            Guid boothId,
            DateTime startDate,
            DateTime endDate,
            Guid? excludeRentalId = null)
        {
            var period = new RentalPeriod(startDate, endDate);

            // Check for conflicts
            var hasConflict = await _rentalRepository.HasActiveRentalForBoothAsync(
                boothId,
                startDate,
                endDate,
                excludeRentalId);

            if (hasConflict)
            {
                throw new BusinessException("BOOTH_ALREADY_RENTED_IN_PERIOD")
                    .WithData("BoothId", boothId)
                    .WithData("StartDate", startDate)
                    .WithData("EndDate", endDate);
            }

            // Validate gap rules (no gaps between rentals)
            await ValidateNoGapsAsync(boothId, period);
        }

        private async Task ValidateBoothAvailabilityAsync(Guid boothId, RentalPeriod newPeriod)
        {
            // Sprawdź status stanowiska - tylko Maintenance blokuje rezerwację
            var booth = await _boothRepository.GetAsync(boothId);
            if (booth.Status == BoothStatus.Maintenance)
            {
                throw new BusinessException("BOOTH_IN_MAINTENANCE")
                    .WithData("BoothId", boothId)
                    .WithData("Status", booth.Status);
            }

            // Sprawdź czy stanowisko jest wolne w tym okresie
            var hasConflict = await _rentalRepository.HasActiveRentalForBoothAsync(
                boothId, newPeriod.StartDate, newPeriod.EndDate);

            if (hasConflict)
            {
                throw new BusinessException("BOOTH_ALREADY_RENTED_IN_PERIOD");
            }

            // Sprawdź czy nie ma pustych dni (gap) między wynajęciami
            await ValidateNoGapsAsync(boothId, newPeriod);
        }

        private async Task ValidateNoGapsAsync(Guid boothId, RentalPeriod newPeriod)
        {
            var existingRentals = await _rentalRepository.GetRentalsForBoothAsync(boothId);

            foreach (var existingRental in existingRentals)
            {
                if (existingRental.Status != RentalStatus.Active &&
                    existingRental.Status != RentalStatus.Extended)
                    continue;

                // Sprawdź czy jest gap przed nowym wynajęciem
                /* TODO if (existingRental.Period.EndDate.AddDays(1) < newPeriod.StartDate)
                 {// Czy w ogole powinno to sprawdzac

                     var gap = newPeriod.StartDate - existingRental.Period.EndDate.AddDays(1);
                     if (gap.Days > 7)
                     {
                         throw new BusinessException("RENTAL_CANNOT_HAVE_GAPS")
                             .WithData("gapDays", gap.Days);
                     }tak
            }*/
            }
        }

        public async Task<decimal> CalculateRentalCostAsync(Guid boothId, Guid boothTypeId, RentalPeriod period)
        {
            var booth = await _boothRepository.GetAsync(boothId);
            var boothType = await _boothTypeRepository.GetAsync(boothTypeId);

            if (!boothType.IsActive)
            {
                throw new BusinessException("BOOTH_TYPE_NOT_AVAILABLE");
            }

            return CalculateTotalCost(period, booth.PricePerDay);
        }

        private decimal CalculateTotalCost(RentalPeriod period, decimal dailyRate)
        {
            return period.GetDaysCount() * dailyRate;
        }

        private async Task<Currency> GetTenantCurrencyAsync()
        {
            var currencySetting = await _settingProvider.GetOrNullAsync(MPSettings.Tenant.Currency);
            if (int.TryParse(currencySetting, out var currencyValue))
            {
                return (Currency)currencyValue;
            }

            // Default to PLN if setting not found
            return Currency.PLN;
        }
    }
}
