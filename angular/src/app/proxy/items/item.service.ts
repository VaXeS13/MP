import type { BulkItemCreationResultDto, CreateBulkItemsDto, CreateItemDto, ItemDto, UpdateItemDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedAndSortedResultRequestDto, PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ItemService {
  apiName = 'Default';
  

  create = (input: CreateItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ItemDto>({
      method: 'POST',
      url: '/api/app/item',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  createBulk = (input: CreateBulkItemsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BulkItemCreationResultDto>({
      method: 'POST',
      url: '/api/app/item/bulk',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/item/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ItemDto>({
      method: 'GET',
      url: `/api/app/item/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<ItemDto>>({
      method: 'GET',
      url: '/api/app/item',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getMyItems = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<ItemDto>>({
      method: 'GET',
      url: '/api/app/item/my-items',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ItemDto>({
      method: 'PUT',
      url: `/api/app/item/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
