import type { BoothSettingsDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class BoothSettingsService {
  apiName = 'Default';
  

  get = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothSettingsDto>({
      method: 'GET',
      url: '/api/app/booth-settings',
    },
    { apiName: this.apiName,...config });
  

  update = (input: BoothSettingsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'PUT',
      url: '/api/app/booth-settings',
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
