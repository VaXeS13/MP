using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Volo.Abp;
using MP.Permissions;
using MP.Domain.Booths;
using MP.Domain.Rentals;
using MP.Domain.Carts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Volo.Abp.Uow;
using MP.Carts;
using MP.Application.Contracts.Services;

namespace MP.Rentals
{
    [Authorize(MPPermissions.Rentals.Default)]
    public class RentalAppService : ApplicationService, IRentalAppService
    {
        private readonly IRentalRepository _rentalRepository;
        private readonly IBoothRepository _boothRepository;
        private readonly RentalManager _rentalManager;
        private readonly IIdentityUserRepository _userRepository;
        private readonly ICartRepository _cartRepository;
        private readonly ISignalRNotificationService _signalRNotificationService;

        public RentalAppService(
            IRentalRepository rentalRepository,
            IBoothRepository boothRepository,
            RentalManager rentalManager,
            IIdentityUserRepository userRepository,
            ICartRepository cartRepository,
            ISignalRNotificationService signalRNotificationService)
        {
            _rentalRepository = rentalRepository;
            _boothRepository = boothRepository;
            _rentalManager = rentalManager;
            _userRepository = userRepository;
            _cartRepository = cartRepository;
            _signalRNotificationService = signalRNotificationService;
        }

        public async Task<RentalDto> GetAsync(Guid id)
        {
            var rental = await _rentalRepository.GetRentalWithItemsAsync(id);
            if (rental == null)
                throw new EntityNotFoundException(typeof(Rental), id);

            return ObjectMapper.Map<Rental, RentalDto>(rental);
        }

        public async Task<PagedResultDto<RentalListDto>> GetListAsync(GetRentalListDto input)
        {
            var queryable = await _rentalRepository.GetQueryableAsync();

            // Include navigation properties
            queryable = queryable
                .Include(r => r.User)
                .Include(r => r.Booth);

            // Filtering
            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                var filter = input.Filter.ToLower();
                queryable = queryable.Where(r =>
                    r.User.Name.ToLower().Contains(filter) ||
                    r.User.Surname.ToLower().Contains(filter) ||
                    r.User.Email.ToLower().Contains(filter) ||
                    r.Booth.Number.ToLower().Contains(filter));
            }

            if (input.Status.HasValue)
                queryable = queryable.Where(r => r.Status == input.Status.Value);

            if (input.UserId.HasValue)
                queryable = queryable.Where(r => r.UserId == input.UserId.Value);

            if (input.BoothId.HasValue)
                queryable = queryable.Where(r => r.BoothId == input.BoothId.Value);

            if (input.FromDate.HasValue)
                queryable = queryable.Where(r => r.Period.StartDate >= input.FromDate.Value);

            if (input.ToDate.HasValue)
                queryable = queryable.Where(r => r.Period.EndDate <= input.ToDate.Value);

            if (input.IsOverdue.HasValue && input.IsOverdue.Value)
                queryable = queryable.Where(r =>
                    (r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended) &&
                    r.Period.EndDate < DateTime.Today);

            // Sorting
            if (string.IsNullOrEmpty(input.Sorting))
                queryable = queryable.OrderByDescending(r => r.CreationTime);

            var totalCount = await AsyncExecuter.CountAsync(queryable);

            var items = await AsyncExecuter.ToListAsync(
                queryable.Skip(input.SkipCount).Take(input.MaxResultCount));

            var dtos = ObjectMapper.Map<List<Rental>, List<RentalListDto>>(items);

            return new PagedResultDto<RentalListDto>(totalCount, dtos);
        }

