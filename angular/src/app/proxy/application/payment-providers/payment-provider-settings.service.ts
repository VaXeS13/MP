import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { PaymentProviderSettingsDto, UpdatePaymentProviderSettingsDto } from '../contracts/payment-providers/models';

@Injectable({
  providedIn: 'root',
})
export class PaymentProviderSettingsService {
  apiName = 'Default';
  

  get = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentProviderSettingsDto>({
      method: 'GET',
      url: '/api/app/payment-provider-settings',
    },
    { apiName: this.apiName,...config });
  

  update = (input: UpdatePaymentProviderSettingsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'PUT',
      url: '/api/app/payment-provider-settings',
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
