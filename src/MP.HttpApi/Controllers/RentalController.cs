using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MP.Rentals;
using MP.Application.Rentals;
using MP.Domain.Payments;
using MP.Domain.Payments.Events;
using MP.Application.Contracts.Payments;
using MP.Domain.Rentals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Domain.Repositories;

namespace MP.Controllers
{
    [RemoteService(Name = "Default")]
    [Area("app")]
    [Route("api/app/rentals")]
    public class RentalController : AbpControllerBase, IRentalAppService
    {
        private readonly IRentalAppService _rentalAppService;
        private readonly RentalPaymentService _paymentService;
        private readonly IP24TransactionRepository _p24TransactionRepository;
        private readonly ILocalEventBus _localEventBus;
        private readonly IRepository<Rental, Guid> _rentalRepository;

        public RentalController(
            IRentalAppService rentalAppService,
            RentalPaymentService paymentService,
            IP24TransactionRepository p24TransactionRepository,
            ILocalEventBus localEventBus,
            IRepository<Rental, Guid> rentalRepository)
        {
            _rentalAppService = rentalAppService;
            _paymentService = paymentService;
            _p24TransactionRepository = p24TransactionRepository;
            _localEventBus = localEventBus;
            _rentalRepository = rentalRepository;
        }

        [HttpGet]
        [Route("{id}")]
        public Task<RentalDto> GetAsync(Guid id)
        {
            return _rentalAppService.GetAsync(id);
        }

        [HttpGet]
        public Task<PagedResultDto<RentalListDto>> GetListAsync(GetRentalListDto input)
        {
            return _rentalAppService.GetListAsync(input);
        }

        [HttpPost]
        public Task<RentalDto> CreateAsync(CreateRentalDto input)
        {
            return _rentalAppService.CreateAsync(input);
        }

        [HttpPut]
        [Route("{id}")]
        public Task<RentalDto> UpdateAsync(Guid id, UpdateRentalDto input)
        {
            return _rentalAppService.UpdateAsync(id, input);
        }

        [HttpDelete]
        [Route("{id}")]
        public Task DeleteAsync(Guid id)
        {
            return _rentalAppService.DeleteAsync(id);
        }

        [HttpPost]
        [Route("{id}/pay")]
        public Task<RentalDto> PayAsync(Guid id, PaymentDto input)
        {
            return _rentalAppService.PayAsync(id, input);
        }

        [HttpPost]
        [Route("{id}/start")]
        public Task<RentalDto> StartRentalAsync(Guid id)
        {
            return _rentalAppService.StartRentalAsync(id);
        }

        [HttpPost]
        [Route("{id}/complete")]
        public Task<RentalDto> CompleteRentalAsync(Guid id)
        {
            return _rentalAppService.CompleteRentalAsync(id);
        }

        [HttpPost]
        [Route("{id}/cancel")]
        public Task<RentalDto> CancelRentalAsync(Guid id, [FromBody] string reason)
        {
            return _rentalAppService.CancelRentalAsync(id, reason);
        }

        [HttpPost]
        [Route("{id}/extend")]
        public Task<RentalDto> ExtendRentalAsync(Guid id, ExtendRentalDto input)
        {
            return _rentalAppService.ExtendRentalAsync(id, input);
        }

        [HttpGet]
        [Route("my-rentals")]
        public Task<PagedResultDto<RentalListDto>> GetMyRentalsAsync(GetRentalListDto input)
        {
            return _rentalAppService.GetMyRentalsAsync(input);
        }

        [HttpPost]
        [Route("my-rental")]
        public Task<RentalDto> CreateMyRentalAsync(CreateMyRentalDto input)
        {
            return _rentalAppService.CreateMyRentalAsync(input);
        }

        [HttpPost]
        [Route("create-with-payment")]
        public Task<CreateRentalWithPaymentResultDto> CreateMyRentalWithPaymentAsync(CreateRentalWithPaymentDto input)
        {
            return _rentalAppService.CreateMyRentalWithPaymentAsync(input);
        }

        [HttpGet]
        [Route("active")]
        public Task<List<RentalListDto>> GetActiveRentalsAsync()
        {
            return _rentalAppService.GetActiveRentalsAsync();
        }

        [HttpGet]
        [Route("expired")]
        public Task<List<RentalListDto>> GetExpiredRentalsAsync()
        {
            return _rentalAppService.GetExpiredRentalsAsync();
        }

