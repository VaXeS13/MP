import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { IActionResult } from '../microsoft/asp-net-core/mvc/models';

@Injectable({
  providedIn: 'root',
})
export class SubdomainInfoService {
  apiName = 'Default';
  

  getDebugInfo = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, IActionResult>({
      method: 'GET',
      url: '/api/subdomain/debug',
    },
    { apiName: this.apiName,...config });
  

  getSubdomainInfo = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, IActionResult>({
      method: 'GET',
      url: '/api/subdomain/info',
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
