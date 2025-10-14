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
        private readonly RentalExtensionHandler _extensionHandler;

        public RentalAppService(
            IRentalRepository rentalRepository,
            IBoothRepository boothRepository,
            RentalManager rentalManager,
            IIdentityUserRepository userRepository,
            ICartRepository cartRepository,
            ISignalRNotificationService signalRNotificationService,
            RentalExtensionHandler extensionHandler)
        {
            _rentalRepository = rentalRepository;
            _boothRepository = boothRepository;
            _rentalManager = rentalManager;
            _userRepository = userRepository;
            _cartRepository = cartRepository;
            _signalRNotificationService = signalRNotificationService;
            _extensionHandler = extensionHandler;
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
            queryable = queryable.AsNoTracking();

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

            // Use projection to load only required fields instead of full User and Booth entities
            var dtos = await AsyncExecuter.ToListAsync(
                queryable
                    .Skip(input.SkipCount)
                    .Take(input.MaxResultCount)
                    .Select(r => new RentalListDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserName = r.User.Name + " " + r.User.Surname,
                        UserEmail = r.User.Email,
                        BoothId = r.BoothId,
                        BoothNumber = r.Booth.Number,
                        StartDate = r.Period.StartDate,
                        EndDate = r.Period.EndDate,
                        DaysCount = (r.Period.EndDate - r.Period.StartDate).Days + 1,
                        Status = r.Status,
                        StatusDisplayName = "",
                        TotalAmount = r.Payment.TotalAmount,
                        PaidAmount = r.Payment.PaidAmount,
                        IsPaid = r.Payment.IsPaid,
                        CreationTime = r.CreationTime,
                        StartedAt = r.StartedAt,
                        ItemsCount = r.ItemSheets.Sum(sheet => sheet.Items.Count),
                        SoldItemsCount = r.ItemSheets.Sum(sheet => sheet.Items.Count(item => item.Status == MP.Domain.Items.ItemSheetItemStatus.Sold))
                    }));

            // Set display names in memory after loading from database
            foreach (var dto in dtos)
            {
                dto.StatusDisplayName = GetRentalStatusDisplayName(dto.Status);
            }

            return new PagedResultDto<RentalListDto>(totalCount, dtos);
        }

        private static string GetRentalStatusDisplayName(RentalStatus status)
        {
            return status switch
            {
                RentalStatus.Draft => "Projekt",
                RentalStatus.Active => "Aktywne",
                RentalStatus.Extended => "Przedłużone",
                RentalStatus.Expired => "Wygasłe",
                RentalStatus.Cancelled => "Anulowane",
                _ => status.ToString()
            };
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

        [Authorize(MPPermissions.Rentals.Extend)]
        public async Task<RentalDto> ExtendRentalAsync(Guid id, ExtendRentalDto input)
        {
            var rental = await _rentalRepository.GetRentalWithItemsAsync(id);
            if (rental == null)
                throw new EntityNotFoundException(typeof(Rental), id);

            // Validation
            if (!rental.IsActive())
                throw new BusinessException("CAN_ONLY_EXTEND_ACTIVE_RENTAL");

            if (input.NewEndDate <= rental.Period.EndDate)
                throw new BusinessException("NEW_END_DATE_MUST_BE_LATER");

            // Calculate cost
            var booth = await _boothRepository.GetAsync(rental.BoothId);
            var additionalDays = (input.NewEndDate - rental.Period.EndDate).Days;
            var additionalCost = additionalDays * booth.PricePerDay;

            // Handle based on payment type
            switch (input.PaymentType)
            {
                case ExtensionPaymentType.Free:
                    await _extensionHandler.HandleFreeExtensionAsync(rental, input.NewEndDate);
                    break;

                case ExtensionPaymentType.Cash:
                    await _extensionHandler.HandleCashExtensionAsync(rental, input.NewEndDate, additionalCost);
                    break;

                case ExtensionPaymentType.Terminal:
                    await _extensionHandler.HandleTerminalExtensionAsync(
                        rental,
                        input.NewEndDate,
                        additionalCost,
                        input.TerminalTransactionId,
                        input.TerminalReceiptNumber);
                    break;

                case ExtensionPaymentType.Online:
                    await _extensionHandler.HandleOnlineExtensionAsync(
                        rental,
                        input.NewEndDate,
                        additionalCost,
                        input.OnlineTimeoutMinutes);
                    break;

                default:
                    throw new BusinessException("INVALID_EXTENSION_PAYMENT_TYPE");
            }

            var updatedRental = await _rentalRepository.GetRentalWithItemsAsync(rental.Id);
            return ObjectMapper.Map<Rental, RentalDto>(updatedRental!);
        }

        [Authorize(MPPermissions.Rentals.Extend)]
        public async Task<RentalDto?> GetActiveRentalForBoothAsync(Guid boothId)
        {
            var today = DateTime.Today;
            var queryable = await _rentalRepository.GetQueryableAsync();

            var rental = await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Where(r =>
                    r.BoothId == boothId &&
                    (r.Status == RentalStatus.Active || r.Status == RentalStatus.Extended) &&
                    r.Period.StartDate <= today &&
                    r.Period.EndDate >= today
                )
            );

            if (rental == null)
                return null;

            var rentalWithItems = await _rentalRepository.GetRentalWithItemsAsync(rental.Id);
            return ObjectMapper.Map<Rental, RentalDto>(rentalWithItems!);
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
                    Currency = rental.Currency.ToString(),
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
            // Include historical rentals (Completed, Cancelled) to show them in greyed out state
            // Use projection to load only required fields instead of full User entity
            var queryable = await _rentalRepository.GetQueryableAsync();
            var rentals = await AsyncExecuter.ToListAsync(
                queryable
                    .AsNoTracking()
                    .Where(r => r.BoothId == input.BoothId &&
                               r.Period.StartDate <= input.EndDate &&
                               r.Period.EndDate >= input.StartDate)
                    .Select(r => new RentalCalendarProjection
                    {
                        Id = r.Id,
                        StartDate = r.Period.StartDate,
                        EndDate = r.Period.EndDate,
                        Status = r.Status,
                        UserName = r.User.Name,
                        UserEmail = r.User.Email,
                        Notes = r.Notes
                    })
            );

            Logger.LogInformation("GetBoothCalendar: Found {RentalCount} rentals for booth {BoothId} between {StartDate:yyyy-MM-dd} and {EndDate:yyyy-MM-dd}",
                rentals.Count, input.BoothId, input.StartDate, input.EndDate);
            foreach (var rental in rentals)
            {
                Logger.LogInformation("  Rental {RentalId}: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}, Status: {Status}",
                    rental.Id, rental.StartDate, rental.EndDate, rental.Status);
            }

            // Get all active cart items for this booth that overlap with the requested date range
            // IMPORTANT: Only include items with active (non-expired) reservations
            var now = DateTime.Now;
            var cartQueryable = await _cartRepository.GetQueryableAsync();
            var cartItems = await AsyncExecuter.ToListAsync(
                cartQueryable
                    .AsNoTracking()
                    .Where(c => c.Status == CartStatus.Active)
                    .SelectMany(c => c.Items)
                    .Where(item => item.BoothId == input.BoothId &&
                                  item.StartDate <= input.EndDate &&
                                  item.EndDate >= input.StartDate &&
                                  item.ReservationExpiresAt.HasValue &&
                                  item.ReservationExpiresAt.Value >= now &&  // Only active reservations
                                  (!input.ExcludeCartId.HasValue || item.CartId != input.ExcludeCartId.Value))
                    .Select(item => new CartItemCalendarProjection
                    {
                        Id = item.Id,
                        CartId = item.CartId,
                        StartDate = item.StartDate,
                        EndDate = item.EndDate,
                        UserName = item.Cart.User.Name,
                        UserEmail = item.Cart.User.Email,
                        Notes = item.Notes,
                        ReservationExpiresAt = item.ReservationExpiresAt
                    })
            );

            Logger.LogInformation("GetBoothCalendar: Found {CartItemCount} cart items for booth {BoothId} between {StartDate:yyyy-MM-dd} and {EndDate:yyyy-MM-dd}",
                cartItems.Count, input.BoothId, input.StartDate, input.EndDate);
            foreach (var cartItem in cartItems)
            {
                Logger.LogInformation("  CartItem {CartItemId}: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}, CartId: {CartId}",
                    cartItem.Id, cartItem.StartDate, cartItem.EndDate, cartItem.CartId);
            }

            // Generate calendar dates
            var calendarDates = new List<CalendarDateDto>();
            var currentDate = input.StartDate.Date;

            while (currentDate <= input.EndDate.Date)
            {
                var dateStatus = GetDateStatus(currentDate, rentals, cartItems);
                var rentalForDate = GetRentalForDate(currentDate, rentals);
                var cartItemForDate = GetCartItemForDate(currentDate, cartItems);

                // Prefer rental over cart item if both exist (rental is confirmed)
                var calendarDate = new CalendarDateDto
                {
                    Date = currentDate.ToString("yyyy-MM-dd"),
                    Status = dateStatus,
                    StatusDisplayName = GetStatusDisplayName(dateStatus),
                    RentalId = rentalForDate?.Id,
                    UserName = rentalForDate?.UserName ?? cartItemForDate?.UserName,
                    UserEmail = rentalForDate?.UserEmail ?? cartItemForDate?.UserEmail,
                    RentalStartDate = rentalForDate?.StartDate ?? cartItemForDate?.StartDate,
                    RentalEndDate = rentalForDate?.EndDate ?? cartItemForDate?.EndDate,
                    Notes = rentalForDate?.Notes ?? cartItemForDate?.Notes
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
                { ((int)CalendarDateStatus.PastDate).ToString(), "Past date" },
                { ((int)CalendarDateStatus.Historical).ToString(), "Historical rental (past)" }
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

        private CalendarDateStatus GetDateStatus(DateTime date, List<RentalCalendarProjection> rentals, List<CartItemCalendarProjection> cartItems)
        {
            var today = DateTime.Today;

            // Check if there's a rental for this date (priority over cart items)
            var rental = GetRentalForDate(date, rentals);
            if (rental != null)
            {
                switch (rental.Status)
                {
                    case RentalStatus.Draft:
                        return CalendarDateStatus.Reserved;
                    case RentalStatus.Active:
                    case RentalStatus.Extended:
                        // If rental ended in the past, show as historical
                        if (rental.EndDate < today)
                            return CalendarDateStatus.Historical;
                        return CalendarDateStatus.Occupied;
                    case RentalStatus.Expired:
                    case RentalStatus.Cancelled:
                        return CalendarDateStatus.Historical;
                    default:
                        return CalendarDateStatus.Available;
                }
            }

            // Check if there's a cart item for this date
            var cartItem = GetCartItemForDate(date, cartItems);
            if (cartItem != null)
            {
                // Cart items are always shown as Reserved (pending payment)
                return CalendarDateStatus.Reserved;
            }

            // Past dates without rentals or cart items
            if (date < today)
                return CalendarDateStatus.PastDate;

            return CalendarDateStatus.Available;
        }

        private RentalCalendarProjection? GetRentalForDate(DateTime date, List<RentalCalendarProjection> rentals)
        {
            return rentals.FirstOrDefault(r =>
                date >= r.StartDate.Date &&
                date <= r.EndDate.Date);
        }

        private CartItemCalendarProjection? GetCartItemForDate(DateTime date, List<CartItemCalendarProjection> cartItems)
        {
            return cartItems.FirstOrDefault(item =>
                date >= item.StartDate.Date &&
                date <= item.EndDate.Date);
        }

        // Projection class for calendar queries - loads only required fields
        private class RentalCalendarProjection
        {
            public Guid Id { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public RentalStatus Status { get; set; }
            public string? UserName { get; set; }
            public string? UserEmail { get; set; }
            public string? Notes { get; set; }
        }

        // Projection class for cart item calendar queries
        private class CartItemCalendarProjection
        {
            public Guid Id { get; set; }
            public Guid CartId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string? UserName { get; set; }
            public string? UserEmail { get; set; }
            public string? Notes { get; set; }
            public DateTime? ReservationExpiresAt { get; set; }
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
                CalendarDateStatus.Historical => "Historical Rental",
                _ => "Unknown"
            };
        }

        public async Task<bool> CheckAvailabilityAsync(Guid boothId, DateTime startDate, DateTime endDate)
        {
            // Check if booth exists
            var booth = await _boothRepository.GetAsync(boothId);

            // Check if booth is in maintenance
            if (booth.Status == BoothStatus.Maintenance)
                return false;

            // Check if there are any active rentals for this booth in the given period
            var hasConflict = await _rentalRepository.HasActiveRentalForBoothAsync(
                boothId, startDate, endDate);

            // Return true if available (no conflicts), false otherwise
            return !hasConflict;
        }

        public async Task<decimal> CalculateCostAsync(Guid boothId, Guid boothTypeId, DateTime startDate, DateTime endDate)
        {
            // Create rental period
            var period = new RentalPeriod(startDate, endDate);

            // Use RentalManager to calculate cost
            var cost = await _rentalManager.CalculateRentalCostAsync(boothId, boothTypeId, period);

            return cost;
        }

        [Authorize(MPPermissions.Rentals.Manage)]
        public async Task<MaxExtensionDateResponseDto> GetMaxExtensionDateAsync(Guid boothId, DateTime currentRentalEndDate)
        {
            // Find the nearest rental after the current rental end date
            var nextRental = await _rentalRepository.GetNearestRentalAfterAsync(boothId, currentRentalEndDate);

            if (nextRental == null)
            {
                // No blocking rental - can extend indefinitely
                return new MaxExtensionDateResponseDto
                {
                    MaxExtensionDate = null,
                    HasBlockingRental = false,
                    Message = "No upcoming rentals - can extend indefinitely"
                };
            }

            // Calculate max extension date (day before next rental starts)
            var maxDate = nextRental.Period.StartDate.AddDays(-1);

            return new MaxExtensionDateResponseDto
            {
                MaxExtensionDate = maxDate,
                HasBlockingRental = true,
                NextRentalId = nextRental.Id,
                NextRentalStartDate = nextRental.Period.StartDate,
                Message = $"Can extend until {maxDate:yyyy-MM-dd} (next rental starts {nextRental.Period.StartDate:yyyy-MM-dd})"
            };
        }

        [Authorize(MPPermissions.Rentals.Manage)]
        public async Task<RentalDto> AdminManageBoothRentalAsync(AdminManageBoothRentalDto input)
        {
            if (input.IsExtension)
            {
                return await HandleRentalExtensionAsync(input);
            }
            else
            {
                return await HandleNewRentalAsync(input);
            }
        }

        private async Task<RentalDto> HandleNewRentalAsync(AdminManageBoothRentalDto input)
        {
            // Validate required fields for new rental
            if (!input.UserId.HasValue)
                throw new BusinessException("USER_ID_REQUIRED_FOR_NEW_RENTAL");

            if (!input.BoothTypeId.HasValue)
                throw new BusinessException("BOOTH_TYPE_ID_REQUIRED_FOR_NEW_RENTAL");

            // Validate gap rules
            await _rentalManager.ValidateGapRulesAsync(
                input.BoothId,
                input.StartDate,
                input.EndDate);

            // Get booth for price calculation
            var booth = await _boothRepository.GetAsync(input.BoothId);

            // Create rental through RentalManager
            var rental = await _rentalManager.CreateRentalAsync(
                input.UserId.Value,
                input.BoothId,
                input.BoothTypeId.Value,
                input.StartDate,
                input.EndDate,
                booth.PricePerDay);

            rental.SetNotes(input.Notes);

            // Apply payment method
            await ApplyPaymentMethodAsync(rental, input.PaymentMethod, input, booth);

            // Save rental
            await _rentalRepository.InsertAsync(rental);

            // Update booth status
            await _boothRepository.UpdateAsync(booth);

            // Send SignalR notification
            await _signalRNotificationService.SendBoothStatusUpdateAsync(
                CurrentTenant.Id,
                booth.Id,
                booth.Status.ToString(),
                !booth.IsAvailable(),
                rental.Id,
                rental.Period.EndDate);

            var savedRental = await _rentalRepository.GetRentalWithItemsAsync(rental.Id);
            return ObjectMapper.Map<Rental, RentalDto>(savedRental!);
        }

        private async Task<RentalDto> HandleRentalExtensionAsync(AdminManageBoothRentalDto input)
        {
            // Validate required fields for extension
            if (!input.ExistingRentalId.HasValue)
                throw new BusinessException("EXISTING_RENTAL_ID_REQUIRED_FOR_EXTENSION");

            var rental = await _rentalRepository.GetRentalWithItemsAsync(input.ExistingRentalId.Value);
            if (rental == null)
                throw new EntityNotFoundException(typeof(Rental), input.ExistingRentalId.Value);

            // Validate extension is possible
            if (!rental.IsActive())
                throw new BusinessException("CAN_ONLY_EXTEND_ACTIVE_RENTAL");

            if (input.EndDate <= rental.Period.EndDate)
                throw new BusinessException("EXTENSION_MUST_INCREASE_END_DATE");

            // Validate no conflicts
            await _rentalManager.ValidateExtensionAsync(rental, input.EndDate);

            // Calculate cost
            var booth = await _boothRepository.GetAsync(rental.BoothId);
            var additionalDays = (input.EndDate - rental.Period.EndDate).Days;
            var additionalCost = additionalDays * booth.PricePerDay;

            // Convert RentalPaymentMethod to ExtensionPaymentType
            var extensionPaymentType = ConvertToExtensionPaymentType(input.PaymentMethod);

            // Handle extension based on payment type
            switch (extensionPaymentType)
            {
                case ExtensionPaymentType.Free:
                    await _extensionHandler.HandleFreeExtensionAsync(rental, input.EndDate);
                    break;

                case ExtensionPaymentType.Cash:
                    await _extensionHandler.HandleCashExtensionAsync(rental, input.EndDate, additionalCost);
                    break;

                case ExtensionPaymentType.Terminal:
                    if (string.IsNullOrWhiteSpace(input.TerminalTransactionId))
                        throw new BusinessException("TERMINAL_TRANSACTION_ID_REQUIRED");

                    await _extensionHandler.HandleTerminalExtensionAsync(
                        rental,
                        input.EndDate,
                        additionalCost,
                        input.TerminalTransactionId,
                        input.TerminalReceiptNumber);
                    break;

                case ExtensionPaymentType.Online:
                    await _extensionHandler.HandleOnlineExtensionAsync(
                        rental,
                        input.EndDate,
                        additionalCost,
                        input.OnlineTimeoutMinutes);
                    break;

                default:
                    throw new BusinessException("INVALID_PAYMENT_METHOD");
            }

            var updatedRental = await _rentalRepository.GetRentalWithItemsAsync(rental.Id);
            return ObjectMapper.Map<Rental, RentalDto>(updatedRental!);
        }

        private async Task ApplyPaymentMethodAsync(
            Rental rental,
            RentalPaymentMethod paymentMethod,
            AdminManageBoothRentalDto input,
            Booth booth)
        {
            var totalAmount = rental.Payment.TotalAmount;

            switch (paymentMethod)
            {
                case RentalPaymentMethod.Free:
                    // Free rental - mark as paid with 0 amount
                    rental.MarkAsPaid(0, DateTime.Now, "FREE_RENTAL");
                    break;

                case RentalPaymentMethod.Cash:
                    // Cash payment - mark as paid immediately
                    rental.MarkAsPaid(totalAmount, DateTime.Now, "CASH_PAYMENT");
                    break;

                case RentalPaymentMethod.Terminal:
                    // Terminal payment - mark as paid with transaction ID
                    if (string.IsNullOrWhiteSpace(input.TerminalTransactionId))
                        throw new BusinessException("TERMINAL_TRANSACTION_ID_REQUIRED");

                    rental.MarkAsPaid(totalAmount, DateTime.Now, input.TerminalTransactionId);
                    rental.Payment.SetTerminalDetails(input.TerminalTransactionId, input.TerminalReceiptNumber);
                    break;

                case RentalPaymentMethod.Online:
                    // Online payment - add to cart with reservation, rental stays in Draft status
                    var cart = await _cartRepository.GetActiveCartByUserIdAsync(rental.UserId, includeItems: true);
                    if (cart == null)
                    {
                        // Create cart using CartManager
                        var cartManager = LazyServiceProvider.LazyGetRequiredService<CartManager>();
                        cart = await cartManager.GetOrCreateCartAsync(rental.UserId);
                    }

                    // Set reservation timeout (configurable, default 30 minutes for admin-created)
                    var timeout = input.OnlineTimeoutMinutes ?? 30;
                    var reservationExpires = DateTime.Now.AddMinutes(timeout);

                    // Create CartItem for new rental - link to pre-created Rental with reservation
                    var cartItem = new CartItem(
                        GuidGenerator.Create(),
                        cart.Id,
                        rental.BoothId,
                        rental.BoothTypeId,
                        rental.Period.StartDate,
                        rental.Period.EndDate,
                        booth.PricePerDay,
                        rental.Currency,
                        CartItemType.Rental,
                        extendedRentalId: null,
                        rentalId: rental.Id,  // Link to pre-created Rental
                        reservationExpiresAt: reservationExpires,  // NEW: Reservation on CartItem level
                        notes: rental.Notes
                    );

                    cart.AddItem(cartItem);
                    await _cartRepository.UpdateAsync(cart);

                    // Note: User will see the cart item when they open their cart
                    // Could implement additional notification here if needed
                    break;

                default:
                    throw new BusinessException("INVALID_PAYMENT_METHOD");
            }
        }

        private ExtensionPaymentType ConvertToExtensionPaymentType(RentalPaymentMethod paymentMethod)
        {
            return paymentMethod switch
            {
                RentalPaymentMethod.Free => ExtensionPaymentType.Free,
                RentalPaymentMethod.Cash => ExtensionPaymentType.Cash,
                RentalPaymentMethod.Terminal => ExtensionPaymentType.Terminal,
                RentalPaymentMethod.Online => ExtensionPaymentType.Online,
                _ => throw new BusinessException("UNSUPPORTED_PAYMENT_METHOD")
            };
        }
    }
}