        [HttpGet]
        [Route("overdue")]
        public Task<List<RentalListDto>> GetOverdueRentalsAsync()
        {
            return _rentalAppService.GetOverdueRentalsAsync();
        }

        [HttpGet]
        [Route("active-for-booth/{boothId}")]
        public Task<RentalDto?> GetActiveRentalForBoothAsync(Guid boothId)
        {
            return _rentalAppService.GetActiveRentalForBoothAsync(boothId);
        }

        [HttpPost]
        [Route("booth-calendar")]
        public Task<BoothCalendarResponseDto> GetBoothCalendarAsync(BoothCalendarRequestDto input)
        {
            return _rentalAppService.GetBoothCalendarAsync(input);
        }

        [HttpGet]
        [Route("check-availability")]
        public Task<bool> CheckAvailabilityAsync(Guid boothId, DateTime startDate, DateTime endDate)
        {
            return _rentalAppService.CheckAvailabilityAsync(boothId, startDate, endDate);
        }

        [HttpGet]
        [Route("calculate-cost")]
        public Task<decimal> CalculateCostAsync(Guid boothId, Guid boothTypeId, DateTime startDate, DateTime endDate)
        {
            return _rentalAppService.CalculateCostAsync(boothId, boothTypeId, startDate, endDate);
        }

        [HttpGet]
        [Route("max-extension-date")]
        public Task<MaxExtensionDateResponseDto> GetMaxExtensionDateAsync(Guid boothId, DateTime currentRentalEndDate)
        {
            return _rentalAppService.GetMaxExtensionDateAsync(boothId, currentRentalEndDate);
        }

        [HttpPost]
        [Route("admin-manage")]
        public Task<RentalDto> AdminManageBoothRentalAsync(AdminManageBoothRentalDto input)
        {
            return _rentalAppService.AdminManageBoothRentalAsync(input);
        }

        // Nowe endpointy dla płatności
        [HttpPost]
        [Route("{id}/initiate-payment")]
        public async Task<string> InitiatePaymentAsync(Guid id)
        {
            return await _paymentService.InitiatePaymentAsync(id);
        }

        [HttpPost]
        [Route("payment/notification")]
        [AllowAnonymous] // Przelewy24 wywołuje bez autoryzacji
        public async Task<IActionResult> PaymentNotificationAsync([FromForm] P24NotificationDto notification)
        {
            try
            {
                // Find P24Transaction by session ID
                var transaction = await _p24TransactionRepository.FindBySessionIdAsync(notification.P24_session_id);
                if (transaction == null)
                {
                    return BadRequest("Transaction not found");
                }

                // Update transaction status
                transaction.SetStatus("completed");
                transaction.SetVerified(true);
                transaction.OrderId = notification.P24_order_id;
                await _p24TransactionRepository.UpdateAsync(transaction);

                // Update rental payment status (handles both single rental and cart checkout)
                // HandlePaymentCallbackAsync uses SessionId to find all associated rentals
                var success = await _paymentService.HandlePaymentCallbackAsync(notification.P24_session_id, true);

                // Publish PaymentCompletedEvent for immediate notification (don't wait for Hangfire)
                if (success)
                {
                    try
                    {
                        // Get all rentals associated with this transaction
                        var rentals = await _rentalRepository.GetListAsync(r =>
                            r.Payment.Przelewy24TransactionId == notification.P24_session_id,
                            includeDetails: false);

                        if (rentals.Any())
                        {
                            var firstRental = rentals.First();
                            var amountInPln = notification.P24_amount / 100m; // Convert from groszy to PLN

                            // Publish PaymentCompletedEvent
                            await _localEventBus.PublishAsync(new PaymentCompletedEvent
                            {
                                UserId = firstRental.UserId,
                                TransactionId = notification.P24_session_id,
                                Amount = amountInPln,
                                Currency = notification.P24_currency,
                                RentalIds = rentals.Select(r => r.Id).ToList(),
                                CompletedAt = DateTime.UtcNow,
                                PaymentMethod = "Przelewy24"
                            });
                        }
                    }
                    catch
                    {
                        // Log the error but don't fail the whole request
                        // The event will be published again by Hangfire job if needed
                    }
                }

                return Ok("OK");
            }
            catch
            {
                // Log error but return OK to Przelewy24
                return Ok("OK");
            }
        }

        [HttpGet]
        [Route("{id}/payment-status")]
        public async Task<PaymentStatus> GetPaymentStatusAsync(Guid id)
        {
            return await _paymentService.GetPaymentStatusAsync(id);
        }
    }
}