import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { BoothAvailabilityDto, CreateFloorPlanBoothDto, CreateFloorPlanDto, FloorPlanBoothDto, FloorPlanDto, GetFloorPlanListDto, UpdateFloorPlanDto } from '../floor-plans/models';

@Injectable({
  providedIn: 'root',
})
export class FloorPlanService {
  apiName = 'Default';
  

  addBooth = (floorPlanId: string, input: CreateFloorPlanBoothDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanBoothDto>({
      method: 'POST',
      url: `/api/app/floor-plan/${floorPlanId}/booths`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateFloorPlanDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanDto>({
      method: 'POST',
      url: '/api/app/floor-plan',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  deactivate = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanDto>({
      method: 'POST',
      url: `/api/app/floor-plan/${id}/deactivate`,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/floor-plan/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanDto>({
      method: 'GET',
      url: `/api/app/floor-plan/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getBooths = (floorPlanId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanBoothDto[]>({
      method: 'GET',
      url: `/api/app/floor-plan/${floorPlanId}/booths`,
    },
    { apiName: this.apiName,...config });
  

  getBoothsAvailability = (floorPlanId: string, startDate: string, endDate: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothAvailabilityDto[]>({
      method: 'GET',
      url: `/api/app/floor-plan/${floorPlanId}/booths-availability`,
      params: { startDate, endDate },
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetFloorPlanListDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<FloorPlanDto>>({
      method: 'GET',
      url: '/api/app/floor-plan',
      params: { tenantId: input.tenantId, isActive: input.isActive, level: input.level, filter: input.filter, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getListByTenant = (tenantId: string, isActive?: boolean, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanDto[]>({
      method: 'GET',
      url: '/api/app/floor-plan/by-tenant',
      params: { tenantId, isActive },
    },
    { apiName: this.apiName,...config });
  

  publish = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanDto>({
      method: 'POST',
      url: `/api/app/floor-plan/${id}/publish`,
    },
    { apiName: this.apiName,...config });
  

  removeBooth = (floorPlanId: string, boothId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/floor-plan/${floorPlanId}/booths/${boothId}`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateFloorPlanDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanDto>({
      method: 'PUT',
      url: `/api/app/floor-plan/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  updateBoothPosition = (floorPlanId: string, boothId: string, input: CreateFloorPlanBoothDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanBoothDto>({
      method: 'PUT',
      url: `/api/app/floor-plan/${floorPlanId}/booths/${boothId}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
