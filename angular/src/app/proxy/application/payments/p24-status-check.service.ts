import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class P24StatusCheckService {
  apiName = 'Default';
  

  scheduleStatusCheck = (delayMinutes: number = 15, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/app/p24Status-check/schedule-status-check',
      params: { delayMinutes },
    },
    { apiName: this.apiName,...config });
  

  startPeriodicStatusCheck = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/app/p24Status-check/start-periodic-status-check',
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
