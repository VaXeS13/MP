using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;
using MP.Application.Contracts.Payments;
using MP.Application.Payments;

namespace MP.Controllers
{
    [Route("api/app/payments")]
    public class PaymentController : AbpControllerBase
    {
        private readonly IPaymentProviderAppService _paymentProviderService;
        private readonly StripeWebhookHandler _stripeWebhookHandler;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentProviderAppService paymentProviderService,
            StripeWebhookHandler stripeWebhookHandler,
            ILogger<PaymentController> logger)
        {
            _paymentProviderService = paymentProviderService;
            _stripeWebhookHandler = stripeWebhookHandler;
            _logger = logger;
        }

        [HttpGet("providers")]
        public async Task<List<PaymentProviderDto>> GetPaymentProvidersAsync()
        {
            return await _paymentProviderService.GetAvailableProvidersAsync();
        }

        [HttpGet("providers/{providerId}/methods")]
        public async Task<List<PaymentMethodDto>> GetPaymentMethodsAsync(string providerId, [FromQuery] string currency = "PLN")
        {
            return await _paymentProviderService.GetPaymentMethodsAsync(providerId, currency);
        }

        [HttpPost("create")]
        public async Task<PaymentCreationResultDto> CreatePaymentAsync(CreatePaymentRequestDto request)
        {
            return await _paymentProviderService.CreatePaymentAsync(request);
        }

        /// <summary>
        /// Stripe webhook endpoint for payment notifications
        /// Endpoint: POST /api/app/payments/stripe/webhook
        /// </summary>
        [HttpPost("stripe/webhook")]
        public async Task<IActionResult> StripeWebhookAsync()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

                _logger.LogInformation("StripeWebhook: Received webhook request");

                var result = await _stripeWebhookHandler.HandleWebhookAsync(json, stripeSignature);

                if (result)
                {
                    return Ok();
                }
                else
                {
                    return BadRequest("Webhook processing failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StripeWebhook: Error processing webhook");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}