import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { AvailablePaymentMethodsDto, CheckoutItemDto, CheckoutResultDto, FindItemByBarcodeDto, ItemForCheckoutDto } from '../contracts/sellers/models';

@Injectable({
  providedIn: 'root',
})
export class ItemCheckoutService {
  apiName = 'Default';
  

  checkTerminalStatus = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, boolean>({
      method: 'POST',
      url: '/api/app/item-checkout/check-terminal-status',
    },
    { apiName: this.apiName,...config });
  

  checkoutItem = (input: CheckoutItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CheckoutResultDto>({
      method: 'POST',
      url: '/api/app/item-checkout/checkout-item',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  findItemByBarcode = (input: FindItemByBarcodeDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ItemForCheckoutDto>({
      method: 'POST',
      url: '/api/app/item-checkout/find-item-by-barcode',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  getAvailablePaymentMethods = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, AvailablePaymentMethodsDto>({
      method: 'GET',
      url: '/api/app/item-checkout/available-payment-methods',
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
