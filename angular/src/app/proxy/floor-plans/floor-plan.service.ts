import type { BoothAvailabilityDto, CreateFloorPlanBoothDto, CreateFloorPlanDto, FloorPlanBoothDto, FloorPlanDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class FloorPlanService {
  apiName = 'Default';
  

  addBooth = (floorPlanId: string, input: CreateFloorPlanBoothDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanBoothDto>({
      method: 'POST',
      url: `/${floorPlanId}/booths`,
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
      url: `/${id}/deactivate`,
    },
    { apiName: this.apiName,...config });
  

  getBooths = (floorPlanId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanBoothDto[]>({
      method: 'GET',
      url: `/${floorPlanId}/booths`,
    },
    { apiName: this.apiName,...config });
  

  getBoothsAvailability = (floorPlanId: string, startDate: string, endDate: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothAvailabilityDto[]>({
      method: 'GET',
      url: `/${floorPlanId}/booths-availability`,
      params: { startDate, endDate },
    },
    { apiName: this.apiName,...config });
  

  publish = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanDto>({
      method: 'POST',
      url: `/${id}/publish`,
    },
    { apiName: this.apiName,...config });
  

  removeBooth = (floorPlanId: string, boothId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/${floorPlanId}/booths/${boothId}`,
    },
    { apiName: this.apiName,...config });
  

  updateBoothPosition = (floorPlanId: string, boothId: string, input: CreateFloorPlanBoothDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FloorPlanBoothDto>({
      method: 'PUT',
      url: `/${floorPlanId}/booths/${boothId}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
