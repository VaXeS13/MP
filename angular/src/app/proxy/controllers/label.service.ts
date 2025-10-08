import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { IActionResult } from '../microsoft/asp-net-core/mvc/models';

@Injectable({
  providedIn: 'root',
})
export class LabelService {
  apiName = 'Default';
  

  generateItemLabel = (rentalItemId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, IActionResult>({
      method: 'GET',
      url: `/api/app/labels/rental-item/${rentalItemId}`,
    },
    { apiName: this.apiName,...config });
  

  generateMultipleLabels = (rentalItemIds: string[], config?: Partial<Rest.Config>) =>
    this.restService.request<any, IActionResult>({
      method: 'POST',
      url: '/api/app/labels/multiple',
      body: rentalItemIds,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