        [Authorize(MPPermissions.Rentals.Create)]
        public async Task<RentalDto> CreateAsync(CreateRentalDto input)
        {
            // Sprawdź czy użytkownik istnieje
            var user = await _userRepository.GetAsync(input.UserId);
            var booth = await _boothRepository.GetAsync(input.BoothId);

            // Utwórz wynajęcie przez domain service
            var rental = await _rentalManager.CreateRentalAsync(
                input.UserId,
                input.BoothId,
                input.BoothTypeId,
                input.StartDate,
                input.EndDate,
                booth.PricePerDay
            );

            rental.SetNotes(input.Notes);

            // Zapisz wynajęcie
            await _rentalRepository.InsertAsync(rental);

            // Zapisz booth ze zmienionym statusem (Reserved)
            await _boothRepository.UpdateAsync(booth);

            // Send SignalR notification about booth status change
            await _signalRNotificationService.SendBoothStatusUpdateAsync(
                CurrentTenant.Id,
                booth.Id,
                booth.Status.ToString(),
                !booth.IsAvailable(),
                rental.Id,
                rental.Period.EndDate
            );

            // Zwróć bezpośrednio z utworzonego obiektu (nie pobieraj ponownie z DB)
            return ObjectMapper.Map<Rental, RentalDto>(rental);
        }

        [Authorize(MPPermissions.Rentals.Edit)]
        public async Task<RentalDto> UpdateAsync(Guid id, UpdateRentalDto input)
        {
            var rental = await _rentalRepository.GetAsync(id);

            // Można edytować tylko wynajęcia w statusie Draft
            if (rental.Status != RentalStatus.Draft)
                throw new BusinessException("CAN_ONLY_EDIT_DRAFT_RENTALS");

            // Waliduj nowy okres przez domain service
            var newPeriod = new RentalPeriod(input.StartDate, input.EndDate);
            var booth = await _boothRepository.GetAsync(rental.BoothId);
            var newTotalCost = newPeriod.GetDaysCount() * booth.PricePerDay;

            // Sprawdź konflikty dla nowego okresu
            var hasConflict = await _rentalRepository.HasActiveRentalForBoothAsync(
                rental.BoothId, input.StartDate, input.EndDate, rental.Id);

            if (hasConflict)
                throw new BusinessException("BOOTH_ALREADY_RENTED_IN_NEW_PERIOD");

            // Aktualizuj wynajęcie (to będzie wymagało rozszerzenia Rental domain)
            // rental.UpdatePeriod(newPeriod, newTotalCost); // TODO: Dodaj tę metodę do Rental
            rental.SetNotes(input.Notes);

            await _rentalRepository.UpdateAsync(rental);

            var updatedRental = await _rentalRepository.GetRentalWithItemsAsync(rental.Id);
            return ObjectMapper.Map<Rental, RentalDto>(updatedRental!);
        }

