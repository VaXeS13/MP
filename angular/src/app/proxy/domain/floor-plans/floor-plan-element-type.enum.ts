import { mapEnumToOptions } from '@abp/ng.core';

export enum FloorPlanElementType {
  Wall = 1,
  Door = 2,
  Window = 3,
  Pillar = 4,
  Stairs = 5,
  Checkout = 6,
  Restroom = 7,
  InfoDesk = 8,
  EmergencyExit = 9,
  Storage = 10,
  TextLabel = 11,
  Zone = 12,
}

export const floorPlanElementTypeOptions = mapEnumToOptions(FloorPlanElementType);
