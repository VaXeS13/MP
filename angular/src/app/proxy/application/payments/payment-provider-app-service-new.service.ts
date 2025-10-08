import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { CreatePaymentRequestDto, PaymentCreationResultDto, PaymentMethodDto, PaymentProviderDto } from '../contracts/payments/models';

@Injectable({
  providedIn: 'root',
})
export class PaymentProviderAppServiceNewService {
  apiName = 'Default';
  

  createPayment = (request: CreatePaymentRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentCreationResultDto>({
      method: 'POST',
      url: '/api/app/payment-provider-app-service-new/payment',
      body: request,
    },
    { apiName: this.apiName,...config });
  

  getAvailableProviders = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentProviderDto[]>({
      method: 'GET',
      url: '/api/app/payment-provider-app-service-new/available-providers',
    },
    { apiName: this.apiName,...config });
  

  getPaymentMethods = (providerId: string, currency: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentMethodDto[]>({
      method: 'GET',
      url: `/api/app/payment-provider-app-service-new/payment-methods/${providerId}`,
      params: { currency },
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
