import type { FloorPlanElementType } from '../domain/floor-plans/floor-plan-element-type.enum';
import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { BoothDto } from '../booths/models';

export interface BoothAvailabilityDto {
  boothId?: string;
  boothNumber?: string;
  status?: string;
  nextAvailableFrom?: string;
  overlaps: RentalOverlapDto[];
}

export interface CreateFloorPlanBoothDto {
  boothId: string;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
}

export interface CreateFloorPlanDto {
  name: string;
  level: number;
  width: number;
  height: number;
  booths: CreateFloorPlanBoothDto[];
  elements: CreateFloorPlanElementDto[];
}

export interface CreateFloorPlanElementDto {
  elementType: FloorPlanElementType;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
  color?: string;
  text?: string;
  iconName?: string;
  thickness?: number;
  opacity?: number;
  direction?: string;
}

export interface FloorPlanBoothDto extends FullAuditedEntityDto<string> {
  floorPlanId?: string;
  boothId?: string;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
  booth: BoothDto;
}

export interface FloorPlanDto extends FullAuditedEntityDto<string> {
  name?: string;
  level: number;
  width: number;
  height: number;
  isActive: boolean;
  booths: FloorPlanBoothDto[];
  elements: FloorPlanElementDto[];
}

export interface FloorPlanElementDto extends FullAuditedEntityDto<string> {
  floorPlanId?: string;
  elementType?: FloorPlanElementType;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
  color?: string;
  text?: string;
  iconName?: string;
  thickness?: number;
  opacity?: number;
  direction?: string;
}

export interface GetFloorPlanElementListDto extends PagedAndSortedResultRequestDto {
  floorPlanId?: string;
  elementType?: FloorPlanElementType;
}

export interface RentalOverlapDto {
  id?: string;
  userId?: string;
  startDate?: string;
  endDate?: string;
  status?: string;
}

export interface UpdateFloorPlanElementDto {
  elementType: FloorPlanElementType;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
  color?: string;
  text?: string;
  iconName?: string;
  thickness?: number;
  opacity?: number;
  direction?: string;
}

export interface GetFloorPlanListDto extends PagedAndSortedResultRequestDto {
  tenantId?: string;
  isActive?: boolean;
  level?: number;
  filter?: string;
}
