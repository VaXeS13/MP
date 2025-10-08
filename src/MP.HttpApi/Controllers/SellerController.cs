using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using MP.Application.Contracts.Sellers;

namespace MP.HttpApi.Controllers
{
    [Route("api/app/seller")]
    public class SellerController : AbpControllerBase
    {
        private readonly IItemCheckoutAppService _checkoutAppService;

        public SellerController(IItemCheckoutAppService checkoutAppService)
        {
            _checkoutAppService = checkoutAppService;
        }

        [HttpPost("find-by-barcode")]
        public virtual Task<ItemForCheckoutDto?> FindItemByBarcodeAsync(FindItemByBarcodeDto input)
        {
            return _checkoutAppService.FindItemByBarcodeAsync(input);
        }

        [HttpGet("payment-methods")]
        public virtual Task<AvailablePaymentMethodsDto> GetAvailablePaymentMethodsAsync()
        {
            return _checkoutAppService.GetAvailablePaymentMethodsAsync();
        }

        [HttpPost("checkout")]
        public virtual Task<CheckoutResultDto> CheckoutItemAsync(CheckoutItemDto input)
        {
            return _checkoutAppService.CheckoutItemAsync(input);
        }

        [HttpGet("terminal-status")]
        public virtual Task<bool> CheckTerminalStatusAsync()
        {
            return _checkoutAppService.CheckTerminalStatusAsync();
        }
    }
}