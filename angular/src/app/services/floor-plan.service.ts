import { Injectable } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';
import { PagedResultDto } from '@abp/ng.core';
import {
  FloorPlanDto,
  FloorPlanBoothDto,
  CreateFloorPlanDto,
  CreateFloorPlanBoothDto,
  UpdateFloorPlanDto,
  GetFloorPlanListDto,
  BoothAvailabilityDto
} from '../shared/models/floor-plan.model';

@Injectable({
  providedIn: 'root'
})
export class FloorPlanService {
  constructor(private rest: RestService) {}

  getList(input: GetFloorPlanListDto): Observable<PagedResultDto<FloorPlanDto>> {
    return this.rest.request<any, PagedResultDto<FloorPlanDto>>({
      method: 'GET',
      url: '/api/app/floor-plan',
      params: input
    });
  }

  getListByTenant(tenantId?: string, isActive?: boolean): Observable<FloorPlanDto[]> {
    const params: any = {};
    if (isActive !== undefined) {
      params.isActive = isActive;
    }
    if (tenantId) {
      params.tenantId = tenantId;
    }

    return this.rest.request<any, FloorPlanDto[]>({
      method: 'GET',
      url: '/api/app/floor-plan/by-tenant',
      params
    });
  }

  get(id: string): Observable<FloorPlanDto> {
    return this.rest.request<any, FloorPlanDto>({
      method: 'GET',
      url: `/api/app/floor-plan/${id}`
    });
  }

  create(input: CreateFloorPlanDto): Observable<FloorPlanDto> {
    return this.rest.request<CreateFloorPlanDto, FloorPlanDto>({
      method: 'POST',
      url: '/api/app/floor-plan',
      body: input
    });
  }

  update(id: string, input: UpdateFloorPlanDto): Observable<FloorPlanDto> {
    return this.rest.request<UpdateFloorPlanDto, FloorPlanDto>({
      method: 'PUT',
      url: `/api/app/floor-plan/${id}`,
      body: input
    });
  }

  delete(id: string): Observable<void> {
    return this.rest.request<any, void>({
      method: 'DELETE',
      url: `/api/app/floor-plan/${id}`
    });
  }

  publish(id: string): Observable<FloorPlanDto> {
    return this.rest.request<any, FloorPlanDto>({
      method: 'POST',
      url: `/api/app/floor-plan/${id}/publish`
    });
  }

  deactivate(id: string): Observable<FloorPlanDto> {
    return this.rest.request<any, FloorPlanDto>({
      method: 'POST',
      url: `/api/app/floor-plan/${id}/deactivate`
    });
  }

  getBooths(floorPlanId: string): Observable<FloorPlanBoothDto[]> {
    return this.rest.request<any, FloorPlanBoothDto[]>({
      method: 'GET',
      url: `/api/app/floor-plan/${floorPlanId}/booths`
    });
  }

  addBooth(floorPlanId: string, input: CreateFloorPlanBoothDto): Observable<FloorPlanBoothDto> {
    return this.rest.request<CreateFloorPlanBoothDto, FloorPlanBoothDto>({
      method: 'POST',
      url: `/api/app/floor-plan/${floorPlanId}/booths`,
      body: input
    });
  }

  updateBoothPosition(
    floorPlanId: string,
    boothId: string,
    input: CreateFloorPlanBoothDto
  ): Observable<FloorPlanBoothDto> {
    return this.rest.request<CreateFloorPlanBoothDto, FloorPlanBoothDto>({
      method: 'PUT',
      url: `/api/app/floor-plan/${floorPlanId}/booths/${boothId}`,
      body: input
    });
  }

  removeBooth(floorPlanId: string, boothId: string): Observable<void> {
    return this.rest.request<any, void>({
      method: 'DELETE',
      url: `/api/app/floor-plan/${floorPlanId}/booths/${boothId}`
    });
  }

  getBoothsAvailability(
    floorPlanId: string,
    startDate: string,
    endDate: string
  ): Observable<BoothAvailabilityDto[]> {
    return this.rest.request<any, BoothAvailabilityDto[]>({
      method: 'GET',
      url: `/api/app/floor-plan/${floorPlanId}/booths-availability`,
      params: { startDate, endDate }
    });
  }

  // Helper methods for working with floor plans

  getAvailableLevels(floorPlans: FloorPlanDto[]): number[] {
    const levels = [...new Set(floorPlans.map(fp => fp.level))];
    return levels.sort((a, b) => a - b);
  }

  getFloorPlansByLevel(floorPlans: FloorPlanDto[], level: number): FloorPlanDto[] {
    return floorPlans.filter(fp => fp.level === level);
  }

  getActiveFloorPlan(floorPlans: FloorPlanDto[], level?: number): FloorPlanDto | undefined {
    const filtered = level !== undefined
      ? floorPlans.filter(fp => fp.level === level && fp.isActive)
      : floorPlans.filter(fp => fp.isActive);

    return filtered[0];
  }

  calculateCanvasBounds(booths: FloorPlanBoothDto[]): { width: number; height: number } {
    if (booths.length === 0) {
      return { width: 800, height: 600 };
    }

    const maxX = Math.max(...booths.map(b => b.x + b.width));
    const maxY = Math.max(...booths.map(b => b.y + b.height));

    return {
      width: Math.max(maxX + 50, 800),
      height: Math.max(maxY + 50, 600)
    };
  }
}