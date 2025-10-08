import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class LabelGeneratorService {
  apiName = 'Default';
  

  generateLabelPdf = (itemSheetItemId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, number[]>({
      method: 'POST',
      url: `/api/app/label-generator/generate-label-pdf/${itemSheetItemId}`,
    },
    { apiName: this.apiName,...config });
  

  generateMultipleLabelsPdf = (itemSheetItemIds: string[], config?: Partial<Rest.Config>) =>
    this.restService.request<any, number[]>({
      method: 'POST',
      url: '/api/app/label-generator/generate-multiple-labels-pdf',
      body: itemSheetItemIds,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
