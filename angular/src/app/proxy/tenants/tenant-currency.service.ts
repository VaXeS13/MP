import type { TenantCurrencyDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TenantCurrencyService {
  apiName = 'Default';
  

  getTenantCurrency = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, TenantCurrencyDto>({
      method: 'GET',
      url: '/api/app/tenant-currency/tenant-currency',
    },
    { apiName: this.apiName,...config });
  

  updateTenantCurrency = (input: TenantCurrencyDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'PUT',
      url: '/api/app/tenant-currency/tenant-currency',
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
