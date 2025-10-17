import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { BoothAvailabilityDto, FloorPlanBoothDto, FloorPlanDto, GetFloorPlanListDto } from '../floor-plans/models';

@Injectable({
  providedIn: 'root',
})
export class FloorPlanService {
  apiName = 'Default';
  

  getBoothsAvailabilityByFloorPlanIdAndStartDateAndEndDate = (floorPlanId: string, startDate: string, endDate: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothAvailabilityDto[]>({
      method: 'GET',
      url: `/api/app/floor-plan/${floorPlanId}/booths-availability`,
      params: { startDate, endDate },
    },
    { apiName: this.apiName,...config });
  

  getBoothsByFloorPlanId = (floorPlanId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanBoothDto[]>({
      method: 'GET',
      url: `/api/app/floor-plan/${floorPlanId}/booths`,
    },
    { apiName: this.apiName,...config });
  

  getById = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanDto>({
      method: 'GET',
      url: `/api/app/floor-plan/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getListByInput = (input: GetFloorPlanListDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<FloorPlanDto>>({
      method: 'GET',
      url: '/api/app/floor-plan',
      params: { tenantId: input.tenantId, isActive: input.isActive, level: input.level, filter: input.filter, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
