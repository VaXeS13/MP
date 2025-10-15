using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;
using MP.Application.Payments;
using MP.Payments;

namespace MP.Controllers
{
    [ApiController]
    [Area("app")]
    [Route("api/app/payment-transactions")]
    public class PaymentTransactionsController : AbpControllerBase
    {
        private readonly IPaymentTransactionAppService _paymentTransactionAppService;

        public PaymentTransactionsController(IPaymentTransactionAppService paymentTransactionAppService)
        {
            _paymentTransactionAppService = paymentTransactionAppService;
        }

        [HttpGet]
        public async Task<PagedResultDto<PaymentTransactionDto>> GetListAsync(GetPaymentTransactionListDto input)
        {
            return await _paymentTransactionAppService.GetListAsync(input);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<PaymentTransactionDto> GetAsync(Guid id)
        {
            return await _paymentTransactionAppService.GetAsync(id);
        }

        [HttpPost]
        public async Task<PaymentTransactionDto> CreateAsync(CreatePaymentTransactionDto input)
        {
            return await _paymentTransactionAppService.CreateAsync(input);
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<PaymentTransactionDto> UpdateAsync(Guid id, UpdatePaymentTransactionDto input)
        {
            return await _paymentTransactionAppService.UpdateAsync(id, input);
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task DeleteAsync(Guid id)
        {
            await _paymentTransactionAppService.DeleteAsync(id);
        }

        [HttpGet]
        [Route("payment-success/{sessionId}")]
        [AllowAnonymous]
        public async Task<PaymentSuccessViewModel> GetPaymentSuccessViewModelAsync(string sessionId)
        {
            return await _paymentTransactionAppService.GetPaymentSuccessViewModelAsync(sessionId);
        }
    }
}