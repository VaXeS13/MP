import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { CreatePaymentTransactionDto, GetPaymentTransactionListDto, PaymentSuccessViewModel, PaymentTransactionDto, UpdatePaymentTransactionDto } from '../../payments/models';

@Injectable({
  providedIn: 'root',
})
export class PaymentTransactionService {
  apiName = 'Default';
  

  create = (input: CreatePaymentTransactionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentTransactionDto>({
      method: 'POST',
      url: '/api/app/payment-transaction',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/payment-transaction/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentTransactionDto>({
      method: 'GET',
      url: `/api/app/payment-transaction/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetPaymentTransactionListDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<PaymentTransactionDto>>({
      method: 'GET',
      url: '/api/app/payment-transaction',
      params: { filter: input.filter, status: input.status, paymentMethod: input.paymentMethod, startDate: input.startDate, endDate: input.endDate, minAmount: input.minAmount, maxAmount: input.maxAmount, rentalId: input.rentalId, email: input.email, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getPaymentSuccessViewModel = (sessionId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentSuccessViewModel>({
      method: 'GET',
      url: `/api/app/payment-transaction/payment-success-view-model/${sessionId}`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdatePaymentTransactionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentTransactionDto>({
      method: 'PUT',
      url: `/api/app/payment-transaction/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
