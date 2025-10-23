import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { BoothTypeDto } from '../application/contracts/booth-types/models';
import type { ActionResult } from '../microsoft/asp-net-core/mvc/models';

@Injectable({
  providedIn: 'root',
})
export class BoothTypeService {
  apiName = 'Default';
  

  getActiveTypes = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, ActionResult<any<BoothTypeDto>>>({
      method: 'GET',
      url: '/api/app/booth-type/active',
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
