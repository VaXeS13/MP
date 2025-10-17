import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { BoothDto, UpdateBoothDto } from '../booths/models';

@Injectable({
  providedIn: 'root',
})
export class BoothService {
  apiName = 'Default';
  

  deleteById = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/booth/${id}`,
    },
    { apiName: this.apiName,...config });
  

  updateByIdAndInput = (id: string, input: UpdateBoothDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothDto>({
      method: 'PUT',
      url: `/api/app/booth/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
