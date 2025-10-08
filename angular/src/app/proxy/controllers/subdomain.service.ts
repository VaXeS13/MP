import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { IActionResult } from '../microsoft/asp-net-core/mvc/models';

@Injectable({
  providedIn: 'root',
})
export class SubdomainService {
  apiName = 'Default';
  

  getOAuthDebugInfo = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, IActionResult>({
      method: 'GET',
      url: '/api/app/subdomain/oauth-debug',
    },
    { apiName: this.apiName,...config });
  

  getSubdomainInfo = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, IActionResult>({
      method: 'GET',
      url: '/api/app/subdomain/info',
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