        [Authorize(MPPermissions.Rentals.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            var rental = await _rentalRepository.GetAsync(id);

            // Można usunąć tylko wynajęcia Draft lub Cancelled
            if (rental.Status != RentalStatus.Draft && rental.Status != RentalStatus.Cancelled)
                throw new BusinessException("CAN_ONLY_DELETE_DRAFT_OR_CANCELLED_RENTALS");

            await _rentalRepository.DeleteAsync(rental);
        }

        [Authorize(MPPermissions.Rentals.Manage)]
        public async Task<RentalDto> PayAsync(Guid id, PaymentDto input)
        {
            var rental = await _rentalRepository.GetRentalWithItemsAsync(id);
            if (rental == null)
                throw new EntityNotFoundException(typeof(Rental), id);

            rental.MarkAsPaid(input.Amount, input.PaidDate);

            await _rentalRepository.UpdateAsync(rental);

            return ObjectMapper.Map<Rental, RentalDto>(rental);
        }

        [Authorize(MPPermissions.Rentals.Manage)]
        public async Task<RentalDto> StartRentalAsync(Guid id)
        {
            var rental = await _rentalRepository.GetRentalWithItemsAsync(id);
            if (rental == null)
                throw new EntityNotFoundException(typeof(Rental), id);

            rental.StartRental();

            await _rentalRepository.UpdateAsync(rental);

            // Send SignalR notification about booth status change
            var booth = await _boothRepository.GetAsync(rental.BoothId);
            await _signalRNotificationService.SendBoothStatusUpdateAsync(
                CurrentTenant.Id,
                booth.Id,
                booth.Status.ToString(),
                !booth.IsAvailable(),
                rental.Id,
                rental.Period.EndDate
            );

            return ObjectMapper.Map<Rental, RentalDto>(rental);
        }

        [Authorize(MPPermissions.Rentals.Manage)]
        public async Task<RentalDto> CompleteRentalAsync(Guid id)
        {
            var rental = await _rentalRepository.GetRentalWithItemsAsync(id);
            if (rental == null)
                throw new EntityNotFoundException(typeof(Rental), id);

            rental.CompleteRental();

            await _rentalRepository.UpdateAsync(rental);

            // Send SignalR notification about booth becoming available
            var booth = await _boothRepository.GetAsync(rental.BoothId);
            await _signalRNotificationService.SendBoothStatusUpdateAsync(
                CurrentTenant.Id,
                booth.Id,
                booth.Status.ToString(),
                !booth.IsAvailable(),
                null,
                null
            );

            return ObjectMapper.Map<Rental, RentalDto>(rental);
        }

        [Authorize(MPPermissions.Rentals.Manage)]
        public async Task<RentalDto> CancelRentalAsync(Guid id, string reason)
        {
            var rental = await _rentalRepository.GetRentalWithItemsAsync(id);
            if (rental == null)
                throw new EntityNotFoundException(typeof(Rental), id);

            rental.Cancel(reason);

            await _rentalRepository.UpdateAsync(rental);

            // Send SignalR notification about booth becoming available
            var booth = await _boothRepository.GetAsync(rental.BoothId);
            await _signalRNotificationService.SendBoothStatusUpdateAsync(
                CurrentTenant.Id,
                booth.Id,
                booth.Status.ToString(),
                !booth.IsAvailable(),
                null,
                null
            );

            return ObjectMapper.Map<Rental, RentalDto>(rental);
        }

        [Authorize(MPPermissions.Rentals.Manage)]
        public async Task<RentalDto> ExtendRentalAsync(Guid id, ExtendRentalDto input)
        {
            var rental = await _rentalRepository.GetRentalWithItemsAsync(id);
            if (rental == null)
                throw new EntityNotFoundException(typeof(Rental), id);

            var booth = await _boothRepository.GetAsync(rental.BoothId);

            // Walidacja przez domain service
            await _rentalManager.ValidateExtensionAsync(rental, input.NewEndDate);

            // Oblicz dodatkowy koszt
            var currentPeriod = rental.Period;
            var newPeriod = new RentalPeriod(currentPeriod.StartDate, input.NewEndDate);
            var additionalDays = newPeriod.GetDaysCount() - currentPeriod.GetDaysCount();
            var additionalCost = additionalDays * booth.PricePerDay;

            rental.ExtendRental(newPeriod, additionalCost);

            await _rentalRepository.UpdateAsync(rental);

            return ObjectMapper.Map<Rental, RentalDto>(rental);
        }

        public async Task<PagedResultDto<RentalListDto>> GetMyRentalsAsync(GetRentalListDto input)
        {
            input.UserId = CurrentUser.GetId();
            return await GetListAsync(input);
        }

        public async Task<RentalDto> CreateMyRentalAsync(CreateMyRentalDto input)
        {
            // Automatically use current user's ID
            var createDto = new CreateRentalDto
            {
                UserId = CurrentUser.GetId(),
                BoothId = input.BoothId,
                BoothTypeId = input.BoothTypeId,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                Notes = input.Notes
            };

            return await CreateAsync(createDto);
        }

        public async Task<CreateRentalWithPaymentResultDto> CreateMyRentalWithPaymentAsync(CreateRentalWithPaymentDto input)
        {
            try
            {
                // 1. Validate booth availability FIRST (without creating rental yet)
                var booth = await _boothRepository.GetAsync(input.BoothId);
                if (!booth.IsAvailable())
                {
                    return new CreateRentalWithPaymentResultDto
                    {
                        Success = false,
                        ErrorMessage = "Booth is not available"
                    };
                }

                // 2. Check if booth is available for the selected period
                var hasConflict = await _rentalRepository.HasActiveRentalForBoothAsync(
                    input.BoothId, input.StartDate, input.EndDate);

                if (hasConflict)
                {
                    return new CreateRentalWithPaymentResultDto
                    {
                        Success = false,
                        ErrorMessage = "Booth is already rented for the selected period"
                    };
                }

                // 3. Create rental using existing logic (this will reserve booth)
                var createDto = new CreateRentalDto
                {
                    UserId = CurrentUser.GetId(),
                    BoothId = input.BoothId,
                    BoothTypeId = input.BoothTypeId,
                    StartDate = input.StartDate,
                    EndDate = input.EndDate,
                    Notes = input.Notes
                };

                var rental = await CreateAsync(createDto);

                // 3.5. Force save changes to ensure rental is persisted before payment
                await UnitOfWorkManager.Current!.SaveChangesAsync();

                // 4. Create payment request
                var paymentRequest = new MP.Application.Contracts.Payments.CreatePaymentRequestDto
                {
                    Amount = rental.TotalAmount,
                    Currency = booth.Currency.ToString(),
                    Description = $"Booth rental {booth.Number} from {input.StartDate:yyyy-MM-dd} to {input.EndDate:yyyy-MM-dd}",
                    ProviderId = input.PaymentProviderId,
                    MethodId = input.PaymentMethodId,
                    Metadata = new Dictionary<string, object>
                    {
                        ["rentalId"] = rental.Id,
                        ["boothId"] = input.BoothId,
                        ["boothTypeId"] = input.BoothTypeId
                    }
                };

                // 5. Call payment service to create payment
                var paymentProviderService = LazyServiceProvider.LazyGetRequiredService<MP.Application.Contracts.Payments.IPaymentProviderAppService>();
                var paymentResult = await paymentProviderService.CreatePaymentAsync(paymentRequest);

                if (!paymentResult.Success)
                {
                    // Payment creation failed - we should cancel/delete the rental
                    await DeleteAsync(rental.Id);

                    return new CreateRentalWithPaymentResultDto
                    {
                        Success = false,
                        ErrorMessage = paymentResult.ErrorMessage ?? "Payment creation failed"
                    };
                }

                return new CreateRentalWithPaymentResultDto
                {
                    Success = true,
                    RentalId = rental.Id,
                    TransactionId = paymentResult.TransactionId,
                    PaymentUrl = paymentResult.PaymentUrl
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating rental with payment");
                return new CreateRentalWithPaymentResultDto
                {
                    Success = false,
                    ErrorMessage = "An error occurred while creating the rental"
                };
            }
        }

        public async Task<List<RentalListDto>> GetActiveRentalsAsync()
        {
            var rentals = await _rentalRepository.GetActiveRentalsAsync();
            return ObjectMapper.Map<List<Rental>, List<RentalListDto>>(rentals);
        }

        public async Task<List<RentalListDto>> GetExpiredRentalsAsync()
        {
            var rentals = await _rentalRepository.GetExpiredRentalsAsync(DateTime.Today);
            return ObjectMapper.Map<List<Rental>, List<RentalListDto>>(rentals);
        }

        public async Task<List<RentalListDto>> GetOverdueRentalsAsync()
        {
            var rentals = await _rentalRepository.GetExpiredRentalsAsync(DateTime.Today.AddDays(-7));
            return ObjectMapper.Map<List<Rental>, List<RentalListDto>>(rentals);
        }

        public async Task<BoothCalendarResponseDto> GetBoothCalendarAsync(BoothCalendarRequestDto input)
        {
            // Get booth information
            var booth = await _boothRepository.GetAsync(input.BoothId);
            if (booth == null)
                throw new EntityNotFoundException(typeof(Booth), input.BoothId);

            // Get all rentals for this booth that overlap with the requested date range
            var queryable = await _rentalRepository.GetQueryableAsync();
            var rentals = await AsyncExecuter.ToListAsync(
                queryable
                    .Include(r => r.User)
                    .Where(r => r.BoothId == input.BoothId &&
                               r.Period.StartDate <= input.EndDate &&
                               r.Period.EndDate >= input.StartDate &&
                               (r.Status == RentalStatus.Draft ||
                                r.Status == RentalStatus.Active ||
                                r.Status == RentalStatus.Extended))
            );

            // Generate calendar dates
            var calendarDates = new List<CalendarDateDto>();
            var currentDate = input.StartDate.Date;

            while (currentDate <= input.EndDate.Date)
            {
                var dateStatus = GetDateStatus(currentDate, rentals);
                var rentalForDate = GetRentalForDate(currentDate, rentals);

                var calendarDate = new CalendarDateDto
                {
                    Date = currentDate.ToString("yyyy-MM-dd"),
                    Status = dateStatus,
                    StatusDisplayName = GetStatusDisplayName(dateStatus),
                    RentalId = rentalForDate?.Id,
                    UserName = rentalForDate?.User?.Name,
                    UserEmail = rentalForDate?.User?.Email,
                    RentalStartDate = rentalForDate?.Period.StartDate,
                    RentalEndDate = rentalForDate?.Period.EndDate,
                    Notes = rentalForDate?.Notes
                };

                calendarDates.Add(calendarDate);
                currentDate = currentDate.AddDays(1);
            }

            // Create legend with enum values as string keys
            var legend = new Dictionary<string, string>
            {
                { ((int)CalendarDateStatus.Available).ToString(), "Available for rental" },
                { ((int)CalendarDateStatus.Reserved).ToString(), "Reserved (pending payment)" },
                { ((int)CalendarDateStatus.Occupied).ToString(), "Occupied (active rental)" },
                { ((int)CalendarDateStatus.Unavailable).ToString(), "Unavailable" },
                { ((int)CalendarDateStatus.PastDate).ToString(), "Past date" }
            };

            return new BoothCalendarResponseDto
            {
                BoothId = booth.Id,
                BoothNumber = booth.Number,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                Dates = calendarDates,
                Legend = legend
            };
        }

        private CalendarDateStatus GetDateStatus(DateTime date, List<Rental> rentals)
        {
            var today = DateTime.Today;

            // Past dates
            if (date < today)
                return CalendarDateStatus.PastDate;

            // Check if there's a rental for this date
            var rental = GetRentalForDate(date, rentals);
            if (rental != null)
            {
                switch (rental.Status)
                {
                    case RentalStatus.Draft:
                        return CalendarDateStatus.Reserved;
                    case RentalStatus.Active:
                    case RentalStatus.Extended:
                        return CalendarDateStatus.Occupied;
                    default:
                        return CalendarDateStatus.Available;
                }
            }

            return CalendarDateStatus.Available;
        }

        private Rental? GetRentalForDate(DateTime date, List<Rental> rentals)
        {
            return rentals.FirstOrDefault(r =>
                date >= r.Period.StartDate.Date &&
                date <= r.Period.EndDate.Date);
        }

        private string GetStatusDisplayName(CalendarDateStatus status)
        {
            return status switch
            {
                CalendarDateStatus.Available => "Available",
                CalendarDateStatus.Reserved => "Reserved",
                CalendarDateStatus.Occupied => "Occupied",
                CalendarDateStatus.Unavailable => "Unavailable",
                CalendarDateStatus.PastDate => "Past Date",
                _ => "Unknown"
            };
        }
    }
}