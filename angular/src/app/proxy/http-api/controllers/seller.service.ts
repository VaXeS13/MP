import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { AvailablePaymentMethodsDto, CheckoutItemDto, CheckoutResultDto, FindItemByBarcodeDto, ItemForCheckoutDto } from '../../application/contracts/sellers/models';

@Injectable({
  providedIn: 'root',
})
export class SellerService {
  apiName = 'Default';
  

  checkTerminalStatus = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, boolean>({
      method: 'GET',
      url: '/api/app/seller/terminal-status',
    },
    { apiName: this.apiName,...config });
  

  checkoutItem = (input: CheckoutItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CheckoutResultDto>({
      method: 'POST',
      url: '/api/app/seller/checkout',
      params: { itemSheetItemId: input.itemSheetItemId, paymentMethod: input.paymentMethod, amount: input.amount },
    },
    { apiName: this.apiName,...config });
  

  findItemByBarcode = (input: FindItemByBarcodeDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ItemForCheckoutDto>({
      method: 'POST',
      url: '/api/app/seller/find-by-barcode',
      params: { barcode: input.barcode },
    },
    { apiName: this.apiName,...config });
  

  getAvailablePaymentMethods = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, AvailablePaymentMethodsDto>({
      method: 'GET',
      url: '/api/app/seller/payment-methods',
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
