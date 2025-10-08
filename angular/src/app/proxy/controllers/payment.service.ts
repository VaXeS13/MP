import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { CreatePaymentRequestDto, PaymentCreationResultDto, PaymentMethodDto, PaymentProviderDto } from '../application/contracts/payments/models';
import type { IActionResult } from '../microsoft/asp-net-core/mvc/models';

@Injectable({
  providedIn: 'root',
})
export class PaymentService {
  apiName = 'Default';
  

  createPayment = (request: CreatePaymentRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentCreationResultDto>({
      method: 'POST',
      url: '/api/app/payments/create',
      params: { amount: request.amount, currency: request.currency, description: request.description, providerId: request.providerId, methodId: request.methodId, metadata: request.metadata },
    },
    { apiName: this.apiName,...config });
  

  getPaymentMethods = (providerId: string, currency: string = "PLN", config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentMethodDto[]>({
      method: 'GET',
      url: `/api/app/payments/providers/${providerId}/methods`,
      params: { currency },
    },
    { apiName: this.apiName,...config });
  

  getPaymentProviders = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentProviderDto[]>({
      method: 'GET',
      url: '/api/app/payments/providers',
    },
    { apiName: this.apiName,...config });
  

  stripeWebhook = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, IActionResult>({
      method: 'POST',
      url: '/api/app/payments/stripe/webhook',
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
