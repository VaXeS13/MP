import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { Przelewy24PaymentMethod, Przelewy24PaymentRequest, Przelewy24PaymentResult, Przelewy24PaymentStatus } from '../../domain/payments/models';

@Injectable({
  providedIn: 'root',
})
export class Przelewy24Service {
  apiName = 'Default';
  

  createPayment = (request: Przelewy24PaymentRequest, config?: Partial<Rest.Config>) =>
    this.restService.request<any, Przelewy24PaymentResult>({
      method: 'POST',
      url: '/api/app/przelewy24/payment',
      body: request,
    },
    { apiName: this.apiName,...config });
  

  generatePaymentUrlByTransactionId = (transactionId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, string>({
      method: 'POST',
      responseType: 'text',
      url: `/api/app/przelewy24/generate-payment-url/${transactionId}`,
    },
    { apiName: this.apiName,...config });
  

  getPaymentMethods = (currency: string = "PLN", config?: Partial<Rest.Config>) =>
    this.restService.request<any, Przelewy24PaymentMethod[]>({
      method: 'GET',
      url: '/api/app/przelewy24/payment-methods',
      params: { currency },
    },
    { apiName: this.apiName,...config });
  

  getPaymentStatus = (transactionId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, Przelewy24PaymentStatus>({
      method: 'GET',
      url: `/api/app/przelewy24/payment-status/${transactionId}`,
    },
    { apiName: this.apiName,...config });
  

  verifyPayment = (transactionId: string, expectedAmount: number, config?: Partial<Rest.Config>) =>
    this.restService.request<any, boolean>({
      method: 'POST',
      url: `/api/app/przelewy24/verify-payment/${transactionId}`,
      params: { expectedAmount },
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
