using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MP.Rentals;
using MP.Application.Rentals;
using MP.Domain.Payments;
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

        public RentalController(
            IRentalAppService rentalAppService,
            RentalPaymentService paymentService,
            IP24TransactionRepository p24TransactionRepository)
        {
            _rentalAppService = rentalAppService;
            _paymentService = paymentService;
            _p24TransactionRepository = p24TransactionRepository;
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

                return Ok("OK");
            }
            catch (Exception ex)
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