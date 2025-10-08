import { BoothDto } from './booth.model';

export interface FloorPlanDto {
  id: string;
  name: string;
  level: number;
  width: number;
  height: number;
  isActive: boolean;
  booths: FloorPlanBoothDto[];
  elements?: ElementPosition[];
  creationTime: Date;
  creatorId?: string;
  lastModificationTime?: Date;
  lastModifierId?: string;
}

export interface FloorPlanBoothDto {
  id: string;
  floorPlanId: string;
  boothId: string;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
  booth?: BoothDto;
  creationTime: Date;
  creatorId?: string;
  lastModificationTime?: Date;
  lastModifierId?: string;
}

export interface CreateFloorPlanDto {
  name: string;
  level: number;
  width: number;
  height: number;
  booths: CreateFloorPlanBoothDto[];
  elements?: any[];
}

export interface CreateFloorPlanBoothDto {
  boothId: string;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
}

export interface UpdateFloorPlanDto {
  name: string;
  level: number;
  width: number;
  height: number;
  booths: CreateFloorPlanBoothDto[];
  elements?: any[];
}

export interface GetFloorPlanListDto {
  tenantId?: string;
  isActive?: boolean;
  level?: number;
  filter?: string;
  skipCount?: number;
  maxResultCount?: number;
  sorting?: string;
}

export interface FloorPlanLevel {
  level: number;
  name: string;
  planCount: number;
}

export interface BoothPosition {
  boothId: string;
  x: number;
  y: number;
  width: number;
  height: number;
  rotation: number;
  booth?: BoothDto;
}

export interface ElementPosition {
  id?: string;
  elementType: number;
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

export interface FloorPlanCanvas {
  width: number;
  height: number;
  scale: number;
  offsetX: number;
  offsetY: number;
}

export interface FloorPlanToolbarAction {
  id: string;
  label: string;
  icon: string;
  action: () => void;
  disabled?: boolean;
}

export interface BoothAvailabilityDto {
  boothId: string;
  boothNumber: string;
  status: string; // available, reserved, rented, maintenance
  nextAvailableFrom: Date;
  overlaps: RentalOverlapDto[];
}

export interface RentalOverlapDto {
  id: string;
  userId: string;
  startDate: Date;
  endDate: Date;
  status: string;
}