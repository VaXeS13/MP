import { RestService, Rest } from '@abp/ng.core';
import type { PagedAndSortedResultRequestDto, PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { CompleteWithdrawalDto, PaymentWithdrawalDto, PaymentWithdrawalStatsDto, ProcessWithdrawalDto, RejectWithdrawalDto } from '../contracts/settlements/models';

@Injectable({
  providedIn: 'root',
})
export class PaymentWithdrawalService {
  apiName = 'Default';
  

  complete = (input: CompleteWithdrawalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentWithdrawalDto>({
      method: 'POST',
      url: '/api/app/payment-withdrawal/complete',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  executeStripePayout = (settlementId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentWithdrawalDto>({
      method: 'POST',
      url: `/api/app/payment-withdrawal/execute-stripe-payout/${settlementId}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentWithdrawalDto>({
      method: 'GET',
      url: `/api/app/payment-withdrawal/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<PaymentWithdrawalDto>>({
      method: 'GET',
      url: '/api/app/payment-withdrawal',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getStats = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentWithdrawalStatsDto>({
      method: 'GET',
      url: '/api/app/payment-withdrawal/stats',
    },
    { apiName: this.apiName,...config });
  

  process = (input: ProcessWithdrawalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentWithdrawalDto>({
      method: 'POST',
      url: '/api/app/payment-withdrawal/process',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  reject = (input: RejectWithdrawalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentWithdrawalDto>({
      method: 'POST',
      url: '/api/app/payment-withdrawal/reject',
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
