import { RestService, Rest } from '@abp/ng.core';
import type { PagedAndSortedResultRequestDto, PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { CreateHomePageSectionDto, HomePageSectionDto, ReorderSectionDto, UpdateHomePageSectionDto } from '../contracts/home-page-content/models';

@Injectable({
  providedIn: 'root',
})
export class HomePageSectionService {
  apiName = 'Default';
  

  activate = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, HomePageSectionDto>({
      method: 'POST',
      url: `/api/app/home-page-section/${id}/activate`,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateHomePageSectionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, HomePageSectionDto>({
      method: 'POST',
      url: '/api/app/home-page-section',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  deactivate = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, HomePageSectionDto>({
      method: 'POST',
      url: `/api/app/home-page-section/${id}/deactivate`,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/home-page-section/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, HomePageSectionDto>({
      method: 'GET',
      url: `/api/app/home-page-section/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getActiveForDisplay = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, HomePageSectionDto[]>({
      method: 'GET',
      url: '/api/app/home-page-section/active-for-display',
    },
    { apiName: this.apiName,...config });
  

  getAllOrdered = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, HomePageSectionDto[]>({
      method: 'GET',
      url: '/api/app/home-page-section/ordered',
    },
    { apiName: this.apiName,...config });
  

  getList = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<HomePageSectionDto>>({
      method: 'GET',
      url: '/api/app/home-page-section',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  reorder = (reorderList: ReorderSectionDto[], config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/app/home-page-section/reorder',
      body: reorderList,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateHomePageSectionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, HomePageSectionDto>({
      method: 'PUT',
      url: `/api/app/home-page-section/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
