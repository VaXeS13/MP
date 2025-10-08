import { RestService, Rest } from '@abp/ng.core';
import type { PagedAndSortedResultRequestDto, PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { BoothTypeDto, CreateBoothTypeDto, UpdateBoothTypeDto } from '../contracts/booth-types/models';

@Injectable({
  providedIn: 'root',
})
export class BoothTypeService {
  apiName = 'Default';
  

  activate = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothTypeDto>({
      method: 'POST',
      url: `/api/app/booth-type/${id}/activate`,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateBoothTypeDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothTypeDto>({
      method: 'POST',
      url: '/api/app/booth-type',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  deactivate = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothTypeDto>({
      method: 'POST',
      url: `/api/app/booth-type/${id}/deactivate`,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/booth-type/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothTypeDto>({
      method: 'GET',
      url: `/api/app/booth-type/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getActiveTypes = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothTypeDto[]>({
      method: 'GET',
      url: '/api/app/booth-type/active-types',
    },
    { apiName: this.apiName,...config });
  

  getList = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<BoothTypeDto>>({
      method: 'GET',
      url: '/api/app/booth-type',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateBoothTypeDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothTypeDto>({
      method: 'PUT',
      url: `/api/app/booth-type/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
