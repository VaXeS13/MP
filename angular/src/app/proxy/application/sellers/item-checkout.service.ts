import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { AvailablePaymentMethodsDto, CheckoutItemDto, CheckoutItemsDto, CheckoutResultDto, CheckoutSummaryDto, FindItemByBarcodeDto, ItemForCheckoutDto } from '../contracts/sellers/models';

@Injectable({
  providedIn: 'root',
})
export class ItemCheckoutService {
  apiName = 'Default';
  

  calculateCheckoutSummary = (itemIds: string[], config?: Partial<Rest.Config>) =>
    this.restService.request<any, CheckoutSummaryDto>({
      method: 'POST',
      url: '/api/app/item-checkout/calculate-checkout-summary',
      body: itemIds,
    },
    { apiName: this.apiName,...config });
  

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
  

  checkoutItems = (input: CheckoutItemsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CheckoutResultDto>({
      method: 'POST',
      url: '/api/app/item-checkout/checkout-items',
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
