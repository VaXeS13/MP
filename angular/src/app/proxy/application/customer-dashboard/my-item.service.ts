import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { BulkUpdateMyItemsDto, CreateMyItemDto, GetMyItemsDto, MyItemDto, MyItemStatisticsDto, UpdateMyItemDto } from '../contracts/customer-dashboard/models';

@Injectable({
  providedIn: 'root',
})
export class MyItemService {
  apiName = 'Default';
  

  bulkUpdate = (input: BulkUpdateMyItemsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/app/my-item/bulk-update',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateMyItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, MyItemDto>({
      method: 'POST',
      url: '/api/app/my-item',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/my-item/${id}`,
    },
    { apiName: this.apiName,...config });
  

  generateItemLabels = (itemIds: string[], config?: Partial<Rest.Config>) =>
    this.restService.request<any, number[]>({
      method: 'POST',
      url: '/api/app/my-item/generate-item-labels',
      body: itemIds,
    },
    { apiName: this.apiName,...config });
  

  getMyItem = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, MyItemDto>({
      method: 'GET',
      url: `/api/app/my-item/${id}/my-item`,
    },
    { apiName: this.apiName,...config });
  

  getMyItemCategories = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, string[]>({
      method: 'GET',
      url: '/api/app/my-item/my-item-categories',
    },
    { apiName: this.apiName,...config });
  

  getMyItemStatistics = (rentalId?: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, MyItemStatisticsDto>({
      method: 'GET',
      url: '/api/app/my-item/my-item-statistics',
      params: { rentalId },
    },
    { apiName: this.apiName,...config });
  

  getMyItems = (input: GetMyItemsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<MyItemDto>>({
      method: 'GET',
      url: '/api/app/my-item/my-items',
      params: { rentalId: input.rentalId, status: input.status, category: input.category, searchTerm: input.searchTerm, createdAfter: input.createdAfter, createdBefore: input.createdBefore, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateMyItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, MyItemDto>({
      method: 'PUT',
      url: `/api/app/my-item/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
