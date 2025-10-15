import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { CreatePaymentTransactionDto, GetPaymentTransactionListDto, PaymentSuccessViewModel, PaymentTransactionDto, UpdatePaymentTransactionDto } from '../payments/models';

@Injectable({
  providedIn: 'root',
})
export class PaymentTransactionsService {
  apiName = 'Default';
  

  create = (input: CreatePaymentTransactionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentTransactionDto>({
      method: 'POST',
      url: '/api/app/payment-transactions',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/payment-transactions/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentTransactionDto>({
      method: 'GET',
      url: `/api/app/payment-transactions/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetPaymentTransactionListDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<PaymentTransactionDto>>({
      method: 'GET',
      url: '/api/app/payment-transactions',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  getPaymentSuccessViewModel = (sessionId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentSuccessViewModel>({
      method: 'GET',
      url: `/api/app/payment-transactions/payment-success/${sessionId}`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdatePaymentTransactionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentTransactionDto>({
      method: 'PUT',
      url: `/api/app/payment-transactions/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
