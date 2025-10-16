import type { CreateFloorPlanElementDto, FloorPlanElementDto, GetFloorPlanElementListDto, UpdateFloorPlanElementDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class FloorPlanElementService {
  apiName = 'Default';
  

  create = (floorPlanId: string, input: CreateFloorPlanElementDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanElementDto>({
      method: 'POST',
      url: '/api/app/floor-plan-element',
      params: { floorPlanId },
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/floor-plan-element/${id}`,
    },
    { apiName: this.apiName,...config });
  

  deleteByFloorPlan = (floorPlanId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/floor-plan-element/by-floor-plan/${floorPlanId}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanElementDto>({
      method: 'GET',
      url: `/api/app/floor-plan-element/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetFloorPlanElementListDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<FloorPlanElementDto>>({
      method: 'GET',
      url: '/api/app/floor-plan-element',
      params: { floorPlanId: input.floorPlanId, elementType: input.elementType, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getListByFloorPlan = (floorPlanId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanElementDto[]>({
      method: 'GET',
      url: `/api/app/floor-plan-element/by-floor-plan/${floorPlanId}`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateFloorPlanElementDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanElementDto>({
      method: 'PUT',
      url: `/api/app/floor-plan-element/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
